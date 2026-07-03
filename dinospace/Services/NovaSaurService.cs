using System;
using System.Threading;
using System.Threading.Tasks;

namespace dinospace.Services
{
    // Thin, safe wrapper around the on-device NovaSaur engine (the bound
    // novasaur.aar). Serializes inference so two prompts never overlap, and
    // isolates all the #if ANDROID plumbing in one place so the UI stays clean.
    public static class NovaSaurService
    {
        private static readonly SemaphoreSlim _lock = new(1, 1);

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

        // Loads the model into memory. Safe to call more than once; blocks
        // until ready, so call it off the UI thread.
        public static Task InitAsync()
        {
#if ANDROID
            return Task.Run(() =>
            {
                if (Com.Novasaur.NovaSaurModule.IsReady) return;
                var ctx = Android.App.Application.Context;
                Com.Novasaur.NovaSaurModule.Init(ctx);
            });
#else
            return Task.CompletedTask;
#endif
        }

        // Runs one prompt to completion and returns the cleaned answer, or a
        // friendly message on timeout/failure. onStarted fires once the model
        // actually begins (after any queue wait).
        public static async Task<string> AskAsync(string prompt, TimeSpan timeout, CancellationToken ct)
        {
#if ANDROID
            var work = Task.Run(async () =>
            {
                await _lock.WaitAsync(ct);
                try { return Com.Novasaur.NovaSaurModule.Ask(prompt); }
                finally { _lock.Release(); }
            }, ct);

            var finished = await Task.WhenAny(work, Task.Delay(timeout, ct));
            if (finished != work)
            {
                // Let the abandoned inference finish and swallow its result so
                // it can't crash later; the lock releases when it completes.
                _ = work.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                return TimeoutMessage;
            }

            string raw = await work;
            if (raw == null) return ErrorMessage;
            if (raw.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine("NovaSaur bridge: " + raw);
                return ErrorMessage;
            }

            string cleaned = PromptBuilder.Clean(raw);
            if (string.IsNullOrWhiteSpace(cleaned)) return ErrorMessage;

            var replaced = NovaGuard.CheckAnswer(cleaned);
            return replaced ?? cleaned;
#else
            await Task.CompletedTask;
            return "NovaSaur runs on Android right now.";
#endif
        }

        public const string TimeoutMessage =
            "That one's taking me a while to think through. Try asking it in a shorter or simpler way!";
        public const string ErrorMessage =
            "Something went sideways answering that. Give it another try in a moment.";
    }
}
