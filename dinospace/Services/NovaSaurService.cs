using System;
using System.Threading;
using System.Threading.Tasks;

namespace dinospace.Services
{
    // Thin, safe wrapper around the on-device NovaSaur engine (the bound
    // novasaur.aar).
    //
    // Reliability model (this is the part that kept biting us):
    //
    //  1. ONE inference at a time. Every request queues on a single lock, so
    //     two inferences — or an inference and an engine reload — can never
    //     overlap and wedge the native engine.
    //
    //  2. RESET IN THE BACKGROUND, AFTER an answer, never before. The small
    //     model can't carry state between questions, so the engine is reloaded
    //     to a clean slate after every answer. Crucially this runs on a
    //     background task while the user is reading the answer and typing the
    //     next one, so the reload cost is hidden. The next question just waits
    //     its turn on the lock.
    //
    //  3. NOTHING CAN HANG. The watcher bounds the time spent waiting for the
    //     engine (a slow reload, a busy lock) BEFORE inference starts, as well
    //     as the silence AFTER it starts. Either way the UI always gets a
    //     friendly reply within a fixed budget — an earlier version only timed
    //     out once inference had begun, so a slow reload showed "thinking…"
    //     forever. That was the bug behind the endless spinner.
    public static class NovaSaurService
    {
        private static readonly SemaphoreSlim _lock = new(1, 1);

        // Time budgets, deliberately tight so the chat never feels stuck.
        // A healthy warm engine produces its first token in seconds; when it
        // hasn't after this long it's wedged, and the chat swaps in the
        // offline answer instead — so these caps bound how long a child can
        // ever stare at "thinking…", not how long an answer may take.
        private static readonly TimeSpan QueueCap = TimeSpan.FromSeconds(35);   // waiting for the engine (reload/other answer)
        private static readonly TimeSpan FirstTokenCap = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan QuietCap = TimeSpan.FromSeconds(15);   // silence after tokens started

        // Two stream failures in a row = the engine is not trustworthy this
        // session; every question answers from the instant offline brain
        // until a warm-up proves the engine healthy again.
        private static int _streamStrikes;

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

        // The engine only counts as usable once it has actually produced a
        // token. LiteRT-LM "initializes" in milliseconds because it loads the
        // model weights lazily on the FIRST inference — so right after init
        // (and after every per-answer reset) the first answer pays a cold
        // model load that can take a minute on a phone CPU. Routing questions
        // at a cold engine is what showed "thinking…" until the timeout on
        // every single question. The warm-up below absorbs that load in the
        // background; until it finishes, questions take the instant offline
        // path instead.
        private static volatile bool _warm;
        private static int _warmPending;
        public static bool IsWarm => IsReady && _warm && Volatile.Read(ref _streamStrikes) < 2;

        private static void Log(string msg)
        {
#if ANDROID
            try { Android.Util.Log.Info("NovaSvc", msg); } catch { }
#endif
            System.Diagnostics.Debug.WriteLine("NovaSvc: " + msg);
        }

        private static void ScheduleWarmUp()
        {
#if ANDROID
            if (Interlocked.Exchange(ref _warmPending, 1) == 1) return;
            _ = Task.Run(async () =>
            {
                await _lock.WaitAsync(CancellationToken.None);
                try
                {
                    Interlocked.Exchange(ref _warmPending, 0);
                    WarmUpLocked();
                }
                finally { _lock.Release(); }
            });
#endif
        }

#if ANDROID
        // Runs one tiny inference to force the lazy model load. Must be called
        // while holding _lock.
        private static void WarmUpLocked()
        {
            if (_warm || !IsReady) return;
            long t0 = Environment.TickCount64;
            try { Android.Util.Log.Info("NovaSaur", "warm-up inference starting"); } catch { }
            string? r = null;
            try { r = Com.Novasaur.NovaSaurModule.Ask("Say OK."); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Nova warm-up: " + ex); }
            bool ok = r != null && !r.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase);
            if (ok) _warm = true;
            try { Android.Util.Log.Info("NovaSaur", $"warm-up {(ok ? "done" : "failed")} in {(Environment.TickCount64 - t0) / 1000.0:0.0}s"); } catch { }
        }
#endif

        private static Task? _initTask;
        private static readonly object _initGate = new();
        private static int _autoInitHooked;

