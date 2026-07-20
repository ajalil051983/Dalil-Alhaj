using CommunityToolkit.Mvvm.Messaging;
using KhayratAlhaj.Messages;
using KhayratAlhaj.Models;
using KhayratAlhaj.Resources.Localization;
using KhayratAlhaj.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace KhayratAlhaj.Pages
{
    public partial class QuranReaderPage : ContentPage
    {
        private const string LastPositionPageKey = "quran_last_page_position";
        private const string LastPositionLegacyAyahKey = "quran_last_position";

        private readonly Category category;
        private readonly DataService dataService;
        private readonly QuranBookmarkService bookmarkService;
        private readonly QuranPageAssetService pageAssetService;
        private QuranSurahData? preloadedSurahData;
        private Task<QuranSurahData?>? preloadedSurahTask;

        private readonly ObservableCollection<QuranPageAssetItem> pageItems = new();
        private readonly List<QuranPageAssetItem> allPages = new();

        private SubCategory subCategory;
        private string? lastLoadedLanguage;
        private bool isLoading;
        private bool isMessengerRegistered;
        private bool hasPackagePages;
        private int loadVersion;
        private int currentPageIndex;
        private int currentSurahNumber;
        private string currentSurahArabicName = string.Empty;

        public QuranReaderPage(
            Category category,
            SubCategory subCategory,
            DataService dataService,
            QuranSurahData? preloadedSurahData = null,
            Task<QuranSurahData?>? preloadedSurahTask = null)
        {
            InitializeComponent();
            this.category = category;
            this.subCategory = subCategory;
            this.dataService = dataService;
            this.preloadedSurahData = preloadedSurahData;
            this.preloadedSurahTask = preloadedSurahTask;
            bookmarkService = new QuranBookmarkService();
            pageAssetService = new QuranPageAssetService();
            lastLoadedLanguage = LocalizationService.GetCurrentLanguage();

            Title = subCategory.Name;
            UpdateLocalizedHeaderTexts();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            UpdateLocalizedHeaderTexts();

            if (!isMessengerRegistered)
            {
                WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, (_, _) =>
                {
                    MainThread.BeginInvokeOnMainThread(RefreshPageBookmarkIcon);
                });
                isMessengerRegistered = true;
            }

            var currentLanguage = LocalizationService.GetCurrentLanguage();
            if (currentLanguage != lastLoadedLanguage)
            {
                lastLoadedLanguage = currentLanguage;
                await ReloadCurrentSubCategoryAsync();
                return;
            }

            if (allPages.Count == 0)
            {
                await LoadSurahAsync(subCategory.SurahNumber);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (isMessengerRegistered)
            {
                WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
                isMessengerRegistered = false;
            }
        }

        private async Task ReloadCurrentSubCategoryAsync()
        {
            var refreshedCategory = await dataService.GetCategoryByIdAsync(category.Id);
            if (refreshedCategory == null)
            {
                return;
            }

            var refreshedSub = refreshedCategory.Subcategories.FirstOrDefault(s => s.Id == subCategory.Id);
            if (refreshedSub != null)
            {
                subCategory = refreshedSub;
            }

            Title = subCategory.Name;
            await LoadSurahAsync(subCategory.SurahNumber);
        }

        private async Task LoadSurahAsync(int? surahNumber, int? scrollToAyah = null)
        {
            if (isLoading || surahNumber is null || surahNumber < 1 || surahNumber > 114)
            {
                return;
            }

            isLoading = true;
            var requestedLoadVersion = ++loadVersion;
            LoadingOverlay.IsVisible = true;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var surah = await ResolveSurahDataAsync(surahNumber.Value);
                if (surah == null)
                {
                    await DisplayAlertAsync(AppResources.Error, AppResources.QuranNoInternet, "حسناً");
                    return;
                }

                if (requestedLoadVersion != loadVersion)
                {
                    LogQuran($"LoadSurahAsync stale load ignored: surah={surahNumber}");
                    return;
                }

                currentSurahNumber = surah.Number;
                currentSurahArabicName = string.IsNullOrWhiteSpace(surah.Name) ? subCategory.Name : surah.Name;

                SurahTitleLabel.Text = subCategory.Name;
                SurahSubtitleLabel.Text = $"{ToLatinDigits(surah.NumberOfAyahs)} آية • مصحف الصفحات";
                MushafSurahNameLabel.Text = string.IsNullOrWhiteSpace(currentSurahArabicName)
                    ? subCategory.Name
                    : $"سورة {currentSurahArabicName}";

                BuildPageItems(surah);
                BindCarouselItems();

                if (requestedLoadVersion != loadVersion)
                {
                    LogQuran($"LoadSurahAsync stale post-bind ignored: surah={surahNumber}");
                    return;
                }

                hasPackagePages = allPages.Any(p => !p.IsImageMissing);
                MissingPackageBorder.IsVisible = !hasPackagePages;

                var targetPageIndex = ResolveInitialPageIndex(scrollToAyah);
                MoveToPage(targetPageIndex, false);

                LogQuran($"Loaded surah={surah.Number}, pages={allPages.Count}, hasPackagePages={hasPackagePages}, elapsedMs={stopwatch.ElapsedMilliseconds}");
            }
            catch (Exception ex)
            {
                LogQuran($"LoadSurahAsync failed: {ex}");
                await DisplayAlertAsync(AppResources.Error, AppResources.QuranNoInternet, "حسناً");
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
                isLoading = false;
            }
        }

        private async Task<QuranSurahData?> ResolveSurahDataAsync(int surahNumber)
        {
            QuranSurahData? surah = null;

            if (preloadedSurahData != null && preloadedSurahData.Number == surahNumber)
            {
                surah = preloadedSurahData;
                preloadedSurahData = null;
            }

            if (surah == null && preloadedSurahTask != null)
            {
                var task = preloadedSurahTask;
                preloadedSurahTask = null;
                var taskSurah = await task;
                if (taskSurah != null && taskSurah.Number == surahNumber)
                {
                    surah = taskSurah;
                }
            }

            surah ??= await dataService.GetQuranSurahAsync(surahNumber, editionIdentifier: QuranService.WarshEdition)
                ?? await dataService.GetQuranSurahAsync(surahNumber, editionIdentifier: QuranService.TajweedEdition);

            return surah;
        }

        private void BuildPageItems(QuranSurahData surah)
        {
            allPages.Clear();

            var groups = surah.Ayahs
                .GroupBy(a => a.Page > 0 ? a.Page : a.NumberInSurah)
                .OrderBy(g => g.Key)
                .ToList();

            for (var i = 0; i < groups.Count; i++)
            {
                var ayahs = groups[i].OrderBy(a => a.NumberInSurah).ToList();
                var firstAyah = ayahs.First().NumberInSurah;
                var lastAyah = ayahs.Last().NumberInSurah;
                var mushafPage = groups[i].Key;
                var imagePath = pageAssetService.GetPageImagePath(mushafPage);

                allPages.Add(new QuranPageAssetItem
                {
                    SequenceNumber = i + 1,
                    MushafPageNumber = mushafPage,
                    SurahNumber = surah.Number,
                    FirstAyahNumber = firstAyah,
                    LastAyahNumber = lastAyah,
                    PageImagePath = imagePath ?? string.Empty,
                    IsImageMissing = string.IsNullOrWhiteSpace(imagePath)
                });
            }
        }

        private void BindCarouselItems()
        {
            pageItems.Clear();
            foreach (var page in allPages)
            {
                pageItems.Add(page);
            }

            PageCarousel.ItemsSource = pageItems;
        }

        private int ResolveInitialPageIndex(int? requestedAyah)
        {
            if (allPages.Count == 0)
            {
                return 0;
            }

            if (requestedAyah.HasValue)
            {
                return FindPageIndexForAyah(requestedAyah.Value);
            }

            var savedPage = GetSavedPagePosition(currentSurahNumber);
            if (savedPage > 0)
            {
                var bySequence = allPages.FindIndex(p => p.SequenceNumber == savedPage);
                if (bySequence >= 0)
                {
                    return bySequence;
                }
            }

            var legacyAyah = GetLegacySavedAyah(currentSurahNumber);
            if (legacyAyah > 0)
            {
                return FindPageIndexForAyah(legacyAyah);
            }

            return 0;
        }

        private int FindPageIndexForAyah(int ayahNumber)
        {
            var index = allPages.FindIndex(p => ayahNumber >= p.FirstAyahNumber && ayahNumber <= p.LastAyahNumber);
            return index >= 0 ? index : 0;
        }

        private void MoveToPage(int index, bool animated)
        {
            if (allPages.Count == 0)
            {
                UpdatePageHeader();
                RefreshPageBookmarkIcon();
                return;
            }

            currentPageIndex = Math.Clamp(index, 0, allPages.Count - 1);
            try
            {
                PageCarousel.ScrollTo(currentPageIndex, position: ScrollToPosition.Center, animate: animated);
            }
            catch
            {
                PageCarousel.Position = currentPageIndex;
            }

            UpdatePageHeader();
            RefreshPageBookmarkIcon();
            SaveLastPosition(currentSurahNumber, allPages[currentPageIndex].SequenceNumber);
        }

        private async void OnPrevSurahClicked(object? sender, EventArgs e)
        {
            var current = subCategory.SurahNumber ?? 1;
            if (current <= 1)
            {
                return;
            }

            await NavigateToSurahAsync(current - 1);
        }

        private async void OnNextSurahClicked(object? sender, EventArgs e)
        {
            var current = subCategory.SurahNumber ?? 1;
            if (current >= 114)
            {
                return;
            }

            await NavigateToSurahAsync(current + 1);
        }

        private async Task NavigateToSurahAsync(int surahNumber)
        {
            var target = category.Subcategories.FirstOrDefault(s => (s.SurahNumber ?? 0) == surahNumber);
            if (target == null)
            {
                return;
            }

            subCategory = target;
            Title = target.Name;
            await LoadSurahAsync(target.SurahNumber);
        }

        private void OnPreviousPageClicked(object? sender, EventArgs e)
        {
            if (isLoading || allPages.Count == 0 || currentPageIndex <= 0)
            {
                return;
            }

            MoveToPage(currentPageIndex - 1, true);
        }

        private void OnNextPageClicked(object? sender, EventArgs e)
        {
            if (isLoading || allPages.Count == 0 || currentPageIndex >= allPages.Count - 1)
            {
                return;
            }

            MoveToPage(currentPageIndex + 1, true);
        }

        private async void OnBookmarksClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new QuranBookmarksPage((surahNumber, ayahNumber) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var targetSub = category.Subcategories.FirstOrDefault(s => (s.SurahNumber ?? 0) == surahNumber);
                    if (targetSub != null)
                    {
                        subCategory = targetSub;
                        Title = targetSub.Name;
                    }

                    await LoadSurahAsync(surahNumber, ayahNumber);
                });
            }));
        }

        private void OnPageBookmarkClicked(object? sender, EventArgs e)
        {
            if (allPages.Count == 0)
            {
                return;
            }

            var page = allPages[currentPageIndex];
            bookmarkService.Toggle(page.SurahNumber, page.FirstAyahNumber, subCategory.Name, $"الآية {ToLatinDigits(page.FirstAyahNumber)}");
            RefreshPageBookmarkIcon();
        }

        private async void OnAyahJumpCompleted(object? sender, EventArgs e)
        {
            if (!int.TryParse(AyahJumpEntry.Text, out var ayahNumber))
            {
                return;
            }

            var pageIndex = FindPageIndexForAyah(ayahNumber);
            if (pageIndex < 0 || pageIndex >= allPages.Count)
            {
                await DisplayAlertAsync("غير موجود", "رقم الآية غير موجود في هذه السورة.", "حسناً");
                return;
            }

            MoveToPage(pageIndex, true);
        }

        private async void OnRetryPackageClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new QuranPackagePage(async () =>
            {
                await LoadSurahAsync(subCategory.SurahNumber);
            }));
        }

        private void OnCarouselPositionChanged(object? sender, PositionChangedEventArgs e)
        {
            if (allPages.Count == 0)
            {
                return;
            }

            currentPageIndex = Math.Clamp(e.CurrentPosition, 0, allPages.Count - 1);
            UpdatePageHeader();
            RefreshPageBookmarkIcon();
            SaveLastPosition(currentSurahNumber, allPages[currentPageIndex].SequenceNumber);
        }

        private void RefreshPageBookmarkIcon()
        {
            if (allPages.Count == 0)
            {
                PageBookmarkButton.Text = "☆";
                return;
            }

            var page = allPages[currentPageIndex];
            PageBookmarkButton.Text = bookmarkService.IsBookmarked(page.SurahNumber, page.FirstAyahNumber) ? "★" : "☆";
        }

        private void UpdatePageHeader()
        {
            if (allPages.Count == 0)
            {
                PageIndicatorLabel.Text = "0 / 0";
                MushafFooterLabel.Text = string.Empty;
                return;
            }

            var page = allPages[currentPageIndex];
            var pageText = $"{ToLatinDigits(currentPageIndex + 1)} / {ToLatinDigits(allPages.Count)}";
            if (page.MushafPageNumber > 0)
            {
                pageText += $"  •  {ToLatinDigits(page.MushafPageNumber)}";
            }

            PageIndicatorLabel.Text = pageText;
            var range = page.FirstAyahNumber == page.LastAyahNumber
                ? ToLatinDigits(page.FirstAyahNumber)
                : ToLatinDigits(page.FirstAyahNumber) + " - " + ToLatinDigits(page.LastAyahNumber);
            MushafFooterLabel.Text = page.MushafPageNumber > 0
                ? $"صفحة {ToLatinDigits(page.MushafPageNumber)}  •  الآيات {range}"
                : $"الآيات {range}";
        }

        private void UpdateLocalizedHeaderTexts()
        {
            Title = subCategory.Name;
        }

        private static void SaveLastPosition(int surahNumber, int pageSequence)
        {
            Preferences.Set(LastPositionPageKey, $"{surahNumber}:{pageSequence}");
        }

        private static int GetSavedPagePosition(int surahNumber)
        {
            var raw = Preferences.Get(LastPositionPageKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return 0;
            }

            var parts = raw.Split(':');
            if (parts.Length != 2)
            {
                return 0;
            }

            if (!int.TryParse(parts[0], out var savedSurah) || !int.TryParse(parts[1], out var savedPage))
            {
                return 0;
            }

            return savedSurah == surahNumber ? savedPage : 0;
        }

        private static int GetLegacySavedAyah(int surahNumber)
        {
            var raw = Preferences.Get(LastPositionLegacyAyahKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return 0;
            }

            var parts = raw.Split(':');
            if (parts.Length != 2)
            {
                return 0;
            }

            if (!int.TryParse(parts[0], out var savedSurah) || !int.TryParse(parts[1], out var savedAyah))
            {
                return 0;
            }

            return savedSurah == surahNumber ? savedAyah : 0;
        }

        private static string ToLatinDigits(int number)
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        private static void LogQuran(string message)
        {
            Debug.WriteLine($"[QuranReader] {message}");
        }
    }

    public class QuranPageAssetItem
    {
        public int SequenceNumber { get; set; }
        public int MushafPageNumber { get; set; }
        public int SurahNumber { get; set; }
        public int FirstAyahNumber { get; set; }
        public int LastAyahNumber { get; set; }
        public string PageImagePath { get; set; } = string.Empty;
        public bool IsImageMissing { get; set; }
    }
}
