using KhayratAlhaj.Resources.Localization;
using KhayratAlhaj.Models;
using KhayratAlhaj.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace KhayratAlhaj.Pages
{
    public partial class QuranSurahListPage : ContentPage
    {
        private const int BatchSize = 10;

        private readonly Category category;
        private readonly DataService dataService;
        private readonly QuranPackageInstallerService quranPackageInstallerService = new();
        private readonly ObservableCollection<QuranSurahListItem> visibleItems = new();

        private List<QuranSurahListItem> allItems = new();
        private bool isLoading;
        private bool isLoadingMore;
        private string searchText = string.Empty;

        public QuranSurahListPage(Category category, DataService dataService)
        {
            InitializeComponent();
            FlowDirection = LocalizationService.GetFlowDirection();

            this.category = category;
            this.dataService = dataService;

            Title = category.Name;
            SurahCollection.ItemsSource = visibleItems;
            ApplyLocalizedTexts();
            UpdateHeaderTexts();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            FlowDirection = LocalizationService.GetFlowDirection();
            UpdateHeaderTexts();

            if (!quranPackageInstallerService.HasInstalledPages())
            {
                var shouldInstall = await DisplayAlertAsync(
                    GetText("QuranSurahListPage_PackageMissingTitle"),
                    GetText("QuranSurahListPage_PackageMissingMessage"),
                    GetText("QuranSurahListPage_InstallNow"),
                    GetText("QuranSurahListPage_Back"));

                if (!shouldInstall)
                {
                    await Navigation.PopAsync();
                    return;
                }

                await Navigation.PushAsync(new QuranPackagePage(async () =>
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (allItems.Count == 0)
                        {
                            await LoadSurahsAsync();
                        }
                    });
                }));

                if (!quranPackageInstallerService.HasInstalledPages())
                {
                    await Navigation.PopAsync();
                    return;
                }
            }

            if (Application.Current != null)
                Application.Current.RequestedThemeChanged += OnThemeChanged;

            if (allItems.Count == 0)
            {
                await LoadSurahsAsync();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (Application.Current != null)
                Application.Current.RequestedThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            foreach (var item in allItems)
                item.UpdateThemeColors(e.RequestedTheme);
        }

        private async Task LoadSurahsAsync()
        {
            if (isLoading)
            {
                return;
            }

            isLoading = true;
            LoadingOverlay.IsVisible = true;

            try
            {
                allItems = category.Subcategories
                    .Where(s => (s.SurahNumber ?? 0) >= 1 && (s.SurahNumber ?? 0) <= 114)
                    .OrderBy(s => s.SurahNumber)
                    .Select((sub, index) => BuildListItem(sub, index))
                    .ToList();

                visibleItems.Clear();
                EnsureVisibleCount(BatchSize);
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
                isLoading = false;
            }
        }

        private QuranSurahListItem BuildListItem(SubCategory sub, int index)
        {
            var surahNo = sub.SurahNumber ?? 0;
            var isEven = index % 2 == 0;
            var item = new QuranSurahListItem
            {
                SubCategory = sub,
                NumberText = surahNo.ToString(),
                SurahName = sub.Name,
                IsEvenRow = isEven
            };
            item.UpdateThemeColors(Application.Current?.RequestedTheme ?? AppTheme.Light);
            return item;
        }

        private void EnsureVisibleCount(int requestedCount)
        {
            var source = GetFilteredItems();
            var targetCount = Math.Min(requestedCount, source.Count);
            for (var index = visibleItems.Count; index < targetCount; index++)
            {
                visibleItems.Add(source[index]);
            }
        }

        private List<QuranSurahListItem> GetFilteredItems()
        {
            if (string.IsNullOrEmpty(searchText))
                return allItems;

            return allItems
                .Where(i => i.SurahName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                         || i.NumberText.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            searchText = e.NewTextValue?.Trim() ?? string.Empty;
            var filtered = GetFilteredItems();
            visibleItems.Clear();
            if (string.IsNullOrEmpty(searchText))
            {
                EnsureVisibleCount(BatchSize);
            }
            else
            {
                foreach (var item in filtered)
                    visibleItems.Add(item);
            }
        }

        private void OnRemainingItemsThresholdReached(object? sender, EventArgs e)
        {
            if (isLoading || isLoadingMore || visibleItems.Count >= GetFilteredItems().Count)
            {
                return;
            }

            _ = LoadMoreSurahsAsync();
        }

        private async Task LoadMoreSurahsAsync()
        {
            if (isLoading || isLoadingMore || visibleItems.Count >= GetFilteredItems().Count)
            {
                return;
            }

            isLoadingMore = true;
            try
            {
                var requestedCount = visibleItems.Count + BatchSize;

                await Task.Yield();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    EnsureVisibleCount(requestedCount);
                });
            }
            finally
            {
                isLoadingMore = false;
            }
        }

        private async void OnSurahTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is not QuranSurahListItem item)
            {
                return;
            }

            LoadingLabel.Text = GetText("QuranSurahListPage_LoadingOpenSurah");
            LoadingOverlay.IsVisible = true;
            try
            {
                var surahNumber = item.SubCategory.SurahNumber;
                Task<QuranSurahData?>? surahTask = surahNumber is int validSurahNumber && validSurahNumber > 0
                    ? dataService.GetQuranSurahAsync(validSurahNumber, editionIdentifier: QuranService.WarshEdition)
                    : null;

                await Navigation.PushAsync(new QuranReaderPage(category, item.SubCategory, dataService, preloadedSurahTask: surahTask));
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
                LoadingLabel.Text = GetText("QuranSurahListPage_LoadingMoreSurahs");
            }
        }

        private async void OnBookmarksClicked(object? sender, EventArgs e)
        {
            Page? bookmarksPage = null;
            bookmarksPage = new QuranBookmarksPage(async (surahNumber, ayahNumber) =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        // Close the bookmarks page first while it is still the top page,
                        // then push the reader so the reader is not popped by mistake.
                        if (bookmarksPage is not null && Navigation.NavigationStack.Contains(bookmarksPage))
                        {
                            await Navigation.PopAsync();
                        }

                        var targetSub = category.Subcategories.FirstOrDefault(s => (s.SurahNumber ?? 0) == surahNumber);
                        if (targetSub == null)
                        {
                            return;
                        }

                        Task<QuranSurahData?>? surahTask = surahNumber > 0
                            ? dataService.GetQuranSurahAsync(surahNumber, editionIdentifier: QuranService.WarshEdition)
                            : null;

                        await Navigation.PushAsync(new QuranReaderPage(
                            category,
                            targetSub,
                            dataService,
                            preloadedSurahTask: surahTask,
                            initialAyah: ayahNumber > 0 ? ayahNumber : null));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[QuranSurahList] Bookmark navigation failed: {ex}");
                    }
                });
            });

            await Navigation.PushAsync(bookmarksPage);
        }

        private void UpdateHeaderTexts()
        {
            SurahNumberHeaderLabel.Text = GetText("QuranSurahListPage_NumberHeader");
            SurahNameHeaderLabel.Text = GetText("QuranSurahListPage_NameHeader");
            SurahSearchBar.Placeholder = GetText("QuranSurahListPage_SearchPlaceholder");
            LoadingLabel.Text = GetText("QuranSurahListPage_LoadingMoreSurahs");
        }

        private static string GetText(string key)
        {
            return GetText(key, string.Empty);
        }

        private static string GetText(string key, string fallback)
        {
            return AppResources.ResourceManager.GetString(key, AppResources.Culture) ?? fallback;
        }

        private void ApplyLocalizedTexts()
        {
            // Keep the title bound to the selected category while localizing the page chrome.
        }
    }

    public class QuranSurahListItem : INotifyPropertyChanged
    {
        private Color _rowBackgroundColor = Colors.Transparent;

        public SubCategory SubCategory { get; set; } = new();
        public string NumberText { get; set; } = string.Empty;
        public string SurahName { get; set; } = string.Empty;
        public bool IsEvenRow { get; set; }

        public Color RowBackgroundColor
        {
            get => _rowBackgroundColor;
            private set { _rowBackgroundColor = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RowBackgroundColor))); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void UpdateThemeColors(AppTheme theme)
        {
            RowBackgroundColor = theme == AppTheme.Dark
                ? (IsEvenRow ? Color.FromArgb("#2E2E36") : Color.FromArgb("#252528"))
                : (IsEvenRow ? Color.FromArgb("#D9D9DE") : Color.FromArgb("#ECECF1"));
        }
    }
}
