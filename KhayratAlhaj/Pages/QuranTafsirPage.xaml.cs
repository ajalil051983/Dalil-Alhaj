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
        private readonly int surahNumber;
        private readonly int pageNumber;
        private readonly int firstAyah;
        private readonly int lastAyah;
        private readonly string surahName;
        private readonly DataService dataService;
        private readonly Func<int, int, Task> navigateToAyah;
        private readonly ObservableCollection<QuranTafsirViewItem> items = new();
        private bool isLoading;

        public QuranTafsirPage(
            int surahNumber,
            int pageNumber,
            int firstAyah,
            int lastAyah,
            string surahName,
            DataService dataService,
            Func<int, int, Task> navigateToAyah)
        {
            InitializeComponent();

            this.surahNumber = surahNumber;
            this.pageNumber = pageNumber;
            this.firstAyah = firstAyah;
            this.lastAyah = lastAyah;
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
            var range = firstAyah == lastAyah
                ? firstAyah.ToString()
                : $"{firstAyah} - {lastAyah}";
            TafsirSubtitleLabel.Text = string.Format(GetText("QuranTafsirPage_SubtitleFormat"), pageNumber, range);
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
                // Ayah text comes from the Warsh edition (already cached by the reader);
                // the commentary comes from the ar.muyassar edition. Both flow through
                // QuranService's memory + Preferences cache, so repeat visits are offline.
                var ayahTask = dataService.GetQuranSurahAsync(surahNumber, editionIdentifier: QuranService.WarshEdition);
                var tafsirTask = dataService.GetQuranSurahAsync(surahNumber, editionIdentifier: QuranService.TafsirMuyassarEdition);

                await Task.WhenAll(ayahTask, tafsirTask);

                var ayahs = ayahTask.Result?.Ayahs ?? new List<QuranAyahData>();
                var tafsirAyahs = tafsirTask.Result?.Ayahs ?? new List<QuranAyahData>();

                // Same lookup as the reference app: SELECT * FROM ayat WHERE page = {page}.
                var pageAyahs = ayahs
                    .Where(a => a.Page == pageNumber)
                    .OrderBy(a => a.NumberInSurah)
                    .ToList();

                // Fall back to the requested ayah range if page numbers are missing.
                if (pageAyahs.Count == 0)
                {
                    pageAyahs = ayahs
                        .Where(a => a.NumberInSurah >= firstAyah && a.NumberInSurah <= lastAyah)
                        .OrderBy(a => a.NumberInSurah)
                        .ToList();
                }

                var tafsirByAyah = tafsirAyahs.ToDictionary(a => a.NumberInSurah, a => a.Text);

                items.Clear();
                foreach (var ayah in pageAyahs)
                {
                    tafsirByAyah.TryGetValue(ayah.NumberInSurah, out var tafsirText);
                    items.Add(new QuranTafsirViewItem
                    {
                        SurahNumber = surahNumber,
                        AyahNumber = ayah.NumberInSurah,
                        AyahNumberText = $"﴿{ayah.NumberInSurah}﴾",
                        AyahText = ayah.Text,
                        TafsirText = string.IsNullOrWhiteSpace(tafsirText)
                            ? GetText("QuranTafsirPage_TafsirMissing")
                            : tafsirText
                    });
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
