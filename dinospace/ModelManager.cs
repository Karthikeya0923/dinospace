using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace dinospace
{
    public enum DownloadState { NotStarted, Downloading, Paused, Completed, Failed }

    public static class ModelManager
    {
        private const string ModelFileName = "NovaSaur.litertlm";
        private const string ModelUrl =
            "https://huggingface.co/Karthikeya0923/NovaSaur/resolve/main/NovaSaur.litertlm";

        public static string ModelPath => Path.Combine(FileSystem.AppDataDirectory, ModelFileName);
        private static string TempPath => ModelPath + ".part";

        public static DownloadState State { get; private set; } = DownloadState.NotStarted;
        public static double Progress { get; private set; } = 0;
        public static event Action Changed;

        private static readonly object _lock = new object();
        private static CancellationTokenSource _cts;
        private static bool _stopAndDelete = false;

        public static bool IsModelDownloaded()
        {
            try
            {
                var fi = new FileInfo(ModelPath);
                return fi.Exists && fi.Length > 100_000_000;
            }
            catch { return false; }
        }

        public static bool HasPartialDownload()
        {
            try { return File.Exists(TempPath); } catch { return false; }
        }

        // Start or resume the download. Safe to call repeatedly.
        public static void Start()
        {
            lock (_lock)
            {
                if (IsModelDownloaded()) { State = DownloadState.Completed; Notify(); return; }
                if (State == DownloadState.Downloading) return;
                State = DownloadState.Downloading;
                _stopAndDelete = false;
                _cts = new CancellationTokenSource();
                var token = _cts.Token;
                _ = Task.Run(() => DownloadLoopAsync(token));
            }
            StartKeepAliveService();
            Notify();
        }

        // Pause: stop downloading but keep the partial file for later resume.
        public static void Pause()
        {
            lock (_lock)
            {
                if (State != DownloadState.Downloading) return;
                _stopAndDelete = false;
                _cts?.Cancel();
                State = DownloadState.Paused;
            }
            Notify();
        }

        // Stop: cancel and delete the partial file (start over next time).
        public static void Stop()
        {
            lock (_lock)
            {
                _stopAndDelete = true;
                _cts?.Cancel();
                State = DownloadState.NotStarted;
                Progress = 0;
            }
            try { if (File.Exists(TempPath)) File.Delete(TempPath); } catch { }
            Notify();
        }

        private static async Task DownloadLoopAsync(CancellationToken token)
        {
            try
            {
                long existing = File.Exists(TempPath) ? new FileInfo(TempPath).Length : 0;

                using var http = new HttpClient();
                http.Timeout = Timeout.InfiniteTimeSpan;
                http.DefaultRequestHeaders.UserAgent.ParseAdd("DinoSpace/1.0");

                var request = new HttpRequestMessage(HttpMethod.Get, ModelUrl);
                if (existing > 0)
                    request.Headers.Range = new RangeHeaderValue(existing, null);

                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

                bool resuming = response.StatusCode == HttpStatusCode.PartialContent;
                if (!resuming && existing > 0)
                {
                    existing = 0;
                    try { File.Delete(TempPath); } catch { }
                }
                response.EnsureSuccessStatusCode();

                long total = (response.Content.Headers.ContentLength ?? 0) + (resuming ? existing : 0);

                using var input = await response.Content.ReadAsStreamAsync(token);
                using var output = new FileStream(TempPath, resuming ? FileMode.Append : FileMode.Create, FileAccess.Write);

                var buffer = new byte[1 << 20]; // 1 MB
                long done = existing;
                int lastPct = -1;
                int read;
                while ((read = await input.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    await output.WriteAsync(buffer, 0, read, token);
                    await output.FlushAsync(token);   // keep the .part consistent so resume is safe
                    done += read;
                    if (total > 0)
                    {
                        Progress = Math.Min(1.0, (double)done / total);
                        int pct = (int)(Progress * 100);
                        if (pct != lastPct) { lastPct = pct; Notify(); }
                    }
                }

                output.Dispose();

                if (File.Exists(ModelPath)) File.Delete(ModelPath);
                File.Move(TempPath, ModelPath);

                Progress = 1.0;
                State = DownloadState.Completed;
                Notify();
            }
            catch (OperationCanceledException)
            {
                // Paused keeps the .part; Stop already deleted it. State was set by the caller.
                if (_stopAndDelete)
                {
                    try { if (File.Exists(TempPath)) File.Delete(TempPath); } catch { }
                }
                Notify();
            }
            catch
            {
                State = DownloadState.Failed;   // keep the .part so retry resumes
                Notify();
            }
        }

        private static void StartKeepAliveService()
        {
#if ANDROID
            try
            {
                var ctx = Android.App.Application.Context;
                var intent = new Android.Content.Intent(ctx, typeof(ModelDownloadService));
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                    ctx.StartForegroundService(intent);
                else
                    ctx.StartService(intent);
            }
            catch { }
#endif
        }

        private static void Notify()
        {
            try { Changed?.Invoke(); } catch { }
        }
    }
}