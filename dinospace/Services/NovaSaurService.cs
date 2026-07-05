using System;
using System.Threading;
using System.Threading.Tasks;

namespace dinospace.Services
{
    // Thin, safe wrapper around the on-device NovaSaur engine (the bound
    // novasaur.aar).
    //
    // Reliability model (learned the hard way): the engine supports exactly
    // ONE inference at a time, and starting a new conversation while an old
    // one is still generating wedges it permanently. So we use the blocking
    // Ask() call, and the lock is held INSIDE the worker task — if a call
    // times out for the UI, the abandoned task keeps holding the lock until
    // the native engine actually finishes, so two inferences can never
    // overlap. The UI gets a friendly timeout; the engine stays healthy.
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
                if (_initTask == null || _initTask.IsFaulted)
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

        public const string TimeoutMessage =
            "That one's taking me a while to think through. Give me a few seconds to catch my breath, then try asking it a shorter way!";
        public const string ErrorMessage =
            "Something went sideways answering that. Give it another try in a moment.";
        public const string BusyMessage =
            "I'm still finishing your last question — give me a few seconds, then ask away!";
    }
}
