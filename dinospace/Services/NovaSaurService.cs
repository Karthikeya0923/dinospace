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
    //     one is still generating wedges the engine permanently, so the lock
    //     is held INSIDE the worker task — if a call times out for the UI,
    //     the abandoned task keeps holding the lock until the native engine
    //     actually finishes, and two inferences can never overlap.
    //
    //  2. FRESH ENGINE per answer. All conversations share one native token
    //     budget, so a long-lived engine goes quiet after a few questions.
    //     After every model answer (or timeout) the engine is reloaded in the
    //     background; each question is fully independent — no chat history,
    //     no leftover state, question 50 behaves like question 1.
    public static class NovaSaurService
    {
        private static readonly SemaphoreSlim _lock = new(1, 1);

        // The UI-facing cap. The native call may run longer in the background;
        // the lock protects the engine while it does. Kept fairly short because
        // the common questions are now answered instantly without the model, so
        // this only bounds the rarer open-ended fallbacks.
        private static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(30);

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
        // Every question runs against a freshly loaded engine. LiteRT-LM draws
        // all conversations from one shared token budget, so an engine that
        // isn't reloaded stops answering after a handful of questions — this
        // was the "NovaSaur only answers 3 questions" bug. The reload happens
        // in the background right after an answer (or a timeout/failure, which
        // also un-wedges a stuck engine); it queues on the same lock as
        // inference so the two can never overlap.
        private static void ScheduleReset()
        {
            _ = Task.Run(async () =>
            {
                await _lock.WaitAsync(CancellationToken.None);
                try { Com.Novasaur.NovaSaurModule.Reset(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Nova reset: " + ex); }
                finally { _lock.Release(); }
            });
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
        // friendly message on timeout/failure. Never hangs the UI: the caller
        // always gets a reply within AnswerTimeout.
        public static async Task<string> AskAsync(string prompt, CancellationToken ct)
        {
#if ANDROID
            // If a previous (possibly abandoned/slow) inference still holds the
            // engine, don't queue behind it for 45s -- tell the user instantly.
            // Two inferences can never run at once, which keeps the engine safe.
            if (_lock.CurrentCount == 0)
                return BusyMessage;

            var work = Task.Run(async () =>
            {
                // Lock acquired inside the task: an abandoned (timed-out) call
                // keeps holding it until the engine really finishes, so the
                // next question can't start a second overlapping inference.
                await _lock.WaitAsync(CancellationToken.None);
                try { return Com.Novasaur.NovaSaurModule.Ask(prompt); }
                finally { _lock.Release(); }
            }, CancellationToken.None);

            var finished = await Task.WhenAny(work, Task.Delay(AnswerTimeout, ct));

            // Whatever happened, the engine gets reloaded before the next
            // question — fresh answers every time, and a hung call recovers.
            ScheduleReset();

            if (finished != work)
            {
                // Swallow the abandoned task's eventual result/exception.
                _ = work.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                return TimeoutMessage;
            }

            string? raw;
            try { raw = await work; }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur ask: " + ex);
                return ErrorMessage;
            }

            if (raw == null) return ErrorMessage;
            if (raw.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur bridge: " + raw);
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

        // Streams the answer token by token, ChatGPT-style — the first words
        // appear in about the time the old blocking call took to *start*.
        // Same one-at-a-time locking as AskAsync; a sliding inactivity window
        // replaces the flat timeout, so long answers aren't cut off while
        // they're still visibly typing.
        public static async Task<string> AskStreamAsync(string prompt, Action<string> onToken, CancellationToken ct)
        {
#if ANDROID
            if (_lock.CurrentCount == 0)
                return BusyMessage;

            var sb = new System.Text.StringBuilder();
            var done = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            long lastActivity = Environment.TickCount64;

            var work = Task.Run(async () =>
            {
                await _lock.WaitAsync(CancellationToken.None);
                try
                {
                    var relay = new StreamRelay(
                        token => { lock (sb) sb.Append(token); lastActivity = Environment.TickCount64; onToken(token); },
                        () => { string full; lock (sb) full = sb.ToString(); done.TrySetResult(full); },
                        err => done.TrySetResult("ERROR:" + err));
                    Com.Novasaur.NovaSaurModule.AskStream(prompt, relay);
                    // hold the lock until the native side reports done (or a
                    // hard cap, so a silent native failure can't wedge us)
                    var finished = await Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromMinutes(3)));
                    return finished == done.Task ? await done.Task : "ERROR:stream never completed";
                }
                finally { _lock.Release(); }
            }, CancellationToken.None);

            // watch from the outside: generous while tokens are flowing,
            // impatient when nothing is happening
            while (!work.IsCompleted)
            {
                await Task.Delay(400, CancellationToken.None);
                long idleMs = Environment.TickCount64 - lastActivity;
                bool started; lock (sb) started = sb.Length > 0;
                if ((!started && idleMs > 30_000) || (started && idleMs > 20_000))
                {
                    ScheduleReset();   // recover the engine behind the scenes
                    return TimeoutMessage;
                }
            }

            // Reload before the next question — every answer starts fresh.
            ScheduleReset();

            string raw;
            try { raw = await work; }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur stream: " + ex);
                return ErrorMessage;
            }
            if (raw.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur stream: " + raw);
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
            "That one's taking me a while to think through. Give me a few seconds to catch my breath, then try asking it a shorter way!";
        public const string ErrorMessage =
            "Something went sideways answering that. Give it another try in a moment.";
        public const string BusyMessage =
            "I'm still finishing your last question — give me a few seconds, then ask away!";
    }
}
