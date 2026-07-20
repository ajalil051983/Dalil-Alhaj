using KhayratAlhaj.Models;
using KhayratAlhaj.Services;
using KhayratAlhaj.Services.PrayerTimes;
using System.Text.Json;

namespace KhayratAlhaj.Pages
{
    public partial class DhikrCounterPage : ContentPage
    {
        private const string ProgressPreferenceKey = "dhikr_counter_progress";

        private readonly Category category;
        private readonly SubCategory subCategory;
        private readonly string dhikrType;
        private readonly DataService dataService;
        private readonly PrayerTimeService prayerTimeService = new();

        private List<DhikrItem> dhikrItems = new();
        private List<int> remainingCounts = new();
        private int currentIndex;
        private bool isSequenceCompleted;
        private bool isBusy;

        public DhikrCounterPage(Category category, SubCategory subCategory, string dhikrType, DataService dataService)
        {
            InitializeComponent();
            FlowDirection = LocalizationService.GetFlowDirection();

            this.category = category;
            this.subCategory = subCategory;
            this.dhikrType = dhikrType;
            this.dataService = dataService;

            Title = subCategory.Name;
            TitleLabel.Text = subCategory.Name;
            HintLabel.Text = GetLocalizedHintText();
            ApplyNavigationLabels();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (dhikrItems.Count == 0)
            {
                await LoadDhikrAsync();
                return;
            }

            await ResetProgressIfDhikrTimeReachedAsync();
            LoadCurrentDhikr();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            SaveProgress();
        }

        private async Task LoadDhikrAsync()
        {
            dhikrItems = await dataService.GetDhikrsByTypeAsync(dhikrType);

            if (dhikrItems.Count == 0)
            {
                isSequenceCompleted = true;
                DhikrTextLabel.Text = GetLocalizedNoDataText();
                CounterButton.Text = "0";
                CounterButton.IsEnabled = false;
                ProgressLabel.Text = string.Empty;
                PreviousButton.IsEnabled = false;
                NextButton.IsEnabled = false;
                return;
            }

            remainingCounts = dhikrItems
                .Select(item => item.InitialTimes > 0 ? item.InitialTimes : 1)
                .ToList();

            currentIndex = 0;
            isSequenceCompleted = false;

            RestoreProgressIfPossible();
            await ResetProgressIfDhikrTimeReachedAsync();
            LoadCurrentDhikr();
        }

        private async void OnCounterButtonClicked(object? sender, EventArgs e)
        {
            if (isBusy || isSequenceCompleted || dhikrItems.Count == 0)
            {
                return;
            }

            isBusy = true;
            try
            {
                await CounterButton.ScaleToAsync(0.93, 80, Easing.CubicOut);
                await CounterButton.ScaleToAsync(1.0, 120, Easing.CubicOut);

                if (remainingCounts[currentIndex] > 1)
                {
                    remainingCounts[currentIndex]--;
                    CounterButton.Text = remainingCounts[currentIndex].ToString();
                    SaveProgress();
                    return;
                }

                remainingCounts[currentIndex] = 0;

                if (currentIndex < dhikrItems.Count - 1)
                {
                    currentIndex++;
                    LoadCurrentDhikr();
                    SaveProgress();
                }
                else
                {
                    CounterButton.Text = "0";
                    isSequenceCompleted = true;
                    HintLabel.Text = GetLocalizedCompletionText();
                    SaveProgress(clearWhenCompleted: true);
                }
            }
            finally
            {
                isBusy = false;
            }
        }

        private async void OnPreviousButtonClicked(object? sender, EventArgs e)
        {
            if (currentIndex <= 0)
            {
                return;
            }

            currentIndex--;
            LoadCurrentDhikr();
            SaveProgress();
        }

        private async void OnNextButtonClicked(object? sender, EventArgs e)
        {
            if (currentIndex >= dhikrItems.Count - 1)
            {
                return;
            }

            currentIndex++;
            LoadCurrentDhikr();
            SaveProgress();
        }

        private void LoadCurrentDhikr()
        {
            if (dhikrItems.Count == 0)
            {
                return;
            }

            var currentDhikr = dhikrItems[currentIndex];
            var remainingCount = remainingCounts[currentIndex];

            DhikrTextLabel.Text = currentDhikr.DisplayText;
            ProgressLabel.Text = GetLocalizedProgressText(currentIndex + 1, dhikrItems.Count);

            CounterButton.Text = remainingCount.ToString();
            CounterButton.IsEnabled = true;
            PreviousButton.IsEnabled = true;
            NextButton.IsEnabled = true;

            isSequenceCompleted = remainingCounts.All(count => count == 0);

            HintLabel.Text = isSequenceCompleted
                ? GetLocalizedCompletionText()
                : GetLocalizedHintText();
        }

