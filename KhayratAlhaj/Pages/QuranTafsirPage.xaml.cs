using KhayratAlhaj.Resources.Localization;
using KhayratAlhaj.Services;
using System.Collections.ObjectModel;

namespace KhayratAlhaj.Pages
{
    /// <summary>
    /// Shows Tafsir Al-Muyassar (التفسير الميسر) for a single mushaf page: every ayah
    /// on the page paired with its commentary. Tapping an entry jumps the reader back
    /// to the mushaf page that contains that ayah (same UX as the reference app).
    /// </summary>
    public partial class QuranTafsirPage : ContentPage
    {
        private readonly List<int> surahNumbers;
        private readonly int pageNumber;
        private readonly string surahName;
        private readonly DataService dataService;
        private readonly Func<int, int, Task> navigateToAyah;
        private readonly ObservableCollection<QuranTafsirViewItem> items = new();
        private bool isLoading;

        public QuranTafsirPage(
            List<int> surahNumbers,
            int pageNumber,
            string surahName,
            DataService dataService,
            Func<int, int, Task> navigateToAyah)
        {
            InitializeComponent();

            this.surahNumbers = surahNumbers is { Count: > 0 } ? surahNumbers : new List<int> { 1 };
            this.pageNumber = pageNumber;
            this.surahName = surahName;
            this.dataService = dataService;
            this.navigateToAyah = navigateToAyah;

            FlowDirection = LocalizationService.GetFlowDirection();
            TafsirCollection.ItemsSource = items;
            ApplyLocalizedTexts();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            FlowDirection = LocalizationService.GetFlowDirection();
            ApplyLocalizedTexts();

            if (items.Count == 0)
            {
                await LoadTafsirAsync();
            }
        }

        private void ApplyLocalizedTexts()
        {
            Title = GetText("QuranTafsirPage_Title");
            TafsirTitleLabel.Text = $"{GetText("QuranTafsirPage_Title")} — {surahName}";
            TafsirSubtitleLabel.Text = string.Format(GetText("QuranTafsirPage_PageOnlyFormat"), pageNumber);
            LoadingMessageLabel.Text = GetText("QuranTafsirPage_Loading");
            EmptyStateLabel.Text = GetText("QuranTafsirPage_Unavailable");
        }

        private async Task LoadTafsirAsync()
        {
            if (isLoading)
            {
                return;
            }

            isLoading = true;
            LoadingOverlay.IsVisible = true;
            EmptyState.IsVisible = false;

            try
            {
                // Fetch ayah text (Warsh) + tafsir (Muyassar) for EVERY surah on the page,
                // then keep only the ayahs that fall on this mushaf page. This makes shared
                // pages (e.g. 604: Ikhlas/Falaq/Nas) show all their surahs' commentary.
                var fetchTasks = surahNumbers.SelectMany(n => new[]
                {
                    dataService.GetQuranSurahAsync(n, editionIdentifier: QuranService.WarshEdition),
                    dataService.GetQuranSurahAsync(n, editionIdentifier: QuranService.TafsirMuyassarEdition)
                }).ToArray();
                await Task.WhenAll(fetchTasks);

                var warshBySurah = new Dictionary<int, List<QuranAyahData>>();
                var tafsirBySurah = new Dictionary<int, List<QuranAyahData>>();
                for (var i = 0; i < surahNumbers.Count; i++)
                {
                    warshBySurah[surahNumbers[i]] = fetchTasks[i * 2].Result?.Ayahs ?? new List<QuranAyahData>();
                    tafsirBySurah[surahNumbers[i]] = fetchTasks[i * 2 + 1].Result?.Ayahs ?? new List<QuranAyahData>();
                    warshSurahCache[surahNumbers[i]] = fetchTasks[i * 2].Result;
                }

                items.Clear();
                foreach (var surahNum in surahNumbers)
                {
                    var pageAyahs = warshBySurah[surahNum]
                        .Where(a => a.Page == pageNumber)
                        .OrderBy(a => a.NumberInSurah)
                        .ToList();

                    var tafsirByAyah = tafsirBySurah[surahNum].ToDictionary(a => a.NumberInSurah, a => a.Text);
                    var surahDisplay = GetSurahName(surahNum);

                    foreach (var ayah in pageAyahs)
                    {
                        tafsirByAyah.TryGetValue(ayah.NumberInSurah, out var tafsirText);
                        items.Add(new QuranTafsirViewItem
                        {
                            SurahNumber = surahNum,
                            AyahNumber = ayah.NumberInSurah,
                            AyahNumberText = $"﴿{surahDisplay} {ayah.NumberInSurah}﴾",
                            AyahText = ayah.Text,
                            TafsirText = string.IsNullOrWhiteSpace(tafsirText)
                                ? GetText("QuranTafsirPage_TafsirMissing")
                                : tafsirText
                        });
                    }
                }

                EmptyState.IsVisible = items.Count == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[QuranTafsir] Load failed: {ex.Message}");
                EmptyState.IsVisible = true;
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
                isLoading = false;
            }
        }

        private string GetSurahName(int surahNumber)
        {
            // Use the Arabic name from the cached surah data when available.
            var data = warshSurahCache.TryGetValue(surahNumber, out var d) ? d : null;
            return data?.Name ?? surahNumber.ToString();
        }

        private readonly Dictionary<int, QuranSurahData?> warshSurahCache = new();

        private async void OnTafsirItemTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is not QuranTafsirViewItem item || navigateToAyah == null)
            {
                return;
            }

            try
            {
                // Pop this page, then move the reader to the tapped ayah's page —
                // mirrors the reference app's "jump to mushaf page from tafsir" action.
                if (Navigation.NavigationStack.LastOrDefault() == this)
                {
                    await Navigation.PopAsync();
                }

                await navigateToAyah(item.SurahNumber, item.AyahNumber);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[QuranTafsir] Jump failed: {ex.Message}");
            }
        }

        private static string GetText(string key)
        {
            return AppResources.ResourceManager.GetString(key, AppResources.Culture) ?? string.Empty;
        }
    }

    public class QuranTafsirViewItem
    {
        public int SurahNumber { get; set; }
        public int AyahNumber { get; set; }
        public string AyahNumberText { get; set; } = string.Empty;
        public string AyahText { get; set; } = string.Empty;
        public string TafsirText { get; set; } = string.Empty;
    }
}
