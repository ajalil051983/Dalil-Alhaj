using KhayratAlhaj.Services;
using KhayratAlhaj.Services.PrayerTimes;
using KhayratAlhaj.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls;

namespace KhayratAlhaj.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private const string DarkModeKey = "dark_mode";
        private const string FontSizeKey = "font_size";
        private bool isApplyingTheme = false; // Prevent redundant theme applications
        private AppTheme? lastAppliedTheme = null; // Track last applied theme
        private bool isLoadingSettings = false; // Prevent language change event during initialization
        private readonly PrayerTimeService prayerService;
        private readonly NotificationService notificationService;

        public SettingsPage()
        {
            InitializeComponent();
            FlowDirection = LocalizationService.GetFlowDirection();
            prayerService = new PrayerTimeService();
            notificationService = new NotificationService();
            LoadSettings();
            // ApplyThemeColors will be called in OnAppearing
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ApplyThemeColors();
            
            // Subscribe to theme changes
            WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, async (recipient, message) =>
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ApplyThemeColors();
                });
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
        }

        private void ApplyThemeColors()
        {
            // Prevent redundant theme applications
            if (isApplyingTheme)
            {
                return;
            }

            var currentTheme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;
            var isDark = currentTheme == AppTheme.Dark;

            // Skip if theme hasn't changed
            var effectiveTheme = isDark ? AppTheme.Dark : AppTheme.Light;
            if (lastAppliedTheme == effectiveTheme)
            {
                return;
            }

            try
            {
                isApplyingTheme = true;
                lastAppliedTheme = effectiveTheme;

                // Page background
                this.BackgroundColor = ThemeColors.PageBackground(isDark);

                                // Update all Borders
                if (this.Content is ScrollView scrollView &&
                    scrollView.Content is VerticalStackLayout stack)
                {
                    bool isFirst = true;
                    foreach (var child in stack.Children)
                    {
                        if (child is Border border)
                        {
                            if (isFirst)
                            {
                                // Keep header purple
                                isFirst = false;
                                continue;
                            }
                            isFirst = false;

                            border.Background = new SolidColorBrush(
                                ThemeColors.CardBackground(isDark));

                            UpdateBorderContent(border, isDark);
                        }
                    }
                }

                // Update specific named elements
                if (LanguageLabel != null)
                    LanguageLabel.TextColor = ThemeColors.LabelText(isDark);
                if (PreviewLabel != null)
                    PreviewLabel.TextColor = ThemeColors.LabelText(isDark);
                if (AboutTitleLabel != null)
                    AboutTitleLabel.TextColor = ThemeColors.LabelText(isDark);
                if (AboutSubtitleLabel != null)
                    AboutSubtitleLabel.TextColor = ThemeColors.SubtleText(isDark);
                if (AboutVersionLabel != null)
                    AboutVersionLabel.TextColor = ThemeColors.SubtleText(isDark);
            }
            finally
            {
                isApplyingTheme = false;
            }
        }

        private void UpdateBorderContent(Border border, bool isDark)
        {
            if (border.Content is Layout layout)
            {
                foreach (var child in layout.Children)
                {
                    if (child is Label label && label != HeaderLabel)
                    {
                        // Update text colors for labels using Black/White scheme
                        if (label.TextColor == ThemeColors.LabelTextLight ||
                            label.TextColor == ThemeColors.LabelTextDark ||
                            label.TextColor == Color.FromArgb("#000000") ||
                            label.TextColor == Color.FromArgb("#FFFFFF"))
                        {
                            label.TextColor = ThemeColors.LabelText(isDark);
                        }
                        else if (label.TextColor == ThemeColors.SubtleTextLight ||
                                 label.TextColor == ThemeColors.SubtleTextDark)
                        {
                            label.TextColor = ThemeColors.SubtleText(isDark);
                        }
                    }
                    else if (child is Picker picker)
                    {
                        picker.TextColor = ThemeColors.LabelText(isDark);
                    }
                    else if (child is Layout nestedLayout)
                    {
                        UpdateBorderContent(new Border { Content = new VerticalStackLayout() }, isDark);
                        // Directly iterate nested layout
                        foreach (var nested in nestedLayout.Children)
                        {
                            if (nested is Label nestedLabel && nestedLabel != HeaderLabel)
                            {
                                if (nestedLabel.TextColor == ThemeColors.LabelTextLight ||
                                    nestedLabel.TextColor == ThemeColors.LabelTextDark ||
                                    nestedLabel.TextColor == Color.FromArgb("#000000") ||
                                    nestedLabel.TextColor == Color.FromArgb("#FFFFFF"))
                                {
                                    nestedLabel.TextColor = ThemeColors.LabelText(isDark);
                                }
                                else if (nestedLabel.TextColor == ThemeColors.SubtleTextLight ||
                                         nestedLabel.TextColor == ThemeColors.SubtleTextDark)
                                {
                                    nestedLabel.TextColor = ThemeColors.SubtleText(isDark);
                                }
                            }
                            else if (nested is Picker nestedPicker)
                            {
                                nestedPicker.TextColor = ThemeColors.LabelText(isDark);
                            }
                        }
                    }
                }
            }
        }

        private void LoadSettings()
        {
            // Set flag to prevent event firing during initialization
            isLoadingSettings = true;
            
            // Load language setting
            var currentLanguage = LocalizationService.GetCurrentLanguage();
            LanguagePicker.SelectedIndex = currentLanguage switch
            {
                "ar" => 0,
                "en" => 1,
                "fr" => 2,
                _ => 0
            };
            
            // Clear flag after setting the value
            isLoadingSettings = false;

            // Load dark mode setting — guard with isLoadingSettings to prevent
            // OnDarkModeToggled from firing during initialization
            isLoadingSettings = true;
            var isDarkMode = Preferences.Get(DarkModeKey, false);
            DarkModeSwitch.IsToggled = isDarkMode;
            isLoadingSettings = false;
            
            // Only apply theme if it's different from current (avoid unnecessary updates)
            if (Application.Current != null)
            {
                var targetTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;
                if (Application.Current.UserAppTheme != targetTheme)
                {
                    Application.Current.UserAppTheme = targetTheme;
                }
            }

            // Load font size setting
            var fontSize = Preferences.Get(FontSizeKey, 17.0);
            PreviewLabel.FontSize = fontSize;
            
            // Only update resource if it has changed
            if (Application.Current != null && Application.Current.Resources.ContainsKey("ContentFontSize"))
            {
                var currentFontSize = Application.Current.Resources["ContentFontSize"];
                if (currentFontSize == null || !currentFontSize.Equals(fontSize))
                {
                    Application.Current.Resources["ContentFontSize"] = fontSize;
                }
            }
            else if (Application.Current != null)
            {
                Application.Current.Resources["ContentFontSize"] = fontSize;
            }

            // Load prayer time settings
            isLoadingSettings = true;
            PrayerNotificationSwitch.IsToggled = NotificationService.IsEnabled;
            CalculationMethodPicker.SelectedIndex = (int)prayerService.GetCalculationMethod();
            MathhabPicker.SelectedIndex = (int)prayerService.GetMathhab() - 1; // enum starts at 1

            // Hijri adjustment: picker items are "-2","-1","0","+1","+2" → index = value + 2
            HijriAdjustmentPicker.SelectedIndex = HijriDateService.GetAdjustment() + 2;
            isLoadingSettings = false;
        }

        private async void OnLanguageChanged(object? sender, EventArgs e)
        {
            // Ignore event if we're loading settings
            if (isLoadingSettings) return;
            
            if (LanguagePicker.SelectedIndex == -1) return;

            var languageCode = LanguagePicker.SelectedIndex switch
            {
                0 => "ar",
                1 => "en",
                2 => "fr",
                _ => "ar"
            };

            var currentLanguage = LocalizationService.GetCurrentLanguage();
            if (languageCode != currentLanguage)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Language change requested: {currentLanguage} -> {languageCode}");
                    
                    LocalizationService.SetLanguage(languageCode);
                    
                    System.Diagnostics.Debug.WriteLine("Language preference saved, showing alert...");
                    
                    // Show confirmation alert
                    await DisplayAlertAsync(
                        KhayratAlhaj.Resources.Localization.AppResources.RestartRequired,
                        KhayratAlhaj.Resources.Localization.AppResources.RestartMessage,
                        KhayratAlhaj.Resources.Localization.AppResources.OK
                    );
                    
                    System.Diagnostics.Debug.WriteLine("Alert dismissed, restarting application...");
                    
                    // Force complete application restart
                    if (Application.Current != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Restarting application to apply language change...");
                        
#if ANDROID
                        try
                        {
                            // On Android, restart the app automatically
                            var context = Android.App.Application.Context;
                            var packageManager = context.PackageManager;
                            var intent = packageManager?.GetLaunchIntentForPackage(context.PackageName!);
                            
                            if (intent != null)
                            {
                                intent.AddFlags(Android.Content.ActivityFlags.ClearTop);
                                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                                intent.AddFlags(Android.Content.ActivityFlags.ClearTask);
                                
                                System.Diagnostics.Debug.WriteLine("Starting new activity with launch intent...");
                                
                                // Start the new activity first
                                context.StartActivity(intent);
                                
                                // Small delay to let the intent process
                                await Task.Delay(100);
                                
                                System.Diagnostics.Debug.WriteLine("Killing current process...");
                                // Kill the process to force complete restart
                                Java.Lang.JavaSystem.Exit(0);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("ERROR: Could not get launch intent!");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ERROR restarting app: {ex.Message}");
                        }
#elif IOS || MACCATALYST
                        // On iOS/MacCatalyst, exit the app (user must reopen manually)
                        System.Environment.Exit(0);
#elif WINDOWS
                        // On Windows, restart the application
                        System.Diagnostics.Process.Start(System.Environment.ProcessPath!);
                        System.Environment.Exit(0);
#else
                        // Fallback: recreate AppShell (may not refresh all resources)
                        if (Application.Current.Windows.Count > 0)
                        {
                            var newShell = new AppShell();
                            System.Diagnostics.Debug.WriteLine("New AppShell created, setting as main page...");
                            Application.Current.Windows[0].Page = newShell;
                            System.Diagnostics.Debug.WriteLine("AppShell set successfully!");
                        }
#endif
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ERROR: Application.Current is null!");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error restarting app: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    // Fallback: try to navigate back
                    try
                    {
                        await Navigation.PopToRootAsync();
                    }
                    catch (Exception navEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error navigating back: {navEx.Message}");
                    }
                }
            }
        }

        private async void OnDarkModeToggled(object? sender, ToggledEventArgs e)
        {
            if (isLoadingSettings) return;

            try
            {
                // Disable the switch temporarily to prevent rapid toggling
                if (sender is Switch switchControl)
                {
                    switchControl.IsEnabled = false;
                }

                Preferences.Set(DarkModeKey, e.Value);
                await ApplyDarkModeAsync(e.Value);

                // Re-enable the switch
                if (sender is Switch switchControl2)
                {
                    switchControl2.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnDarkModeToggled: {ex.Message}");
                
                // Re-enable the switch on error
                if (sender is Switch switchControl)
                {
                    switchControl.IsEnabled = true;
                }
            }
        }

        private async Task ApplyDarkModeAsync(bool isDark)
        {
            if (Application.Current == null) return;

            // Update app theme on main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Application.Current.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;

#if ANDROID
                // Force Android night mode so AppThemeBinding follows the app preference,
                // not the system dark-mode setting
                AndroidX.AppCompat.App.AppCompatDelegate.DefaultNightMode = isDark
                    ? AndroidX.AppCompat.App.AppCompatDelegate.ModeNightYes
                    : AndroidX.AppCompat.App.AppCompatDelegate.ModeNightNo;
#endif
            });

            // Small delay to allow theme change to propagate
            await Task.Delay(50);
            
            // Update current page colors
            await ApplyThemeToCurrentPageAsync(isDark);
        }

        private async Task ApplyThemeToCurrentPageAsync(bool isDark)
        {
            // Update page background on main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                this.BackgroundColor = ThemeColors.PageBackground(isDark);
            });

            // Notify other pages to update asynchronously
            await Task.Run(() =>
            {
                WeakReferenceMessenger.Default.Send(new ThemeChangedMessage(isDark));
            });
        }

        private async void OnFontSizeClicked(object? sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string sizeStr)
            {
                var size = double.Parse(sizeStr);
                Preferences.Set(FontSizeKey, size);
                PreviewLabel.FontSize = size;
                
                // Update global font size
                Application.Current!.Resources["ContentFontSize"] = size;

                var sizeLabel = size == 14 ? KhayratAlhaj.Resources.Localization.AppResources.FontSmall :
                                size == 17 ? KhayratAlhaj.Resources.Localization.AppResources.FontMedium :
                                KhayratAlhaj.Resources.Localization.AppResources.FontLarge;
                
                await DisplayAlertAsync(
                    KhayratAlhaj.Resources.Localization.AppResources.Success,
                    $"{KhayratAlhaj.Resources.Localization.AppResources.FontChanged} {sizeLabel}",
                    KhayratAlhaj.Resources.Localization.AppResources.OK
                );
            }
        }

        #region Prayer Settings

        private async void OnPrayerNotificationToggled(object? sender, ToggledEventArgs e)
        {
            if (isLoadingSettings) return;

            // Disable the switch while working to prevent double-triggering
            if (sender is Switch sw) sw.IsEnabled = false;

            try
            {
                NotificationService.IsEnabled = e.Value;
                if (e.Value)
                {
                    // Request permission first (must be on main thread — shows a system dialog)
                    await NotificationService.RequestPermissionAsync();

                    // Compute prayer times AND schedule notifications entirely on a background
                    // thread so the UI is never blocked by AlarmManager calls.
                    await Task.Run(async () =>
                    {
                        var result = new List<KhayratAlhaj.Models.PrayerTimes.DayPrayerTimes>();
                        for (int i = 0; i < NotificationService.DaysAhead; i++)
                        {
                            var t = await prayerService.GetPrayerTimesAsync(DateTime.Today.AddDays(i))
                                .ConfigureAwait(false);
                            if (t != null) result.Add(t);
                        }
                        if (result.Count > 0)
                            await notificationService.ScheduleMultiDayNotificationsAsync(result)
                                .ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
                else
                {
                    await Task.Run(() => notificationService.CancelAllAsync()).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Error toggling notifications: {ex.Message}");
            }
            finally
            {
                // Re-enable the switch on the main thread — ConfigureAwait(false) means
                // the finally block may run on a background thread, and touching views
                // from a non-UI thread throws AndroidRuntimeException.
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (sender is Switch sw2) sw2.IsEnabled = true;
                });
            }
        }

        private void OnCalculationMethodChanged(object? sender, EventArgs e)
        {
            if (isLoadingSettings) return;
            if (CalculationMethodPicker.SelectedIndex < 0) return;

            prayerService.SetCalculationMethod(
                (KhayratAlhaj.Models.PrayerTimes.CalculationMethod)CalculationMethodPicker.SelectedIndex);
        }

        private void OnMathhabChanged(object? sender, EventArgs e)
        {
            if (isLoadingSettings) return;
            if (MathhabPicker.SelectedIndex < 0) return;

            // Mathhab enum: Shafii=1, Hanafi=2 — picker index is 0-based
            prayerService.SetMathhab(
                (KhayratAlhaj.Models.PrayerTimes.Mathhab)(MathhabPicker.SelectedIndex + 1));
        }

        private void OnHijriAdjustmentChanged(object? sender, EventArgs e)
        {
            if (isLoadingSettings) return;
            if (HijriAdjustmentPicker.SelectedIndex < 0) return;

            // Picker items: "-2","-1","0","+1","+2" → value = index − 2
            int adjustment = HijriAdjustmentPicker.SelectedIndex - 2;
            HijriDateService.SetAdjustment(adjustment);
        }

        #endregion
    }
}
