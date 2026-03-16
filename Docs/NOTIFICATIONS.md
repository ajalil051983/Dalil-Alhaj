# Prayer Times Notifications

## Overview

Dalil Alhaj delivers local push notifications for every daily prayer. Two notifications fire per prayer:

- **Reminder** — 5 minutes before the prayer time
- **Adhan (Arrival)** — exactly at the prayer time

Notifications are pre-scheduled up to **7 days ahead** so they fire reliably even when the app is completely closed. They survive device reboots via a `BroadcastReceiver`, and they respect the user's language (Arabic / French / English).

---

## Files Involved

| File | Role |
|---|---|
| `ZadAlhaj/Services/NotificationService.cs` | Core scheduling, cancellation, content, ID management |
| `ZadAlhaj/App.xaml.cs` | Schedules 7 days on app startup; handles notification tap-to-navigate |
| `ZadAlhaj/Platforms/Android/BootReceiver.cs` | Reschedules after device reboot |
| `ZadAlhaj/Pages/SettingsPage.xaml.cs` | Toggle switch to enable/disable; reschedules on enable |
| `ZadAlhaj/Pages/PrayerTimesPage.xaml.cs` | Requests permission when the page is opened |
| `ZadAlhaj/MauiProgram.cs` | Registers the plugin and creates the `prayer_times` Android channel |
| `ZadAlhaj/Models/PrayerTimes/DayPrayerTimes.cs` | Data model carrying prayer times + city UTC offset |

---

## Plugin

The app uses [`Plugin.LocalNotification`](https://github.com/thudugala/Plugin.LocalNotification) (NuGet).

Registration in `MauiProgram.cs`:

```csharp
.UseLocalNotification(config =>
{
    config.AddAndroid(android =>
    {
        android.AddChannel(new NotificationChannelRequest
        {
            Id             = "prayer_times",
            Name           = "Prayer Times",
            Description    = "Notifications for prayer times",
            Importance     = AndroidImportance.High,
            EnableSound    = true,
            EnableVibration = true,
            ShowBadge      = true
        });
    });
})
```

This creates the `prayer_times` channel (Android O+). All prayer notifications are posted to this channel.

---

## Notification Types and ID Scheme

Each prayer produces **two** scheduled notifications per day.

| Type | Base ID constant | Formula |
|---|---|---|
| Adhan (arrival) | `BaseNotificationId = 1000` | `1000 + (dayOffset × 10) + prayerTypeInt` |
| Reminder (5 min early) | `BaseReminderNotificationId = 2000` | `2000 + (dayOffset × 10) + prayerTypeInt` |

`dayOffset` is 0 for today, 1 for tomorrow, …, 6 for 7 days ahead.
`prayerTypeInt` is the integer value of the `PrayerTimeType` enum (Fajr=0, Shurooq=1, …).

**Example** — Dhuhr arrival notification for tomorrow:
`1000 + (1 × 10) + 2 = 1012`

This deterministic scheme allows `CancelAllAsync()` to cancel every possible ID without storing a list.

---

## Prayers Covered

| Prayer | Reminder | Adhan |
|---|---|---|
| Fajr | ✅ | ✅ |
| Shurooq (Sunrise) | ✅ | ✅ (informational) |
| Dhuhr | ✅ | ✅ |
| Asr | ✅ | ✅ |
| Maghrib | ✅ | ✅ |
| Isha | ✅ | ✅ |

---

## Scheduling Flow

### 1. App Startup (`App.xaml.cs → CreateWindow`)

On every app launch, `SchedulePrayerNotificationsOnStartupAsync()` runs fire-and-forget:

```
CreateWindow
  └─ SchedulePrayerNotificationsOnStartupAsync()
       ├─ Check IsEnabled (Preferences)
       ├─ RequestPermissionAsync()
       ├─ [background thread] PrayerTimeService.GetPrayerTimesAsync(today + i) × 7
       └─ NotificationService.ScheduleMultiDayNotificationsAsync(7 days)
```

All prayer-time calculations and AlarmManager registration happen on a **background thread** to avoid blocking the MAUI UI.

### 2. Settings Toggle (`SettingsPage.xaml.cs → OnPrayerNotificationToggled`)

When the user flips the **Prayer Notifications** switch:

- **Toggle ON**:
  1. Save preference (`NotificationService.IsEnabled = true`)
  2. `RequestPermissionAsync()` (main thread — shows system dialog)
  3. [background thread] compute 7 days of prayer times
  4. `ScheduleMultiDayNotificationsAsync(7 days)`

- **Toggle OFF**:
  1. Save preference (`NotificationService.IsEnabled = false`)
  2. [background thread] `CancelAllAsync()`

The switch is disabled during the operation to prevent double-triggers and re-enabled in a `finally` block on the main thread.

### 3. Device Reboot (`BootReceiver.cs`)

Android clears all `AlarmManager` alarms on reboot. The `BootReceiver` listens for `ACTION_BOOT_COMPLETED`:

```
Device reboots
  └─ BootReceiver.OnReceive()
       ├─ Skip if MAUI App is already running
       ├─ Check IsEnabled
       ├─ PrayerTimeService.GetPrayerTimesAsync(today + i) × 7
       └─ NotificationService.ScheduleMultiDayNotificationsAsync(7 days)
```

It is declared with `DirectBootAware = false`, so it fires after the user unlocks the device post-reboot.

---

## Core Scheduling Logic (`NotificationService.cs`)

### `ScheduleMultiDayNotificationsAsync`

```
1. Acquire SemaphoreSlim (prevents race conditions from concurrent calls)
2. CancelAllAsync() — wipe existing alarms
3. For each DayPrayerTimes in the list:
     For each prayer (Fajr, Shurooq, Dhuhr, Asr, Maghrib, Isha):
       → SchedulePrayerNotification(prayer, time, date, now, dayOffset, cityUtcOffset)
4. Release semaphore
```

The semaphore prevents the app-startup scheduling and a concurrent settings-toggle from corrupting each other.

### `SchedulePrayerNotification` — Timezone Conversion

Prayer times in `DayPrayerTimes` are expressed in the **selected city's local time** (not the device's timezone). Before scheduling, they go through a two-step conversion:

