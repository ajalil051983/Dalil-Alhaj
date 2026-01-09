using DalilAlHaj.Services;
using DalilAlHaj.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls;

namespace DalilAlHaj.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private const string DarkModeKey = "dark_mode";
        private const string FontSizeKey = "font_size";
        private bool isApplyingTheme = false; // Prevent redundant theme applications
        private AppTheme? lastAppliedTheme = null; // Track last applied theme
        private bool isLoadingSettings = false; // Prevent language change event during initialization

        public SettingsPage()
        {
            InitializeComponent();
            FlowDirection = LocalizationService.GetFlowDirection();
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
                this.BackgroundColor = isDark ? Color.FromArgb("#1C1C1E") : Color.FromArgb("#F5F5F5");
            
                // Update all frames
#pragma warning disable CS0618 // Frame is obsolete
                if (this.Content is ScrollView scrollView && 
                    scrollView.Content is VerticalStackLayout stack)
                {
                    foreach (var child in stack.Children)
                {
                    if (child is Frame frame)
                    {
                        // Keep header frame purple, update others
                        if (frame == stack.Children[0]) // Header frame
                        {
                            // Keep header purple
                            continue;
                        }
                        else
                        {
                            // Settings frames
                            frame.BackgroundColor = isDark ? Color.FromArgb("#2C2C2E") : Colors.White;
                            
                            // Update labels inside frames
                            UpdateFrameContent(frame, isDark);
                        }
                    }
                }
            }
#pragma warning restore CS0618
            }
            finally
            {
                isApplyingTheme = false;
            }
        }

#pragma warning disable CS0618 // Frame is obsolete
        private void UpdateFrameContent(Frame frame, bool isDark)
#pragma warning restore CS0618
        {
            if (frame.Content is Layout layout)
            {
                foreach (var child in layout.Children)
                {
                    if (child is Label label && label != HeaderLabel)
                    {
                        // Update text colors
                        if (label.TextColor == Color.FromArgb("#2C3E50") || 
                            label.TextColor == Color.FromArgb("#34495E"))
                        {
                            label.TextColor = isDark ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#2C3E50");
                        }
                        else if (label.TextColor == Color.FromArgb("#7F8C8D"))
                        {
                            label.TextColor = isDark ? Color.FromArgb("#AEAEB2") : Color.FromArgb("#7F8C8D");
                        }
                        else if (label.TextColor == Color.FromArgb("#95A5A6"))
                        {
                            label.TextColor = isDark ? Color.FromArgb("#98989D") : Color.FromArgb("#95A5A6");
                        }
                    }
                    else if (child is Picker picker)
                    {
                        picker.TextColor = isDark ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#2C3E50");
                    }
                    else if (child is Layout nestedLayout)
                    {
                        UpdateFrameContent(nestedLayout, isDark);
                    }
                }
            }
        }

        private void UpdateFrameContent(Layout layout, bool isDark)
        {
            foreach (var child in layout.Children)
            {
                if (child is Label label && label != HeaderLabel)
                {
                    // Handle PreviewLabel separately (it has FontSize property we need to preserve)
                    if (label == PreviewLabel || label == AboutTitleLabel)
                    {
                        label.TextColor = isDark ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#2C3E50");
                    }
                    else if (label == AboutSubtitleLabel)
                    {
                        label.TextColor = isDark ? Color.FromArgb("#AEAEB2") : Color.FromArgb("#7F8C8D");
                    }
                    else if (label == AboutVersionLabel)
                    {
                        label.TextColor = isDark ? Color.FromArgb("#98989D") : Color.FromArgb("#95A5A6");
                    }
                    else if (label.TextColor == Color.FromArgb("#2C3E50") || 
                        label.TextColor == Color.FromArgb("#34495E") ||
                        label.TextColor == Color.FromArgb("#F5F5F5") ||
                        label.TextColor == null)
                    {
                        label.TextColor = isDark ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#2C3E50");
                    }
                    else if (label.TextColor == Color.FromArgb("#7F8C8D") ||
                             label.TextColor == Color.FromArgb("#AEAEB2"))
                    {
                        label.TextColor = isDark ? Color.FromArgb("#AEAEB2") : Color.FromArgb("#7F8C8D");
                    }
                    else if (label.TextColor == Color.FromArgb("#95A5A6") ||
                             label.TextColor == Color.FromArgb("#98989D"))
                    {
                        label.TextColor = isDark ? Color.FromArgb("#98989D") : Color.FromArgb("#95A5A6");
                    }
                }
                else if (child is Picker picker)
                {
                    picker.TextColor = isDark ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#2C3E50");
                }
                else if (child is Layout nestedLayout)
                {
                    UpdateFrameContent(nestedLayout, isDark);
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

            // Load dark mode setting (but don't trigger theme change)
            var isDarkMode = Preferences.Get(DarkModeKey, false);
            DarkModeSwitch.IsToggled = isDarkMode;
            
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
                        DalilAlHaj.Resources.Localization.AppResources.RestartRequired,
                        DalilAlHaj.Resources.Localization.AppResources.RestartMessage,
                        DalilAlHaj.Resources.Localization.AppResources.OK
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
                if (isDark)
                {
                    Application.Current.UserAppTheme = AppTheme.Dark;
                }
                else
                {
                    Application.Current.UserAppTheme = AppTheme.Light;
                }
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
                this.BackgroundColor = isDark ? Color.FromArgb("#1C1C1E") : Color.FromArgb("#F5F5F5");
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

                var sizeLabel = size == 14 ? DalilAlHaj.Resources.Localization.AppResources.FontSmall :
                                size == 17 ? DalilAlHaj.Resources.Localization.AppResources.FontMedium :
                                DalilAlHaj.Resources.Localization.AppResources.FontLarge;
                
                await DisplayAlertAsync(
                    DalilAlHaj.Resources.Localization.AppResources.Success,
                    $"{DalilAlHaj.Resources.Localization.AppResources.FontChanged} {sizeLabel}",
                    DalilAlHaj.Resources.Localization.AppResources.OK
                );
            }
        }
    }
}
