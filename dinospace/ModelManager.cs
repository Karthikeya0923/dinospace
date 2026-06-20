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
    public enum DownloadState { NotStarted, Downloading, Completed, Failed }

    public static class ModelManager
    {
        private const string ModelFileName = "NovaSaur.litertlm";
        private const string ModelUrl =
            "https://huggingface.co/Karthikeya0923/NovaSaur/resolve/main/NovaSaur.litertlm";

        public static string ModelPath => Path.Combine(FileSystem.AppDataDirectory, ModelFileName);
        private static string TempPath => ModelPath + ".part";

        public static DownloadState State { get; private set; } = DownloadState.NotStarted;
        public static double Progress { get; private set; } = 0;   // 0.0 - 1.0
        public static event Action Changed;                        // fires on progress/state changes

        private static readonly object _lock = new object();

        public static bool IsModelDownloaded()
        {
            try
            {
                var fi = new FileInfo(ModelPath);
                return fi.Exists && fi.Length > 100_000_000;
            }
            catch { return false; }
        }

        // Starts the download in the background. Safe to call repeatedly.
        public static void Start()
        {
            lock (_lock)
            {
                if (IsModelDownloaded()) { State = DownloadState.Completed; Notify(); return; }
                if (State == DownloadState.Downloading) return;   // already running
                State = DownloadState.Downloading;
                Progress = 0;
                _ = Task.Run(DownloadLoopAsync);
            }
            Notify();
        }

        private static async Task DownloadLoopAsync()
        {
            try
            {
                long existing = File.Exists(TempPath) ? new FileInfo(TempPath).Length : 0;

                using var http = new HttpClient();
                http.Timeout = Timeout.InfiniteTimeSpan;
                http.DefaultRequestHeaders.UserAgent.ParseAdd("DinoSpace/1.0");

                var request = new HttpRequestMessage(HttpMethod.Get, ModelUrl);
                if (existing > 0)
                    request.Headers.Range = new RangeHeaderValue(existing, null);   // resume

                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                bool resuming = response.StatusCode == HttpStatusCode.PartialContent;
                if (!resuming && existing > 0)
                {
                    existing = 0;
                    try { File.Delete(TempPath); } catch { }
                }
                response.EnsureSuccessStatusCode();

                long total = (response.Content.Headers.ContentLength ?? 0) + (resuming ? existing : 0);

                using var input = await response.Content.ReadAsStreamAsync();
                using var output = new FileStream(TempPath,
                    resuming ? FileMode.Append : FileMode.Create, FileAccess.Write);

                var buffer = new byte[1 << 20]; // 1 MB
                long done = existing;
                int lastPct = -1;
                int read;
                while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await output.WriteAsync(buffer, 0, read);
                    done += read;
                    if (total > 0)
                    {
                        Progress = Math.Min(1.0, (double)done / total);
                        int pct = (int)(Progress * 100);
                        if (pct != lastPct) { lastPct = pct; Notify(); }
                    }
                }

                await output.FlushAsync();
                output.Dispose();

                if (File.Exists(ModelPath)) File.Delete(ModelPath);
                File.Move(TempPath, ModelPath);

                Progress = 1.0;
                State = DownloadState.Completed;
                Notify();
            }
            catch
            {
                // Keep the .part file so a retry resumes instead of restarting.
                State = DownloadState.Failed;
                Notify();
            }
        }

        private static void Notify()
        {
            try { Changed?.Invoke(); } catch { }
        }
    }
}