using System;
using System.Threading;
using System.Threading.Tasks;

namespace dinospace.Services
{
    // Thin, safe wrapper around the on-device NovaSaur engine (the bound
    // novasaur.aar).
    //
    // Reliability model (learned the hard way):
    //
    //  1. ONE inference at a time. Starting a new conversation while an old
    //     one is still generating wedges the engine, so the lock is held
    //     INSIDE the worker task. Every question queues on that lock — a new
    //     question simply waits its turn instead of racing or being rejected.
    //
    //  2. PERIODIC RELOAD. LiteRT-LM's conversations share one native token
    //     budget, so a long-lived engine goes quiet after a handful of
    //     questions (the old "only 3 answers" bug). So before an inference,
    //     if the engine has already answered a couple of times (or a previous
    //     call errored), it is reloaded first — inside the same lock, so it
    //     can never overlap an inference. Bounded budget use, self-healing,
    //     and the reload cost is only paid every few questions, not every one.
    //
    //  3. THE IDLE CLOCK STARTS AT INFERENCE. Time spent queued behind another
    //     answer or a reload does NOT count against the "is it stuck?" timeout,
    //     so a question that waited its turn still gets its full chance to
    //     answer instead of being cut off as a false timeout.
    public static class NovaSaurService
    {
        private static readonly SemaphoreSlim _lock = new(1, 1);

        // How many model answers to allow before the engine is reloaded. Set
        // to 1 so every question after the first runs on a completely fresh
        // engine: the small model can't carry state between questions, so we
        // give each one a clean slate. Reloads happen inside the lock, so an
        // inference and a reload can never overlap.
        private const int AnswersPerReload = 1;

        // The UI-facing cap for the blocking Ask() path.
        private static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(45);

        // Engine health, guarded by _lock (only touched while it's held).
        private static int _answersSinceReload;
        private static bool _forceReloadNext;

        public static bool SupportedPlatform =>
#if ANDROID
            true;
#else
            false;
#endif

        public static bool IsReady
        {
#if ANDROID
            get { try { return Com.Novasaur.NovaSaurModule.IsReady; } catch { return false; } }
#else
            get => false;
#endif
        }

        private static Task? _initTask;
        private static readonly object _initGate = new();

        // Loads the model into memory. Single-flight: every caller shares one
        // native Init — running two at once (background warm-up + on-demand
        // load) can wedge the engine, which showed up as an endless
        // "thinking…" on the second question.
        public static Task InitAsync()
        {
#if ANDROID
            lock (_initGate)
            {
                // Re-init when the last attempt faulted, or when it "succeeded"
                // but the engine has since died (e.g. a failed between-question
                // reload) — otherwise a completed task would block recovery.
                bool stale = _initTask != null && _initTask.IsCompleted && !IsReady;
                if (_initTask == null || _initTask.IsFaulted || stale)
                    _initTask = Task.Run(() =>
                    {
                        if (Com.Novasaur.NovaSaurModule.IsReady) return;
                        var ctx = Android.App.Application.Context;
                        Com.Novasaur.NovaSaurModule.Init(ctx);
                    });
                return _initTask;
            }
#else
            return Task.CompletedTask;
#endif
        }

#if ANDROID
        // Reload the engine when its budget may be low or a previous call left
        // it in a bad state. Must be called with _lock held.
        private static void ReloadIfNeeded()
        {
            if (!_forceReloadNext && _answersSinceReload < AnswersPerReload) return;
            try { Com.Novasaur.NovaSaurModule.Reset(); _answersSinceReload = 0; _forceReloadNext = false; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Nova reload: " + ex); }
        }
#endif

        // Waits for the model to load, but never past `cap` — the chat must
        // always come back with something.
        public static async Task<bool> InitWithTimeoutAsync(TimeSpan cap)
        {
            var init = InitAsync();
            var done = await Task.WhenAny(init, Task.Delay(cap));
            if (done == init)
            {
                try { await init; } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Nova init: " + ex); }
            }
            return IsReady;
        }

        // Runs one prompt to completion and returns the cleaned answer, or a
        // friendly message on timeout/failure. Never hangs the UI.
        public static async Task<string> AskAsync(string prompt, CancellationToken ct)
        {
#if ANDROID
            long inferenceStart = 0;
            var work = Task.Run(async () =>
            {
                await _lock.WaitAsync(CancellationToken.None);
                try
                {
                    ReloadIfNeeded();
                    inferenceStart = Environment.TickCount64;
                    string? r = Com.Novasaur.NovaSaurModule.Ask(prompt);
                    _answersSinceReload++;
                    return r;
                }
                finally { _lock.Release(); }
            }, CancellationToken.None);

            // Only start counting the timeout once inference actually begins;
            // queue/reload time doesn't count against it.
            while (!work.IsCompleted)
            {
                await Task.Delay(300, CancellationToken.None);
                if (inferenceStart != 0 && Environment.TickCount64 - inferenceStart > AnswerTimeout.TotalMilliseconds)
                {
                    _forceReloadNext = true;   // the abandoned call may leave the engine unhealthy
                    _ = work.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                    return TimeoutMessage;
                }
            }

            string? raw;
            try { raw = await work; }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur ask: " + ex);
                _forceReloadNext = true;
                return ErrorMessage;
            }

            if (raw == null) { _forceReloadNext = true; return ErrorMessage; }
            if (raw.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur bridge: " + raw);
                _forceReloadNext = true;
                return ErrorMessage;
            }

            string cleaned = PromptBuilder.Clean(raw);
            if (string.IsNullOrWhiteSpace(cleaned)) return ErrorMessage;
            return NovaGuard.CheckAnswer(cleaned) ?? cleaned;
#else
            await Task.CompletedTask;
            return "NovaSaur runs on Android right now.";
#endif
        }

