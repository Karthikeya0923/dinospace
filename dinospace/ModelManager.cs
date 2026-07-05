using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace dinospace
{
    public enum DownloadState { NotStarted, Downloading, Paused, Completed, Failed }

    // Gets the NovaSaur model onto the device, in order of preference:
    //
    //  1. BUNDLED (the normal path for Play Store installs): the model ships
    //     inside the app as Play asset packs. Google Play downloads them
    //     automatically at install time; on first open this class joins the
    //     chunks into the real model file in about a minute, then tells Play
    //     to free the packs. No in-app download, nothing for the user to do.
    //
    //  2. DOWNLOAD (fallback for dev builds / sideloads where the asset
    //     packs aren't present): the original resumable download from
    //     Hugging Face, with auto-retry and size verification.
    public static class ModelManager
    {
        private const string ModelFileName = "NovaSaur.litertlm";
        private const string ModelUrl =
            "https://huggingface.co/Karthikeya0923/NovaSaur/resolve/main/NovaSaur.litertlm";

        // Free space the preflight asks for: the ~3 GB model plus a margin.
        public const long RequiredFreeBytes = 3_600_000_000;

        private const int MaxAttempts = 5; // automatic retries on flaky connections

        public static string ModelPath => Path.Combine(FileSystem.AppDataDirectory, ModelFileName);
        private static string TempPath => ModelPath + ".part";

        public static DownloadState State { get; private set; } = DownloadState.NotStarted;
        public static double Progress { get; private set; } = 0;

        // True while assembling the bundled model locally (vs downloading).
        // The UI uses this to say "Setting up" instead of "Downloading".
        public static bool IsLocalInstall { get; private set; } = false;

        // Byte counts so the UI can show "1.3 of 3.1 GB".
        public static long TotalBytes { get; private set; } = 0;
        public static long DoneBytes { get; private set; } = 0;

        public static event Action? Changed;

        private static readonly object _lock = new object();
        private static CancellationTokenSource? _cts;
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

        public static long GetModelSizeBytes()
        {
            try { var fi = new FileInfo(ModelPath); return fi.Exists ? fi.Length : 0; }
            catch { return 0; }
        }

        public static long GetPartialSizeBytes()
        {
            try { var fi = new FileInfo(TempPath); return fi.Exists ? fi.Length : 0; }
            catch { return 0; }
        }

        // Free space where the model lives. Returns -1 if it can't be measured.
        public static long GetFreeSpaceBytes()
        {
#if ANDROID
            try
            {
                var stat = new Android.OS.StatFs(FileSystem.AppDataDirectory);
                return stat.AvailableBytes;
            }
            catch { return -1; }
#else
            return -1;
#endif
        }

        // Deletes the downloaded model (and any partial file) to free space.
        // Refuses while a download is running. If NovaSaur was already loaded
        // in this session it keeps working until the app restarts.
        public static bool DeleteModel()
        {
            lock (_lock)
            {
                if (State == DownloadState.Downloading) return false;
            }
            try
            {
                if (File.Exists(ModelPath)) File.Delete(ModelPath);
                if (File.Exists(TempPath)) File.Delete(TempPath);
                State = DownloadState.NotStarted;
                Progress = 0;
                TotalBytes = 0;
                DoneBytes = 0;
                Notify();
                return true;
            }
            catch { return false; }
        }

        // ============================================================
        //  PATH 1: BUNDLED MODEL (Play asset packs)
        // ============================================================

        private class ChunkSet
        {
            public List<string> Files = new List<string>(); // ordered part1..partN
            public long TotalSize = 0;
        }

        // Finds the delivered chunks and checks they form a complete set:
        // part numbers 1..N with no gaps. Returns null while packs are still
        // arriving (or were never shipped, e.g. a dev sideload).
        private static ChunkSet? FindCompleteChunkSet()
        {
            var found = new SortedDictionary<int, string>();
            foreach (var file in PlayAssetPacks.FindDeliveredChunkFiles())
            {
                string name = Path.GetFileName(file);
                int idx = name.LastIndexOf(".part", StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;
                int number;
                if (!int.TryParse(name.Substring(idx + 5), out number)) continue;
                if (!found.ContainsKey(number)) found[number] = file;
            }

            if (found.Count == 0) return null;

            // Chunks must be a gap-free run starting at part1.
            int expected = 1;
            var set = new ChunkSet();
            foreach (var pair in found)
            {
                if (pair.Key != expected) return null; // a pack hasn't arrived yet
                set.Files.Add(pair.Value);
                try { set.TotalSize += new FileInfo(pair.Value).Length; } catch { return null; }
                expected++;
            }

            // Every chunk except the last should be a full-size chunk. If the
            // last-found chunk is full-size, a higher-numbered pack may still
            // be on its way, so wait for it.
            if (set.Files.Count >= 1)
            {
                long first = new FileInfo(set.Files[0]).Length;
                long last = new FileInfo(set.Files[set.Files.Count - 1]).Length;
                if (set.Files.Count > 1)
                {
                    for (int i = 0; i < set.Files.Count - 1; i++)
                        if (new FileInfo(set.Files[i]).Length != first) return null;
                }
                if (last == first && set.TotalSize < 2_500_000_000)
                    return null; // suspicious: looks truncated, wait for more packs
            }

            if (set.TotalSize < 100_000_000) return null;
            return set;
        }

        // How many chunk files Play has delivered so far (0 = none yet).
        // Lets the UI say "Google Play is still delivering NovaSaur".
        public static int BundledPartsFound()
        {
            try { return PlayAssetPacks.FindDeliveredChunkFiles().Count; }
            catch { return 0; }
        }

        // If the full set of bundled chunks is on the device, start joining
        // them into the real model file. Returns true if the install started.
        public static bool TryBeginBundledInstall()
        {
            ChunkSet? set;
            lock (_lock)
            {
                if (IsModelDownloaded()) { State = DownloadState.Completed; Notify(); return false; }
                if (State == DownloadState.Downloading) return IsLocalInstall;

                set = FindCompleteChunkSet();
                if (set == null) return false;

                // Joining needs room for the assembled copy.
                long free = GetFreeSpaceBytes();
                if (free >= 0 && free < set.TotalSize + 300_000_000) return false;

                State = DownloadState.Downloading;
                IsLocalInstall = true;
                _stopAndDelete = false;
                _cts = new CancellationTokenSource();
                TotalBytes = set.TotalSize;
                DoneBytes = 0;
                Progress = 0;
                _ = Task.Run(() => InstallFromChunksAsync(set));
            }
            Notify();
            return true;
        }

        private static async Task InstallFromChunksAsync(ChunkSet set)
        {
            try
            {
                using (var output = new FileStream(TempPath, FileMode.Create, FileAccess.Write))
                {
                    var buffer = new byte[1 << 22]; // 4 MB
                    long done = 0;
                    int lastPct = -1;

                    foreach (var chunk in set.Files)
                    {
                        using var input = File.OpenRead(chunk);
                        int read;
                        while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await output.WriteAsync(buffer, 0, read);
                            done += read;
                            DoneBytes = done;
                            Progress = Math.Min(1.0, (double)done / set.TotalSize);
                            int pct = (int)(Progress * 100);
                            if (pct != lastPct) { lastPct = pct; Notify(); }
                        }
                    }
                }

                if (new FileInfo(TempPath).Length != set.TotalSize)
                    throw new IOException("Assembled model has the wrong size.");

                if (File.Exists(ModelPath)) File.Delete(ModelPath);
                File.Move(TempPath, ModelPath);

                // The packs are now duplicate data - let Play delete them so
                // the app doesn't take double the storage.
                PlayAssetPacks.RemoveDeliveredPacks();

                Progress = 1.0;
                DoneBytes = TotalBytes = GetModelSizeBytes();
                IsLocalInstall = false;
                State = DownloadState.Completed;
                Notify();
            }
            catch
            {
                try { if (File.Exists(TempPath)) File.Delete(TempPath); } catch { }
                IsLocalInstall = false;
                State = DownloadState.NotStarted; // chunks are still there; retried on next open
                Progress = 0;
                DoneBytes = 0;
                Notify();
            }
        }

        // ============================================================
        //  PATH 2: DIRECT DOWNLOAD (fallback)
        // ============================================================

        // Start or resume the download. Safe to call repeatedly.
        public static void Start()
        {
            lock (_lock)
            {
                if (IsModelDownloaded()) { State = DownloadState.Completed; Notify(); return; }
                if (State == DownloadState.Downloading) return;
                State = DownloadState.Downloading;
                IsLocalInstall = false;
                _stopAndDelete = false;
                _cts = new CancellationTokenSource();
                var token = _cts.Token;
                _ = Task.Run(() => DownloadLoopAsync(token));
            }
            Notify();
        }

        // Pause: stop downloading but keep the partial file for later resume.
        public static void Pause()
        {
            lock (_lock)
            {
                if (State != DownloadState.Downloading || IsLocalInstall) return;
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
                if (IsLocalInstall) return; // the local join takes ~a minute; let it finish
                _stopAndDelete = true;
                _cts?.Cancel();
                State = DownloadState.NotStarted;
                Progress = 0;
                TotalBytes = 0;
                DoneBytes = 0;
            }
            try { if (File.Exists(TempPath)) File.Delete(TempPath); } catch { }
            Notify();
        }

        private static async Task DownloadLoopAsync(CancellationToken token)
        {
            int attempt = 0;
            while (true)
            {
                attempt++;
                try
                {
                    // Everything (including the Range header) is recomputed per
                    // attempt, so each retry resumes from the bytes already saved.
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
                        // Server ignored the Range header: start clean.
                        existing = 0;
                        try { File.Delete(TempPath); } catch { }
                    }
                    response.EnsureSuccessStatusCode();

                    long total = (response.Content.Headers.ContentLength ?? 0) + (resuming ? existing : 0);
                    TotalBytes = total;
                    DoneBytes = existing;

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
                        DoneBytes = done;
                        if (total > 0)
                        {
                            Progress = Math.Min(1.0, (double)done / total);
                            int pct = (int)(Progress * 100);
                            if (pct != lastPct) { lastPct = pct; Notify(); }
                        }
                    }

                    output.Dispose();

                    // The connection can end early without an exception. Never
                    // promote a short file to "the model" - keep the .part and retry.
                    if (total > 0 && new FileInfo(TempPath).Length < total)
                        throw new IOException("Download ended before the file was complete.");

                    if (File.Exists(ModelPath)) File.Delete(ModelPath);
                    File.Move(TempPath, ModelPath);

                    Progress = 1.0;
                    DoneBytes = TotalBytes = GetModelSizeBytes();
                    State = DownloadState.Completed;
                    Notify();
                    return;
                }
                catch (OperationCanceledException)
                {
                    // Paused keeps the .part; Stop already deleted it. State was set by the caller.
                    if (_stopAndDelete)
                    {
                        try { if (File.Exists(TempPath)) File.Delete(TempPath); } catch { }
                    }
                    Notify();
                    return;
                }
                catch
                {
                    if (attempt >= MaxAttempts || token.IsCancellationRequested)
                    {
                        State = DownloadState.Failed;   // keep the .part so retry resumes
                        Notify();
                        return;
                    }
                    // Transient failure: wait a little longer each time, then resume.
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3 * attempt), token);
                }
                catch (OperationCanceledException)
                {
                    if (_stopAndDelete)
                    {
                        try { if (File.Exists(TempPath)) File.Delete(TempPath); } catch { }
                    }
                    Notify();
                    return;
                }
            }
        }

        private static void Notify()
        {
            try { Changed?.Invoke(); } catch { }
        }
    }
}