        // One-time hook: the moment an in-app model download completes, load
        // the model — so the very next question streams from it, no restart.
        public static void EnsureAutoInit()
        {
            if (Interlocked.Exchange(ref _autoInitHooked, 1) == 1) return;
            ModelManager.Changed += () =>
            {
                if (ModelManager.State == DownloadState.Completed && !IsReady)
                    _ = InitAsync();
            };
        }

        // Loads the model into memory. Single-flight: every caller shares one
        // native Init — running two at once (background warm-up + on-demand
        // load) can wedge the engine, which showed up as an endless
        // "thinking…" on the second question.
        public static Task InitAsync()
        {
#if ANDROID
            lock (_initGate)
            {
                bool stale = _initTask != null && _initTask.IsCompleted && !IsReady;
                if (_initTask == null || _initTask.IsFaulted || stale)
                    _initTask = Task.Run(() =>
                    {
                        if (!Com.Novasaur.NovaSaurModule.IsReady)
                        {
                            var ctx = Android.App.Application.Context;
                            Com.Novasaur.NovaSaurModule.Init(ctx);
                        }
                        // Init is fast (the weights load lazily) — the warm-up
                        // is what actually gets the model into memory.
                        ScheduleWarmUp();
                    });
                return _initTask;
            }
#else
            return Task.CompletedTask;
#endif
        }

#if ANDROID
        private static int _resetPending;

