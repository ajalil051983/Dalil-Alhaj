using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using KhayratAlhaj.Models.PrayerTimes;

namespace KhayratAlhaj.Services
{
    /// <summary>
    /// Manages local notifications for prayer times.
    /// Uses Plugin.LocalNotification for Android notification scheduling.
    /// Schedules multiple days ahead so notifications fire even when the app is closed.
    /// </summary>
    public class NotificationService
    {
        private const string NotificationsEnabledKey = "prayer_notifications_enabled";

        /// <summary>
        /// Prevents multiple concurrent scheduling operations that would race against each other.
        /// </summary>
        private static readonly SemaphoreSlim _schedulingSemaphore = new SemaphoreSlim(1, 1);

        // Notification IDs: base + (dayOffset * 10) + prayer type (arrival)
        private const int BaseNotificationId = 1000;
        // Notification IDs: base + (dayOffset * 10) + prayer type (5-min reminder)
        private const int BaseReminderNotificationId = 2000;

        /// <summary>How many days ahead to schedule notifications.</summary>
        public const int DaysAhead = 7;

        /// <summary>How many minutes before prayer time to fire the reminder.</summary>
        private const int ReminderMinutesBefore = 5;

        /// <summary>
        /// Constant returned in <see cref="NotificationRequest.ReturningData"/>
        /// so the app can navigate to PrayerTimesPage on tap.
        /// </summary>
        public const string PrayerNotificationReturningData = "navigate_prayer_times";

        /// <summary>
        /// Whether prayer notifications are enabled (default: true).
        /// </summary>
        public static bool IsEnabled
        {
            get => Preferences.Get(NotificationsEnabledKey, true);
            set => Preferences.Set(NotificationsEnabledKey, value);
        }

        /// <summary>
        /// Schedule notifications for all prayers for the given day.
        /// Cancels any previously scheduled prayer notifications first.
        /// </summary>
        public async Task ScheduleDailyNotificationsAsync(DayPrayerTimes times)
        {
            await ScheduleMultiDayNotificationsAsync(new List<DayPrayerTimes> { times });
        }

        /// <summary>
        /// Schedule notifications for multiple days of prayer times.
        /// Cancels any previously scheduled prayer notifications first.
        /// This ensures notifications fire even when the app is not running.
        /// </summary>
        public async Task ScheduleMultiDayNotificationsAsync(List<DayPrayerTimes> multiDayTimes)
        {
            if (!IsEnabled) return;

            // Only allow one scheduling operation at a time to prevent race conditions
            if (!await _schedulingSemaphore.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false))
            {
                System.Diagnostics.Debug.WriteLine("[Notifications] Scheduling already in progress, skipping concurrent call");
                return;
            }

            try
            {
                // Cancel existing prayer notifications
                await CancelAllAsync().ConfigureAwait(false);

                var now = DateTime.Now;

                for (int dayOffset = 0; dayOffset < multiDayTimes.Count; dayOffset++)
                {
                    var times = multiDayTimes[dayOffset];
                    var date = times.Date.Date;
                    var cityUtcOffset = times.UtcOffsetHours;

                    await SchedulePrayerNotification(PrayerTimeType.Fajr, times.Fajr, date, now, dayOffset, cityUtcOffset).ConfigureAwait(false);
                    await SchedulePrayerNotification(PrayerTimeType.Dhuhr, times.Dhuhr, date, now, dayOffset, cityUtcOffset).ConfigureAwait(false);
                    await SchedulePrayerNotification(PrayerTimeType.Asr, times.Asr, date, now, dayOffset, cityUtcOffset).ConfigureAwait(false);
                    await SchedulePrayerNotification(PrayerTimeType.Maghrib, times.Maghrib, date, now, dayOffset, cityUtcOffset).ConfigureAwait(false);
                    await SchedulePrayerNotification(PrayerTimeType.Isha, times.Isha, date, now, dayOffset, cityUtcOffset).ConfigureAwait(false);

                    // Also schedule Shurooq as informational
                    await SchedulePrayerNotification(PrayerTimeType.Shurooq, times.Shurooq, date, now, dayOffset, cityUtcOffset).ConfigureAwait(false);
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[Notifications] Scheduled prayer notifications for {multiDayTimes.Count} day(s)");
            }
            finally
            {
                _schedulingSemaphore.Release();
            }
        }

