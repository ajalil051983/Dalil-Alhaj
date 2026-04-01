using Android.App;
using Android.Content;
using KhayratAlhaj.Models.PrayerTimes;
using KhayratAlhaj.Services;
using KhayratAlhaj.Services.PrayerTimes;

namespace KhayratAlhaj
{
    /// <summary>
    /// Reschedules prayer time notifications after the device reboots.
    /// Android clears all scheduled alarms on reboot, so this receiver
    /// recalculates prayer times and re-registers the notifications.
    /// Only runs on actual boot — skips if the MAUI app is already active.
    /// </summary>
    [BroadcastReceiver(Name = "com.ilafalkhayr.zadalhaj.BootReceiver", Enabled = true, Exported = true, DirectBootAware = false)]
    [IntentFilter([Intent.ActionBootCompleted])]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action != Intent.ActionBootCompleted) return;

            // Skip if the MAUI app is already running (it will schedule on its own)
            if (App.Current != null)
            {
                System.Diagnostics.Debug.WriteLine("[BootReceiver] App is already running, skipping");
                return;
            }

            System.Diagnostics.Debug.WriteLine("[BootReceiver] Device rebooted — rescheduling prayer notifications");

            Task.Run(async () =>
            {
                try
                {
                    if (!NotificationService.IsEnabled) return;

                    var prayerService = new PrayerTimeService();
                    var notificationService = new NotificationService();

                    var multiDayTimes = new List<DayPrayerTimes>();
                    for (int i = 0; i < NotificationService.DaysAhead; i++)
                    {
                        var times = await prayerService.GetPrayerTimesAsync(DateTime.Today.AddDays(i));
                        if (times != null)
                            multiDayTimes.Add(times);
                    }

                    if (multiDayTimes.Count > 0)
                    {
                        await notificationService.ScheduleMultiDayNotificationsAsync(multiDayTimes);
                        System.Diagnostics.Debug.WriteLine(
                            $"[BootReceiver] Rescheduled notifications for {multiDayTimes.Count} day(s)");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BootReceiver] Error: {ex.Message}");
                }
            });
        }
    }
}