```
city local  →  UTC  →  device local

arrivalUtc   = date.Add(prayerTime) - TimeSpan.FromHours(cityUtcOffset)
arrivalTime  = TimeZoneInfo.ConvertTimeFromUtc(arrivalUtc, TimeZoneInfo.Local)
```

This ensures the `NotifyTime` passed to `Plugin.LocalNotification` is always in the **device's** timezone (required by the plugin) and the `arrivalTime > now` future-check is comparing apples to apples.

Debug log emitted for every prayer:

```
[Notifications] Dhuhr: city=12:36, utc=11:36, device=14:36, now=11:00
```

### Skip Logic

A notification is only scheduled if its fire time is **still in the future**:

```csharp
if (reminderTime > now)  // schedule 5-min reminder
if (arrivalTime  > now)  // schedule adhan
```

Past prayers on day 0 are skipped silently. This avoids immediately firing stale notifications on startup.

---

## Notification Content (Localization)

Titles and bodies are generated per-language at the time of scheduling.

### Reminder (5 min before)

| Language | Title | Body |
|---|---|---|
| Arabic  | `⏰ اقترب وقت الظهر` | `باقي 5 دقائق على أذان الظهر (12:36)` |
| French  | `⏰ Dhuhr dans 5 minutes` | `Dhuhr est dans 5 minutes (12:36)` |
| English | `⏰ Dhuhr in 5 minutes` | `Dhuhr is in 5 minutes (12:36)` |

### Adhan (arrival)

| Language | Title | Body |
|---|---|---|
| Arabic  | `🕌 حان وقت الظهر` | `حان الآن وقت صلاة الظهر (12:36)` |
| French  | `🕌 C'est l'heure de Dhuhr` | `C'est maintenant l'heure de Dhuhr (12:36)` |
| English | `🕌 Time for Dhuhr` | `It is now time for Dhuhr prayer (12:36)` |

The time shown in the body is the city local time (how it appears on the prayer times screen).

---

## Tap-to-Navigate

When the user taps any prayer notification, `App.xaml.cs → OnNotificationTapped` fires:

```csharp
LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationTapped;
```

If `ReturningData == "navigate_prayer_times"`, the app pushes `PrayerTimesPage` onto the navigation stack (with a 500 ms delay to ensure the UI is ready).

---

## Enable / Disable Preference

The on/off state is persisted in `Preferences` under the key `prayer_notifications_enabled` (default: **true**).

```csharp
public static bool IsEnabled
{
    get => Preferences.Get("prayer_notifications_enabled", true);
    set => Preferences.Set("prayer_notifications_enabled", value);
}
```

All three scheduling entry points (startup, settings toggle, BootReceiver) check `IsEnabled` before doing anything.

---

## Android Permission

Android 13+ (API 33) requires the `POST_NOTIFICATIONS` runtime permission. The app requests it:

- On `PrayerTimesPage` appearance (if notifications are enabled)
- Before scheduling when the toggle is turned on in Settings
- On app startup (via `SchedulePrayerNotificationsOnStartupAsync`)

`RequestPermissionAsync()` calls the plugin's built-in permission request, which shows the system dialog. It is always called on the **main thread**.

---

## Cancellation

`CancelAllAsync()` iterates all possible notification IDs deterministically (7 days × 8 slots × 2 types) and calls `LocalNotificationCenter.Current.Cancel(id)` for each. No ID list needs to be persisted.

```
for day  in 0..6:
  for slot in 0..7:
    Cancel(1000 + day*10 + slot)   // adhan
    Cancel(2000 + day*10 + slot)   // reminder
```

---

## Debugging

Filter logcat by the following tags to trace the full lifecycle:

| Tag | What it shows |
|---|---|
| `[Notifications]` | Per-prayer time conversion, scheduled/skipped decisions |
| `[App]` | Startup scheduling result |
| `[BootReceiver]` | Post-reboot rescheduling |
| `[Settings]` | Toggle errors |

Example healthy output after app launch:

```
[App] Prayer notifications scheduled for 7 day(s) on startup
[Notifications] Fajr: city=05:08, utc=04:08, device=05:08, now=11:36
[Notifications] Scheduled reminder for Dhuhr at 12:31
[Notifications] Scheduled arrival for Dhuhr at 12:36
```

---

## Extending

**Add a new prayer**: Add a `PrayerTimeType` value + `TimeSpan` in `DayPrayerTimes`, add a `SchedulePrayerNotification` call in `ScheduleMultiDayNotificationsAsync`, and add entries in `GetPrayerName`, `GetReminderContent`, and `GetArrivalContent`.

**Change advance warning time**: Adjust the `ReminderMinutesBefore` constant in `NotificationService`.

**Change how many days ahead**: Adjust the `DaysAhead` constant in `NotificationService`. `CancelAllAsync` uses the same constant so IDs stay consistent.