        private async Task ResetProgressIfDhikrTimeReachedAsync()
        {
            if (!await IsDhikrTimeReachedAsync() || dhikrItems.Count == 0)
            {
                return;
            }

            currentIndex = 0;
            remainingCounts = dhikrItems
                .Select(item => item.InitialTimes > 0 ? item.InitialTimes : 1)
                .ToList();
            isSequenceCompleted = false;
            SaveProgress();
        }

        private async Task<bool> IsDhikrTimeReachedAsync()
        {
            var prayerTimes = await prayerTimeService.GetPrayerTimesAsync(DateTime.Today, allowGpsFallback: false);
            if (prayerTimes == null)
            {
                return true;
            }

            var now = DateTime.Now.TimeOfDay;

            return dhikrType switch
            {
                "Morning" => now >= prayerTimes.Fajr.Add(TimeSpan.FromHours(1)) && now < prayerTimes.Dhuhr,
                "Evening" => now >= prayerTimes.Asr && now < prayerTimes.Maghrib,
                _ => true
            };
        }

        private void RestoreProgressIfPossible()
        {
            try
            {
                var json = Preferences.Get(ProgressPreferenceKey, string.Empty);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                var progress = JsonSerializer.Deserialize<DhikrProgressState>(json);
                if (progress == null ||
                    !string.Equals(progress.DhikrType, dhikrType, StringComparison.OrdinalIgnoreCase) ||
                    progress.RemainingCounts.Count != dhikrItems.Count)
                {
                    return;
                }

                currentIndex = Math.Clamp(progress.CurrentIndex, 0, dhikrItems.Count - 1);
                remainingCounts = progress.RemainingCounts
                    .Select((count, index) => Math.Clamp(count, 0, Math.Max(1, dhikrItems[index].InitialTimes)))
                    .ToList();
                isSequenceCompleted = progress.IsSequenceCompleted || remainingCounts.All(count => count == 0);
            }
            catch
            {
                // Ignore invalid persisted state and start fresh.
            }
        }

        private void ApplyNavigationLabels()
        {
            switch (LocalizationService.GetCurrentLanguage())
            {
                case "en":
                    PreviousButton.Text = "Previous";
                    NextButton.Text = "Next";
                    break;
                case "fr":
                    PreviousButton.Text = "Precedent";
                    NextButton.Text = "Suivant";
                    break;
                default:
                    PreviousButton.Text = "السابق";
                    NextButton.Text = "التالي";
                    break;
            }
        }

        private void SaveProgress(bool clearWhenCompleted = false)
        {
            if (clearWhenCompleted || dhikrItems.Count == 0 || remainingCounts.All(count => count == 0))
            {
                Preferences.Remove(ProgressPreferenceKey);
                return;
            }

            var progress = new DhikrProgressState
            {
                DhikrType = dhikrType,
                Date = DateTime.Today.ToString("yyyy-MM-dd"),
                CurrentIndex = currentIndex,
                RemainingCounts = remainingCounts.ToList(),
                IsSequenceCompleted = isSequenceCompleted
            };

            Preferences.Set(ProgressPreferenceKey, JsonSerializer.Serialize(progress));
        }

        private static string GetLocalizedNoDataText()
        {
            return LocalizationService.GetCurrentLanguage() switch
            {
                "en" => "No athkar available.",
                "fr" => "Aucun adhkar disponible.",
                _ => "لا توجد أذكار متاحة حاليا."
            };
        }

        private static string GetLocalizedHintText()
        {
            return LocalizationService.GetCurrentLanguage() switch
            {
                "en" => "Tap the button to repeat and continue.",
                "fr" => "Touchez le bouton pour répéter et continuer.",
                _ => "اضغط الزر للتكرار والمتابعة."
            };
        }

        private static string GetLocalizedCompletionText()
        {
            return LocalizationService.GetCurrentLanguage() switch
            {
                "en" => "Completed. May Allah accept.",
                "fr" => "Termine. Qu'Allah accepte.",
                _ => "تم الانتهاء. تقبل الله."
            };
        }

        private static string GetLocalizedProgressText(int index, int total)
        {
            return LocalizationService.GetCurrentLanguage() switch
            {
                "en" => $"Dhikr {index} of {total}",
                "fr" => $"Dhikr {index} sur {total}",
                _ => $"الذكر {index} من {total}"
            };
        }

        private sealed class DhikrProgressState
        {
            public string DhikrType { get; set; } = string.Empty;
            public string Date { get; set; } = string.Empty;
            public int CurrentIndex { get; set; }
            public List<int> RemainingCounts { get; set; } = new();
            public bool IsSequenceCompleted { get; set; }
        }
    }
}
