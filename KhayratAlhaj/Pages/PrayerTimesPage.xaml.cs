using KhayratAlhaj.Models.PrayerTimes;
using KhayratAlhaj.Services.PrayerTimes;
using KhayratAlhaj.Services;
using KhayratAlhaj.Messages;
using KhayratAlhaj.Resources.Localization;
using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;
using System.Globalization;

namespace KhayratAlhaj.Pages
{
    public partial class PrayerTimesPage : ContentPage
    {
        private readonly PrayerTimeService prayerService;
        private readonly NotificationService notificationService;
        private DayPrayerTimes? todayTimes;
        private IDispatcherTimer? countdownTimer;
        private bool isApplyingTheme = false;
        private AppTheme? lastAppliedTheme = null;
        private bool isMessengerRegistered = false;
        private bool isWeeklyLoaded = false;
        private PrayerTimeType? _currentHighlightedPrayer = null;

        public PrayerTimesPage()
        {
            InitializeComponent();
            FlowDirection = LocalizationService.GetFlowDirection();
            prayerService = new PrayerTimeService();
            notificationService = new NotificationService();
        }

        protected override async void OnAppearing()
        {
            try
            {
                base.OnAppearing();
                Debug.WriteLine("[PrayerTimesPage] OnAppearing called");

                ApplyThemeColors();

                if (!isMessengerRegistered)
                {
                    WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, async (recipient, message) =>
                    {
                        try
                        {
                            await MainThread.InvokeOnMainThreadAsync(() => ApplyThemeColors());
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[PrayerTimesPage] Theme message error: {ex.Message}");
                        }
                    });
                    isMessengerRegistered = true;
                }

                // Show loading overlay while computing prayer times
                LoadingOverlay.IsVisible = true;
                LoadingOverlay.Opacity = 1;

                // Yield to UI thread so the overlay can render before heavy work
                await Task.Delay(50);

                await LoadPrayerTimesAsync();

                // Fade out loading overlay for smooth transition
                await LoadingOverlay.FadeToAsync(0, 250, Easing.CubicIn);
                LoadingOverlay.IsVisible = false;
                LoadingOverlay.Opacity = 1; // Reset for next appearance

                StartCountdownTimer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PrayerTimesPage] OnAppearing error: {ex.Message}");
            }
        }

        protected override void OnDisappearing()
        {
            try
            {
                base.OnDisappearing();
                StopCountdownTimer();

                if (isMessengerRegistered)
                {
                    WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
                    isMessengerRegistered = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PrayerTimesPage] OnDisappearing error: {ex.Message}");
            }
        }

        private async Task LoadPrayerTimesAsync()
        {
            try
            {
                // Run prayer time calculation on background thread to avoid blocking UI
                todayTimes = await Task.Run(async () => await prayerService.GetPrayerTimesAsync(DateTime.Today));
                if (todayTimes == null)
                {
                    Debug.WriteLine("[PrayerTimesPage] Failed to compute prayer times");
                    return;
                }

                // Update location display
                CityNameLabel.Text = todayTimes.LocationName ?? "---";
                CountryLabel.Text = todayTimes.CountryCode ?? "";
                UpdateLocationModeIndicator();

                // Update date labels
                DateLabel.Text = todayTimes.Date.ToString("dddd, dd MMMM yyyy",
                    CultureInfo.CurrentCulture);

                // Hijri date — use HijriDateService (Aladhan API + adjustment + fallback)
                try
                {
                    var hijriService = new HijriDateService();
                    var hijri = await hijriService.GetHijriDateAsync(todayTimes.Date);
                    if (hijri != null)
                    {
                        HijriDateLabel.Text = HijriDateService.Format(hijri);
                    }
                    else
                    {
                        HijriDateLabel.Text = "";
                    }
                }
                catch
                {
                    HijriDateLabel.Text = "";
                }

                // Update prayer time labels
                FajrTimeLabel.Text = FormatTime(todayTimes.Fajr);
                ShurooqTimeLabel.Text = FormatTime(todayTimes.Shurooq);
                DhuhrTimeLabel.Text = FormatTime(todayTimes.Dhuhr);
                AsrTimeLabel.Text = FormatTime(todayTimes.Asr);
                MaghribTimeLabel.Text = FormatTime(todayTimes.Maghrib);
                IshaTimeLabel.Text = FormatTime(todayTimes.Isha);

                // Highlight next prayer
                HighlightNextPrayer();

                // Show city local time if its timezone differs from the device
                UpdateCityTimeDisplay();

                // Request permission; multi-day scheduling is handled by App startup and BootReceiver
                if (NotificationService.IsEnabled)
                {
                    await NotificationService.RequestPermissionAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PrayerTimesPage] LoadPrayerTimes error: {ex.Message}");
                Debug.WriteLine($"[PrayerTimesPage] Stack: {ex.StackTrace}");
            }
        }

        private async Task LoadWeeklyScheduleAsync()
        {
            try
            {
                // Show loading spinner
                WeeklyLoadingSpinner.IsVisible = true;
                WeeklyLoadingSpinner.IsRunning = true;

                // Yield so spinner can render
                await Task.Delay(50);

                // Run weekly calculations on background thread
                var weeklyTimes = await Task.Run(async () => await prayerService.GetWeeklyPrayerTimesAsync(DateTime.Today));
                var weeklyItems = weeklyTimes.Select(d => new WeeklyDayItem
                {
                    DayName = d.Date.ToString("ddd dd/MM", CultureInfo.CurrentCulture),
                    FajrHeader = AppResources.Fajr,
                    FajrTime = FormatTime(d.Fajr),
                    ShurooqHeader = AppResources.Shurooq,
                    ShurooqTime = FormatTime(d.Shurooq),
                    DhuhrHeader = AppResources.Dhuhr,
                    DhuhrTime = FormatTime(d.Dhuhr),
                    AsrHeader = AppResources.Asr,
                    AsrTime = FormatTime(d.Asr),
                    MaghribHeader = AppResources.Maghrib,
                    MaghribTime = FormatTime(d.Maghrib),
                    IshaHeader = AppResources.Isha,
                    IshaTime = FormatTime(d.Isha)
                }).ToList();

                WeeklyCollection.ItemsSource = weeklyItems;

                // Hide spinner and show the section
                WeeklyLoadingSpinner.IsVisible = false;
                WeeklyLoadingSpinner.IsRunning = false;
                WeeklySection.IsVisible = true;
                isWeeklyLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PrayerTimesPage] LoadWeekly error: {ex.Message}");
                WeeklyLoadingSpinner.IsVisible = false;
                WeeklyLoadingSpinner.IsRunning = false;
            }
        }

        private async void OnWeeklyToggled(object? sender, ToggledEventArgs e)
        {
            var isWeeklyView = e.Value; // true = Weekly, false = Today

            if (isWeeklyView)
            {
                // Show weekly view - Load data if not loaded yet
                if (!isWeeklyLoaded)
                {
                    WeeklyToggleSwitch.IsEnabled = false;
                    await LoadWeeklyScheduleAsync();
                    WeeklyToggleSwitch.IsEnabled = true;
                }
                else
                {
                    WeeklySection.IsVisible = true;
                }

                // Wait one layout pass so MAUI can measure WeeklySection's position.
                // Without this, ScrollToAsync sees Y=0 on first load and is a no-op.
                await Task.Delay(150);
                await MainScrollView.ScrollToAsync(WeeklySection, ScrollToPosition.Start, animated: true);
            }
            else
            {
                // Hide weekly section (show today's view), scroll back to top
                WeeklySection.IsVisible = false;
                await MainScrollView.ScrollToAsync(0, 0, animated: true);
            }
        }

        private static readonly TimeSpan PrayerGracePeriod = TimeSpan.FromMinutes(15);

        private void HighlightNextPrayer()
        {
            if (todayTimes == null) return;

            var now = GetCityNow();
            var next = todayTimes.GetNextPrayer(now, PrayerGracePeriod);
            var highlightColor = Color.FromArgb("#1ABC9C");
            var highlightBg = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#1A1ABC9C")
                : Color.FromArgb("#201ABC9C");
            var normalBg = Colors.Transparent;

            // Reset all rows
            FajrRow.BackgroundColor = normalBg;
            ShurooqRow.BackgroundColor = normalBg;
            DhuhrRow.BackgroundColor = normalBg;
            AsrRow.BackgroundColor = normalBg;
            MaghribRow.BackgroundColor = normalBg;
            IshaRow.BackgroundColor = normalBg;

            if (next.HasValue)
            {
                var (type, time) = next.Value;
                NextPrayerNameLabel.Text = GetPrayerName(type);

                var remaining = time - now;
                if (remaining.TotalSeconds > 0)
                {
                    // Prayer hasn't arrived yet — show countdown
                    CountdownLabel.Text = FormatCountdown(remaining);
                }
                else
                {
                    // Prayer time has passed but still within grace period — show elapsed time
                    CountdownLabel.Text = FormatElapsed(-remaining);
                }

                // Highlight the active row
                switch (type)
                {
                    case PrayerTimeType.Fajr: FajrRow.BackgroundColor = highlightBg; break;
                    case PrayerTimeType.Shurooq: ShurooqRow.BackgroundColor = highlightBg; break;
                    case PrayerTimeType.Dhuhr: DhuhrRow.BackgroundColor = highlightBg; break;
                    case PrayerTimeType.Asr: AsrRow.BackgroundColor = highlightBg; break;
                    case PrayerTimeType.Maghrib: MaghribRow.BackgroundColor = highlightBg; break;
                    case PrayerTimeType.Isha: IshaRow.BackgroundColor = highlightBg; break;
                }
                _currentHighlightedPrayer = type;
            }
            else
            {
                // All prayers passed — next is tomorrow's Fajr
                NextPrayerNameLabel.Text = AppResources.Fajr;
                CountdownLabel.Text = "--:--";
            }
        }

        #region Countdown Timer

        private void StartCountdownTimer()
        {
            StopCountdownTimer();
            countdownTimer = Dispatcher.CreateTimer();
            countdownTimer.Interval = TimeSpan.FromSeconds(1);
            countdownTimer.Tick += OnCountdownTick;
            countdownTimer.Start();
        }

        private void StopCountdownTimer()
        {
            if (countdownTimer != null)
            {
                countdownTimer.Stop();
                countdownTimer.Tick -= OnCountdownTick;
                countdownTimer = null;
            }
        }

        private void OnCountdownTick(object? sender, EventArgs e)
        {
            if (todayTimes == null) return;

            UpdateCityTimeDisplay();

            var now = GetCityNow();
            var next = todayTimes.GetNextPrayer(now, PrayerGracePeriod);
            if (next.HasValue)
            {
                var (type, time) = next.Value;
                var remaining = time - now;

                // Refresh highlight whenever the active prayer changes
                if (type != _currentHighlightedPrayer)
                {
                    HighlightNextPrayer();
                    return;
                }

                NextPrayerNameLabel.Text = GetPrayerName(type);

                if (remaining.TotalSeconds > 0)
                {
                    // Prayer hasn't arrived yet — show countdown
                    CountdownLabel.Text = FormatCountdown(remaining);
                }
                else
                {
                    // Prayer time has come — show how long ago it started
                    CountdownLabel.Text = FormatElapsed(-remaining);
                }
            }
            else
            {
                NextPrayerNameLabel.Text = AppResources.Fajr;
                CountdownLabel.Text = "--:--";
            }
        }

        #endregion

        #region City Search

        private void OnLocationDisplayClicked(object? sender, EventArgs e)
        {
            SearchSection.IsVisible = !SearchSection.IsVisible;
        }

        private async void OnUseCurrentLocationClicked(object? sender, EventArgs e)
        {
            try
            {
                // Check and request location permission at runtime (required on Android 6+)
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlertAsync(
                        AppResources.LocationPermissionDenied,
                        AppResources.LocationPermissionRequired,
                        AppResources.OK);
                    return;
                }

                // Clear stored city so GPS will be used
                prayerService.ClearStoredCity();
                prayerService.SetUseCurrentLocationAsDefault(true);
                UpdateLocationModeIndicator();

                // Hide search section if open
                SearchSection.IsVisible = false;
                CitySearchBar.Text = "";
                CityResults.IsVisible = false;

                // Reset weekly schedule so it refreshes for the new location
                isWeeklyLoaded = false;
                WeeklySection.IsVisible = false;
                WeeklyToggleSwitch.IsToggled = false; // Reset switch to "Today" view

                // Show loading overlay with GPS detection message
                LoadingLabel.Text = AppResources.GettingLocation;
                LoadingOverlay.IsVisible = true;
                LoadingOverlay.Opacity = 1;
                await Task.Delay(50);

                await LoadPrayerTimesAsync();

                // Restore default loading label for future appearances
                LoadingLabel.Text = AppResources.Loading;

                await LoadingOverlay.FadeToAsync(0, 250, Easing.CubicIn);
                LoadingOverlay.IsVisible = false;
                LoadingOverlay.Opacity = 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PrayerTimesPage] UseCurrentLocation error: {ex.Message}");
                LoadingLabel.Text = AppResources.Loading;
                LoadingOverlay.IsVisible = false;
                LoadingOverlay.Opacity = 1;
                await DisplayAlertAsync(
                    AppResources.UnableToGetLocation,
                    ex.Message,
                    AppResources.OK);
            }
        }

        private async void OnCitySearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.NewTextValue) || e.NewTextValue.Length < 2)
            {
                CityResults.IsVisible = false;
                return;
            }

            try
            {
                var results = await prayerService.SearchCitiesAsync(e.NewTextValue);
                CityResults.ItemsSource = results;
                CityResults.IsVisible = results.Any();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PrayerTimesPage] Search error: {ex.Message}");
            }
        }

        private void OnCitySearchPressed(object? sender, EventArgs e)
        {
            // Already handled by TextChanged
        }

        private async void OnCitySelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is LocationEntry city)
            {
                prayerService.SaveSelectedCity(city);
                prayerService.SetUseCurrentLocationAsDefault(false);
                UpdateLocationModeIndicator();

                SearchSection.IsVisible = false;
                CitySearchBar.Text = "";
                CityResults.IsVisible = false;

                // Reset weekly schedule so it refreshes for the newly selected city
                isWeeklyLoaded = false;
                WeeklySection.IsVisible = false;
                WeeklyToggleSwitch.IsToggled = false; // Reset switch to "Today" view

                await LoadPrayerTimesAsync();
            }
        }

        #endregion

        #region Helpers

        private void UpdateLocationModeIndicator()
        {
            if (LocationModeLabel == null)
            {
                return;
            }

            var isAuto = prayerService.IsUsingCurrentLocationAsDefault();
            var lang = LocalizationService.GetCurrentLanguage();

            if (isAuto)
            {
                LocationModeLabel.Text = lang switch
                {
                    "en" => "Mode: Auto GPS",
                    "fr" => "Mode: GPS auto",
                    _ => "الوضع: الموقع الحالي (GPS)"
                };
            }
            else
            {
                LocationModeLabel.Text = lang switch
                {
                    "en" => "Mode: Selected city",
                    "fr" => "Mode: Ville choisie",
                    _ => "الوضع: المدينة المختارة"
                };
            }
        }

        /// <summary>
        /// Shows or hides the city local time card.
        /// The card is visible only when the selected city's UTC offset differs
        /// from the device's current local UTC offset, giving the user a reference
        /// for what time it is in the chosen city.
        /// </summary>
        private void UpdateCityTimeDisplay()
        {
            if (todayTimes == null)
            {
                CityTimeBorder.IsVisible = false;
                return;
            }

            double deviceOffsetHours = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalHours;
            double cityOffsetHours   = todayTimes.UtcOffsetHours;

            // Show the card only when the timezone differs (rounded to quarter-hour precision)
            bool isDifferent = Math.Abs(deviceOffsetHours - cityOffsetHours) > 0.1;
            CityTimeBorder.IsVisible = isDifferent;

            if (isDifferent)
            {
                CityLocalTimePrefixLabel.Text = AppResources.CityLocalTime;
                var cityNow = DateTime.UtcNow + TimeSpan.FromHours(cityOffsetHours);
                CityTimeLabel.Text = cityNow.ToString("HH:mm:ss");

                // Format UTC offset label e.g. "(UTC+3)" or "(UTC-5:30)"
                int    totalMin  = (int)Math.Round(cityOffsetHours * 60);
                int    h        = totalMin / 60;
                int    m        = Math.Abs(totalMin % 60);
                string sign     = h >= 0 ? "+" : "-";
                CityUtcOffsetLabel.Text = m == 0
                    ? $"(UTC{sign}{Math.Abs(h)})"
                    : $"(UTC{sign}{Math.Abs(h)}:{m:D2})";
            }
        }

        /// <summary>
        /// Returns the current time-of-day in the selected city's timezone.
        /// Prayer times are expressed in the city's local time, so the countdown
        /// must also use that timezone rather than the device's local time.
        /// </summary>
        private TimeSpan GetCityNow()
        {
            if (todayTimes == null)
                return DateTime.Now.TimeOfDay;
            return (DateTime.UtcNow + TimeSpan.FromHours(todayTimes.UtcOffsetHours)).TimeOfDay;
        }

        private static string FormatTime(TimeSpan ts)
        {
            if (ts == TimeSpan.Zero) return "--:--";
            int h = ts.Hours;
            int m = ts.Minutes;
            return $"{h:D2}:{m:D2}";
        }

        private static string FormatCountdown(TimeSpan remaining)
        {
            if (remaining.TotalSeconds < 0) return "--:--";
            int hours = (int)remaining.TotalHours;
            int minutes = remaining.Minutes;
            int seconds = remaining.Seconds;
            return hours > 0
                ? $"{hours}:{minutes:D2}:{seconds:D2}"
                : $"{minutes:D2}:{seconds:D2}";
        }

        /// <summary>
        /// Formats elapsed time since a prayer started as "+MM:SS" (or "+H:MM:SS" when over an hour).
        /// The leading '+' visually distinguishes elapsed time from a countdown.
        /// </summary>
        private static string FormatElapsed(TimeSpan elapsed)
        {
            if (elapsed.TotalSeconds < 0) return "00:00";
            int hours = (int)elapsed.TotalHours;
            int minutes = elapsed.Minutes;
            int seconds = elapsed.Seconds;
            return hours > 0
                ? $"+{hours}:{minutes:D2}:{seconds:D2}"
                : $"+{minutes:D2}:{seconds:D2}";
        }

        private static string GetPrayerName(PrayerTimeType type) => type switch
        {
            PrayerTimeType.Fajr => AppResources.Fajr,
            PrayerTimeType.Shurooq => AppResources.Shurooq,
            PrayerTimeType.Dhuhr => AppResources.Dhuhr,
            PrayerTimeType.Asr => AppResources.Asr,
            PrayerTimeType.Maghrib => AppResources.Maghrib,
            PrayerTimeType.Isha => AppResources.Isha,
            _ => type.ToString()
        };

        #endregion

        #region Theme

        private void ApplyThemeColors()
        {
            var currentTheme = Application.Current?.RequestedTheme ?? AppTheme.Light;
            if (isApplyingTheme || lastAppliedTheme == currentTheme) return;

            try
            {
                isApplyingTheme = true;
                lastAppliedTheme = currentTheme;

                var bgColor = ThemeColors.PageBackground(currentTheme == AppTheme.Dark);
                var textColor = ThemeColors.PrimaryText(currentTheme == AppTheme.Dark);

                PageGrid.BackgroundColor = bgColor;

                Debug.WriteLine($"[PrayerTimesPage] Theme applied: {currentTheme}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PrayerTimesPage] ApplyThemeColors error: {ex.Message}");
            }
            finally
            {
                isApplyingTheme = false;
            }
        }

        #endregion
    }

    /// <summary>
    /// Simple data class for weekly schedule CollectionView binding.
    /// </summary>
    public class WeeklyDayItem
    {
        public string DayName { get; set; } = "";
        public string FajrHeader { get; set; } = "";
        public string FajrTime { get; set; } = "";
        public string ShurooqHeader { get; set; } = "";
        public string ShurooqTime { get; set; } = "";
        public string DhuhrHeader { get; set; } = "";
        public string DhuhrTime { get; set; } = "";
        public string AsrHeader { get; set; } = "";
        public string AsrTime { get; set; } = "";
        public string MaghribHeader { get; set; } = "";
        public string MaghribTime { get; set; } = "";
        public string IshaHeader { get; set; } = "";
        public string IshaTime { get; set; } = "";
    }
}
