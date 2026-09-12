using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;

namespace KhayratAlhaj
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

#if !DEBUG && ANDROID
            // Runtime integrity checks — prevent debugger attachment and tampered installs
            PerformIntegrityChecks();
#endif

            // Add global exception handler
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            
            // Initialize language
            Services.LocalizationService.InitializeLanguage();
            
            // Initialize theme from preferences
            var isDarkMode = Preferences.Get("dark_mode", false);
            UserAppTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;

#if ANDROID
            // Force Android night mode to match app preference
            // so that AppThemeBinding follows UserAppTheme, not the system setting
            AndroidX.AppCompat.App.AppCompatDelegate.DefaultNightMode = isDarkMode
                ? AndroidX.AppCompat.App.AppCompatDelegate.ModeNightYes
                : AndroidX.AppCompat.App.AppCompatDelegate.ModeNightNo;
#endif
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UNHANDLED EXCEPTION: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                // Ignore ObjectDisposedException for IServiceProvider during app shutdown
                // This is a known MAUI Android issue in ShellFragmentContainer.OnDestroy
                if (ex is ObjectDisposedException && ex.Message.Contains("IServiceProvider"))
                {
                    System.Diagnostics.Debug.WriteLine("Ignoring IServiceProvider disposal exception during shutdown");
                    return;
                }
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"UNOBSERVED TASK EXCEPTION: {e.Exception.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Message: {e.Exception.Message}");
            if (e.Exception.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner exception: {e.Exception.InnerException.Message}");
            }
            e.SetObserved(); // Prevent app crash
        }

        private bool _windowCreated = false;

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Guard against multiple calls (can happen if Android recreates the activity
            // due to a locale/UiMode change before ConfigChanges can prevent it).
            if (_windowCreated && Windows.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("[App] CreateWindow called again — reusing existing window.");
                return Windows[0];
            }

            _windowCreated = true;

            var window = new Window(new Pages.LoadingPage());
            window.Title = "Khayrat Alhaj";
            window.FlowDirection = Services.LocalizationService.GetFlowDirection();

            // Listen for notification taps to navigate to PrayerTimesPage
            LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationTapped;

            // Delay notification scheduling so it doesn't compete with the initial UI render.
            // Scheduling 84 AlarmManager entries during startup overloads the main thread.
            window.Activated += OnWindowFirstActivated;

            return window;
        }

        private bool _notificationsScheduled = false;

        private void OnWindowFirstActivated(object? sender, EventArgs e)
        {
            if (sender is Window window)
                window.Activated -= OnWindowFirstActivated;

            if (_notificationsScheduled) return;
            _notificationsScheduled = true;

            // Give the UI 3 seconds to settle before scheduling background work.
            _ = Task.Delay(3000).ContinueWith(_ => SchedulePrayerNotificationsOnStartupAsync(),
                TaskScheduler.Default);
        }

        /// <summary>
        /// Called when the user taps a local notification.
        /// If the notification was a prayer notification, navigate to PrayerTimesPage.
        /// </summary>
        private async void OnNotificationTapped(NotificationActionEventArgs e)
        {
            try
            {
                if (e.Request?.ReturningData == Services.NotificationService.PrayerNotificationReturningData)
                {
                    // Small delay to ensure the app UI is fully loaded
                    await Task.Delay(500);

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        // Navigate to PrayerTimesPage
                        var mainPage = Current?.Windows.FirstOrDefault()?.Page;
                        if (mainPage?.Navigation != null)
                        {
                            await mainPage.Navigation.PushAsync(new Pages.PrayerTimesPage());
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error navigating from notification: {ex.Message}");
            }
        }

        private static async Task SchedulePrayerNotificationsOnStartupAsync()
        {
            try
            {
                // Permission already requested in LoadingPage on first app startup
                if (!Services.NotificationService.IsEnabled) return;

                // Don't schedule notifications when no real location is set
                // (prevents firing Makkah-based times for users elsewhere)
                var locationCheck = new Services.PrayerTimes.PrayerTimeService();
                if (!await locationCheck.HasUserLocationAsync()) 
                {
                    // Cancel any stale notifications left over from a previous session
                    // that may have used the Makkah fallback
                    var cancel = new Services.NotificationService();
                    await cancel.CancelAllAsync().ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine(
                        "[App] Cancelled stale notifications — no user location set yet");
                    return;
                }

                // Compute prayer times on background thread to avoid blocking UI
                var multiDayTimes = await Task.Run(async () =>
                {
                    var prayerService = new Services.PrayerTimes.PrayerTimeService();
                    var result = new List<Models.PrayerTimes.DayPrayerTimes>();
                    for (int i = 0; i < Services.NotificationService.DaysAhead; i++)
                    {
                        var times = await prayerService.GetPrayerTimesAsync(DateTime.Today.AddDays(i));
                        if (times != null)
                            result.Add(times);
                    }
                    return result;
                }).ConfigureAwait(false);

                if (multiDayTimes.Count > 0)
                {
                    // Schedule notifications on the background thread (AlarmManager does not need the main thread)
                    var notificationService = new Services.NotificationService();
                    await notificationService.ScheduleMultiDayNotificationsAsync(multiDayTimes);
                    System.Diagnostics.Debug.WriteLine(
                        $"[App] Prayer notifications scheduled for {multiDayTimes.Count} day(s) on startup");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Failed to schedule prayer notifications: {ex.Message}");
            }
        }


#if !DEBUG && ANDROID
        /// <summary>
        /// Runtime integrity checks to deter reverse engineering.
        /// Detects debugger attachment and verifies the app was installed from a legitimate source.
        /// </summary>
        private static void PerformIntegrityChecks()
        {
            // 1. Abort if a debugger is attached
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                return;
            }

            try
            {
                var context = Android.App.Application.Context;

                // 2. Verify installer (Google Play, Samsung, Huawei stores)
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
                {
#pragma warning disable CA1416
                    var installer = context.PackageManager?.GetInstallSourceInfo(context.PackageName!)?.InstallingPackageName;
#pragma warning restore CA1416
                    var trustedInstallers = new[] { "com.android.vending", "com.sec.android.app.samsungapps", "com.huawei.appmarket" };
                    if (installer != null && !trustedInstallers.Contains(installer))
                    {
                        System.Diagnostics.Debug.WriteLine("[Integrity] Untrusted installer detected.");
                        // Log but don't kill — allows sideloading during internal testing
                    }
                }

                // 3. Check if app is marked debuggable in manifest (tampered rebuild)
                var appInfo = context.ApplicationInfo;
                if (appInfo != null && (appInfo.Flags & Android.Content.PM.ApplicationInfoFlags.Debuggable) != 0)
                {
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                    return;
                }
            }
            catch
            {
                // Swallow — integrity checks should never crash the app
            }
        }
#endif
    }
}