        // Streams the answer token by token, ChatGPT-style. Same one-at-a-time
        // locking as AskAsync; a sliding inactivity window (that only starts
        // once inference begins) replaces a flat timeout, so long answers
        // aren't cut off while they're still visibly typing, and a question
        // that waited in the queue isn't punished for the wait.
        public static async Task<string> AskStreamAsync(string prompt, Action<string> onToken, CancellationToken ct)
        {
#if ANDROID
            var sb = new System.Text.StringBuilder();
            var done = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            long lastActivity = 0;
            bool inferenceStarted = false;

            var work = Task.Run(async () =>
            {
                await _lock.WaitAsync(CancellationToken.None);
                try
                {
                    ReloadIfNeeded();

                    // Inference is starting NOW — start the idle clock here so
                    // the queue/reload wait isn't counted as "stuck".
                    lastActivity = Environment.TickCount64;
                    inferenceStarted = true;

                    var relay = new StreamRelay(
                        token => { lock (sb) sb.Append(token); lastActivity = Environment.TickCount64; onToken(token); },
                        () => { string full; lock (sb) full = sb.ToString(); done.TrySetResult(full); },
                        err => done.TrySetResult("ERROR:" + err));
                    Com.Novasaur.NovaSaurModule.AskStream(prompt, relay);
                    // hold the lock until the native side reports done (or a
                    // hard cap, so a silent native failure can't wedge us)
                    var finished = await Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromMinutes(3)));
                    string result = finished == done.Task ? await done.Task : "ERROR:stream never completed";
                    if (!result.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase)) _answersSinceReload++;
                    return result;
                }
                finally { _lock.Release(); }
            }, CancellationToken.None);

            // Watch from the outside: patient while queued, generous while
            // tokens flow, impatient only when inference has started and then
            // gone silent.
            while (!work.IsCompleted)
            {
                await Task.Delay(400, CancellationToken.None);
                if (!inferenceStarted) continue;          // still queued or reloading — keep waiting
                long idleMs = Environment.TickCount64 - lastActivity;
                bool started; lock (sb) started = sb.Length > 0;
                if ((!started && idleMs > 45_000) || (started && idleMs > 25_000))
                {
                    _forceReloadNext = true;              // recover the engine before the next question
                    return TimeoutMessage;
                }
            }

            string raw;
            try { raw = await work; }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur stream: " + ex);
                _forceReloadNext = true;
                return ErrorMessage;
            }
            if (raw.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur stream: " + raw);
                _forceReloadNext = true;
                return ErrorMessage;
            }
            string cleaned = PromptBuilder.Clean(raw);
            if (string.IsNullOrWhiteSpace(cleaned)) return ErrorMessage;
            return NovaGuard.CheckAnswer(cleaned) ?? cleaned;
#else
            await Task.CompletedTask;
            return "NovaSaur runs on Android right now.";
#endif
        }

#if ANDROID
        // Marshals the Java streaming callbacks into plain C# delegates.
        private sealed class StreamRelay : Java.Lang.Object, Com.Novasaur.IStreamCallback
        {
            private readonly Action<string> _onToken;
            private readonly Action _onDone;
            private readonly Action<string> _onError;

            public StreamRelay(Action<string> onToken, Action onDone, Action<string> onError)
            { _onToken = onToken; _onDone = onDone; _onError = onError; }

            public void OnToken(string? token) { if (!string.IsNullOrEmpty(token)) _onToken(token); }
            public void OnDone() => _onDone();
            public void OnError(string? error) => _onError(error ?? "unknown");
        }
#endif

        public const string TimeoutMessage =
            "Phew, that one's a real brain-bender! Give me a few seconds to catch my breath, then try asking it a shorter way.";
        public const string ErrorMessage =
            "Something went sideways answering that. Give it another try in a moment.";
        public const string BusyMessage =
            "I'm still finishing your last question — give me a few seconds, then ask away!";
    }
}
