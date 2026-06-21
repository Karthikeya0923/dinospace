using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace dinospace
{
    [Service(Exported = false, ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync)]
    public class ModelDownloadService : Service
    {
        const int NotifId = 4242;
        const string ChannelId = "novasaur_download";

        public override IBinder OnBind(Intent intent) => null;

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            CreateChannel();
            StartForeground(NotifId, BuildNotification((int)(ModelManager.Progress * 100)));

            ModelManager.Changed -= OnChanged;
            ModelManager.Changed += OnChanged;

            // If nothing is actually downloading, don't hang around.
            if (ModelManager.State != DownloadState.Downloading)
                StopSelfSafely();

            return StartCommandResult.Sticky;
        }

        void OnChanged()
        {
            if (ModelManager.State == DownloadState.Downloading)
            {
                try
                {
                    NotificationManagerCompat.From(this)
                        .Notify(NotifId, BuildNotification((int)(ModelManager.Progress * 100)));
                }
                catch { }
            }
            else
            {
                StopSelfSafely();
            }
        }

        void StopSelfSafely()
        {
            ModelManager.Changed -= OnChanged;
            try { StopForeground(StopForegroundFlags.Remove); } catch { }
            StopSelf();
        }

        Notification BuildNotification(int pct)
        {
            return new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("Downloading NovaSaur")
                .SetContentText($"{pct}% complete - you can close the app, this keeps going")
                .SetSmallIcon(Android.Resource.Drawable.StatSysDownload)
                .SetProgress(100, pct, false)
                .SetOngoing(true)
                .SetOnlyAlertOnce(true)
                .Build();
        }

        void CreateChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
            var channel = new NotificationChannel(ChannelId, "NovaSaur download", NotificationImportance.Low);
            var mgr = (NotificationManager)GetSystemService(NotificationService);
            mgr?.CreateNotificationChannel(channel);
        }

        public override void OnDestroy()
        {
            ModelManager.Changed -= OnChanged;
            base.OnDestroy();
        }
    }
}