        // Reload the engine to a clean slate, in the background, after an
        // answer. Holds the lock while it runs so the next question simply
        // waits its turn; if it fails the next Init recovers. Only one reload
        // ever queues — a reload takes tens of seconds on a slow phone, and
        // stacking them kept the engine busy long enough that every following
        // question timed out in a row.
        private static void ScheduleReset()
        {
            // The engine is cold from this moment until the post-reset warm-up
            // finishes — questions in that window answer offline instantly.
            _warm = false;
            if (Interlocked.Exchange(ref _resetPending, 1) == 1) return;
            _ = Task.Run(async () =>
            {
                await _lock.WaitAsync(CancellationToken.None);
                try
                {
                    Interlocked.Exchange(ref _resetPending, 0);
                    Com.Novasaur.NovaSaurModule.Reset();
                    WarmUpLocked();   // absorb the lazy reload now, not on the next question
                }
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

        // Blocking one-shot answer (kept for completeness; the chat streams).
        public static async Task<string> AskAsync(string prompt, CancellationToken ct)
        {
#if ANDROID
            long inferenceStart = 0;
            long callStart = Environment.TickCount64;
            int abandoned = 0;   // 1 = the caller gave up while we were still queued

            var work = Task.Run(async () =>
            {
                await _lock.WaitAsync(CancellationToken.None);
                try
                {
                    if (Interlocked.CompareExchange(ref abandoned, 0, 0) == 1) return (string?)"ERROR:abandoned";
                    inferenceStart = Environment.TickCount64;
                    return (string?)Com.Novasaur.NovaSaurModule.Ask(prompt);
                }
                finally { _lock.Release(); }
            }, CancellationToken.None);

            while (!work.IsCompleted)
            {
                await Task.Delay(300, CancellationToken.None);
                if (inferenceStart == 0)
                {
                    // Queue timeout: the engine was never touched, so no reset.
                    if (Environment.TickCount64 - callStart > QueueCap.TotalMilliseconds)
                    {
                        Interlocked.Exchange(ref abandoned, 1);
                        return TimeoutMessage;
                    }
                    continue;
                }
                if (Environment.TickCount64 - inferenceStart > FirstTokenCap.TotalMilliseconds) { ScheduleReset(); return TimeoutMessage; }
            }

            string? raw;
            try { raw = await work; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("NovaSaur ask: " + ex); ScheduleReset(); return ErrorMessage; }

            ScheduleReset();   // fresh engine for the next question
            if (raw == null || raw.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase)) return ErrorMessage;
            string cleaned = PromptBuilder.Clean(raw);
            if (string.IsNullOrWhiteSpace(cleaned)) return ErrorMessage;
            return NovaGuard.CheckAnswer(cleaned) ?? cleaned;
#else
            await Task.CompletedTask;
            return "Nova runs on Android right now.";
#endif
        }

        // Streams the answer token by token. The watcher bounds BOTH the wait
        // for the engine (before any token) and the silence after tokens start,
        // so the chat can never hang no matter how slow a reload is.
        public static async Task<string> AskStreamAsync(string prompt, Action<string> onToken, CancellationToken ct)
        {
#if ANDROID
            var sb = new System.Text.StringBuilder();
            var done = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            long lastActivity = 0;
            bool inferenceStarted = false;
            long callStart = Environment.TickCount64;
            int abandoned = 0;   // 1 = the caller gave up while we were still queued

            var work = Task.Run(async () =>
            {
                await _lock.WaitAsync(CancellationToken.None);
                try
                {
                    // The caller may have timed out while we sat in the queue —
                    // don't run a stale inference nobody is waiting for, it
                    // would only delay the user's NEXT question.
                    if (Interlocked.CompareExchange(ref abandoned, 0, 0) == 1) return "ERROR:abandoned";

                    // The lock is ours — the engine is idle and reset. Inference
                    // starts NOW, so the idle clock starts here (queue/reload
                    // time is not counted against the answer).
                    lastActivity = Environment.TickCount64;
                    inferenceStarted = true;

                    var relay = new StreamRelay(
                        token => { lock (sb) sb.Append(token); lastActivity = Environment.TickCount64; onToken(token); },
                        () => { string full; lock (sb) full = sb.ToString(); done.TrySetResult(full); },
                        err => done.TrySetResult("ERROR:" + err));
                    Com.Novasaur.NovaSaurModule.AskStream(prompt, relay);
                    var finished = await Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromMinutes(2)));
                    return finished == done.Task ? await done.Task : "ERROR:stream never completed";
                }
                finally { _lock.Release(); }
            }, CancellationToken.None);

            while (!work.IsCompleted)
            {
                await Task.Delay(350, CancellationToken.None);
                if (!inferenceStarted)
                {
                    // Still waiting for the engine (a background reload or the
                    // previous answer). Bound that wait so it can't hang. The
                    // engine was never touched, so no reset is needed — queuing
                    // one here just made the jam longer.
                    if (Environment.TickCount64 - callStart > QueueCap.TotalMilliseconds)
                    {
                        Interlocked.Exchange(ref abandoned, 1);
                        Log($"queue timeout after {(Environment.TickCount64 - callStart) / 1000}s");
                        return TimeoutMessage;
                    }
                    continue;
                }
                long idleMs = Environment.TickCount64 - lastActivity;
                bool started; lock (sb) started = sb.Length > 0;
                if ((!started && idleMs > FirstTokenCap.TotalMilliseconds) || (started && idleMs > QuietCap.TotalMilliseconds))
                {
                    Interlocked.Increment(ref _streamStrikes);
                    Log($"stream {(started ? "stalled mid-answer" : "produced no tokens")} after {idleMs / 1000}s (strike {Volatile.Read(ref _streamStrikes)})");
                    ScheduleReset();

                    // A stall after real progress isn't a loss: keep what the
                    // model wrote, trimmed to its last complete sentence.
                    if (started)
                    {
                        string partial; lock (sb) partial = sb.ToString();
                        string salvage = PromptBuilder.Clean(partial);
                        int lastStop = Math.Max(salvage.LastIndexOf('.'), Math.Max(salvage.LastIndexOf('!'), salvage.LastIndexOf('?')));
                        if (lastStop >= 80)
                        {
                            salvage = salvage[..(lastStop + 1)];
                            var guarded = NovaGuard.CheckAnswer(salvage) ?? salvage;
                            Log($"salvaged {guarded.Length} chars from the stalled stream");
                            return guarded;
                        }
                    }
                    return TimeoutMessage;
                }
            }

            string raw;
            try { raw = await work; }
            catch (Exception ex) { Log("stream threw: " + ex.Message); Interlocked.Increment(ref _streamStrikes); ScheduleReset(); return ErrorMessage; }

            ScheduleReset();   // reload in the background for a clean next question
            if (raw.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase)) { Log("stream error: " + raw); Interlocked.Increment(ref _streamStrikes); return ErrorMessage; }
            string cleaned = PromptBuilder.Clean(raw);
            if (string.IsNullOrWhiteSpace(cleaned)) { Log("stream returned empty text"); return ErrorMessage; }
            Interlocked.Exchange(ref _streamStrikes, 0);
            Log($"stream ok ({cleaned.Length} chars)");
            return NovaGuard.CheckAnswer(cleaned) ?? cleaned;
#else
            await Task.CompletedTask;
            return "Nova runs on Android right now.";
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
            "That answer took longer than it should have, so I stopped it. Give it a moment and ask again — shorter questions help.";
        public const string ErrorMessage =
            "Something went sideways answering that. Give it another try in a moment.";
        public const string BusyMessage =
            "I'm still finishing your last question — give me a few seconds, then ask away!";
    }
}