        /// <summary>
        /// Cancel all pending prayer notifications (both reminders and arrival, all days).
        /// </summary>
        public async Task CancelAllAsync()
        {
            try
            {
                for (int day = 0; day < DaysAhead; day++)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        LocalNotificationCenter.Current.Cancel(BaseNotificationId + (day * 10) + i);
                        LocalNotificationCenter.Current.Cancel(BaseReminderNotificationId + (day * 10) + i);
                    }
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Notifications] Error cancelling: {ex.Message}");
            }
        }

        /// <summary>
        /// Request notification permission (required on Android 13+).
        /// Returns true if granted.
        /// </summary>
        public static async Task<bool> RequestPermissionAsync()
        {
            try
            {
                if (MainThread.IsMainThread)
                {
                    return await LocalNotificationCenter.Current.RequestNotificationPermission();
                }

                var tcs = new TaskCompletionSource<bool>();
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var result = await LocalNotificationCenter.Current.RequestNotificationPermission();
                        tcs.TrySetResult(result);
                    }
                    catch
                    {
                        tcs.TrySetResult(false);
                    }
                });

                return await tcs.Task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Notifications] Permission error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Schedule both a 5-minute reminder and an arrival notification for a single prayer.
        /// Prayer times are stored in the selected city's local time. We convert them to UTC
        /// and then to device local time so notifications fire at the correct moment regardless
        /// of the device's timezone setting.
        /// </summary>
        private async Task SchedulePrayerNotification(
            PrayerTimeType prayerType, TimeSpan prayerTime, DateTime date, DateTime now, int dayOffset = 0,
            double cityUtcOffsetHours = 0)
        {
            // Prayer time expressed in city local time (naive DateTime, no timezone info)
            var arrivalCityLocal = date.Add(prayerTime);

            // Convert city local → UTC → device local
            var arrivalUtc = arrivalCityLocal - TimeSpan.FromHours(cityUtcOffsetHours);
            var arrivalTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(arrivalUtc, DateTimeKind.Utc), TimeZoneInfo.Local);

            System.Diagnostics.Debug.WriteLine(
                $"[Notifications] {prayerType}: city={arrivalCityLocal:HH:mm}, utc={arrivalUtc:HH:mm}, device={arrivalTime:HH:mm}, now={now:HH:mm}");

            // --- 5-minute reminder ---
            var reminderTime = arrivalTime.AddMinutes(-ReminderMinutesBefore);
            if (reminderTime > now)
            {
                var reminderId = BaseReminderNotificationId + (dayOffset * 10) + (int)prayerType;
                var (reminderTitle, reminderBody) = GetReminderContent(prayerType, prayerTime);

                var reminderNotification = new NotificationRequest
                {
                    NotificationId = reminderId,
                    Title = reminderTitle,
                    Description = reminderBody,
                    ReturningData = PrayerNotificationReturningData,
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = reminderTime
                    },
                    Android = new AndroidOptions
                    {
                        ChannelId = "prayer_times",
                        IconSmallName = new AndroidIcon("notification_icon"),
                        Priority = AndroidPriority.High,
                        AutoCancel = true
                    }
                };

                try
                {
                    await LocalNotificationCenter.Current.Show(reminderNotification).ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine(
                        $"[Notifications] Scheduled reminder for {prayerType} at {reminderTime:HH:mm}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Notifications] Error scheduling reminder {prayerType}: {ex.Message}");
                }
            }

            // --- Arrival (adhan) notification ---
            if (arrivalTime > now)
            {
                var arrivalId = BaseNotificationId + (dayOffset * 10) + (int)prayerType;
                var (arrivalTitle, arrivalBody) = GetArrivalContent(prayerType, prayerTime);

                var arrivalNotification = new NotificationRequest
                {
                    NotificationId = arrivalId,
                    Title = arrivalTitle,
                    Description = arrivalBody,
                    ReturningData = PrayerNotificationReturningData,
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = arrivalTime
                    },
                    Android = new AndroidOptions
                    {
                        ChannelId = "prayer_times",
                        IconSmallName = new AndroidIcon("notification_icon"),
                        Priority = AndroidPriority.High,
                        AutoCancel = true
                    }
                };

                try
                {
                    await LocalNotificationCenter.Current.Show(arrivalNotification).ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine(
                        $"[Notifications] Scheduled arrival for {prayerType} at {arrivalTime:HH:mm}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Notifications] Error scheduling arrival {prayerType}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get localized content for the 5-minute advance reminder.
        /// </summary>
        private static (string Title, string Body) GetReminderContent(
            PrayerTimeType prayerType, TimeSpan time)
        {
            var timeStr = time.ToString(@"hh\:mm");
            var lang = LocalizationService.GetCurrentLanguage();
            var prayerName = GetPrayerName(prayerType, lang);

            var title = lang switch
            {
                "ar" => $"⏰ اقترب وقت {prayerName}",
                "fr" => $"⏰ {prayerName} dans 5 minutes",
                _ => $"⏰ {prayerName} in 5 minutes"
            };

            var body = lang switch
            {
                "ar" => $"باقي 5 دقائق على أذان {prayerName} ({timeStr})",
                "fr" => $"{prayerName} est dans 5 minutes ({timeStr})",
                _ => $"{prayerName} is in 5 minutes ({timeStr})"
            };

            return (title, body);
        }

        /// <summary>
        /// Get localized content for the prayer-time-arrived notification.
        /// </summary>
        private static (string Title, string Body) GetArrivalContent(
            PrayerTimeType prayerType, TimeSpan time)
        {
            var timeStr = time.ToString(@"hh\:mm");
            var lang = LocalizationService.GetCurrentLanguage();
            var prayerName = GetPrayerName(prayerType, lang);

            var title = lang switch
            {
                "ar" => $"🕌 حان وقت {prayerName}",
                "fr" => $"🕌 C'est l'heure de {prayerName}",
                _ => $"🕌 Time for {prayerName}"
            };

            var body = lang switch
            {
                "ar" => $"حان الآن وقت صلاة {prayerName} ({timeStr})",
                "fr" => $"C'est maintenant l'heure de {prayerName} ({timeStr})",
                _ => $"It is now time for {prayerName} prayer ({timeStr})"
            };

            return (title, body);
        }

        /// <summary>
        /// Get the localized prayer name.
        /// </summary>
        private static string GetPrayerName(PrayerTimeType type, string lang)
        {
            return (type, lang) switch
            {
                (PrayerTimeType.Fajr, "ar") => "الفجر",
                (PrayerTimeType.Fajr, "fr") => "Fajr",
                (PrayerTimeType.Fajr, _) => "Fajr",

                (PrayerTimeType.Shurooq, "ar") => "الشروق",
                (PrayerTimeType.Shurooq, "fr") => "Lever du soleil",
                (PrayerTimeType.Shurooq, _) => "Sunrise",

                (PrayerTimeType.Dhuhr, "ar") => "الظهر",
                (PrayerTimeType.Dhuhr, "fr") => "Dhuhr",
                (PrayerTimeType.Dhuhr, _) => "Dhuhr",

                (PrayerTimeType.Asr, "ar") => "العصر",
                (PrayerTimeType.Asr, "fr") => "Asr",
                (PrayerTimeType.Asr, _) => "Asr",

                (PrayerTimeType.Maghrib, "ar") => "المغرب",
                (PrayerTimeType.Maghrib, "fr") => "Maghrib",
                (PrayerTimeType.Maghrib, _) => "Maghrib",

                (PrayerTimeType.Isha, "ar") => "العشاء",
                (PrayerTimeType.Isha, "fr") => "Isha",
                (PrayerTimeType.Isha, _) => "Isha",

                _ => type.ToString()
            };
        }
    }
}
