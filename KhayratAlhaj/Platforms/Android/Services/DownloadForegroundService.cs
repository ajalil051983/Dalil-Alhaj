using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;

namespace KhayratAlhaj.Platforms.Android.Services
{
    /// <summary>
    /// Foreground service that keeps a long-running download alive when the app is
    /// backgrounded. On Android 14+ a dataSync foreground-service type plus a partial
    /// wake lock are required so the OS does not suspend networking mid-download.
    /// The service only signals "stay awake"; the actual download still runs on the
    /// managed HttpClient. Stops itself once released.
    /// </summary>
    [Service(
        Name = "com.ilafalkhayr.zadalhaj.DownloadForegroundService",
        Exported = false,
        ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeDataSync)]
    public class DownloadForegroundService : Service
    {
        public const int NotificationId = 4201;
        private const string ChannelId = "quran_download_channel";
        private PowerManager.WakeLock? wakeLock;

        public static void Start(Context context, string message)
        {
            var intent = new Intent(context, typeof(DownloadForegroundService));
            intent.PutExtra("message", message);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }

        public static void Stop(Context context)
        {
            context.StopService(new Intent(context, typeof(DownloadForegroundService)));
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            var message = intent?.GetStringExtra("message") ?? "جاري تنزيل حزمة المصحف...";
            EnsureChannel();
            StartForegroundWithNotification(message);
            AcquireWakeLock();
            return StartCommandResult.Sticky;
        }

        private void EnsureChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }

            var channel = new NotificationChannel(ChannelId, "تنزيل المصحف", NotificationImportance.Low)
            {
                Description = "إشعار استمرار تنزيل صفحات المصحف"
            };
            var manager = (NotificationManager?)GetSystemService(NotificationService);
            manager?.CreateNotificationChannel(channel);
        }

        private void StartForegroundWithNotification(string message)
        {
            var notification = new Notification.Builder(this, ChannelId)
                .SetContentTitle("خيرات الحج")
                .SetContentText(message)
                .SetSmallIcon(global::Android.Resource.Drawable.StatSysDownload)
                .SetOngoing(true)
                .Build();

            // The typed StartForeground overload requires API 29+ (and TypeDataSync is
            // only honoured on API 34+); on older versions the plain overload is used.
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                StartForeground(NotificationId, notification, ForegroundService.TypeDataSync);
            }
            else
            {
                StartForeground(NotificationId, notification);
            }
        }

        private void AcquireWakeLock()
        {
            try
            {
                var powerManager = (PowerManager?)GetSystemService(PowerService);
                wakeLock = powerManager?.NewWakeLock(WakeLockFlags.Partial, "khayrat:quran-download");
                wakeLock?.SetReferenceCounted(false);
                wakeLock?.Acquire(60 * 60 * 1000); // 1h safety ceiling; released in OnDestroy.
            }
            catch
            {
                // Wake lock is best-effort; the foreground service already keeps us alive.
            }
        }

        public override void OnDestroy()
        {
            try
            {
                if (wakeLock?.IsHeld == true)
                {
                    wakeLock.Release();
                }
            }
            catch { /* ignore */ }
            wakeLock = null;
            StopForeground(StopForegroundFlags.Remove);
            base.OnDestroy();
        }

        public override IBinder? OnBind(Intent? intent) => null;
    }
}
