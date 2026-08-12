using CommunityToolkit.Mvvm.Messaging;
using KhayratAlhaj.Messages;
using KhayratAlhaj.Models;
using KhayratAlhaj.Resources.Localization;
using KhayratAlhaj.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace KhayratAlhaj.Pages
{
    public partial class QuranReaderPage : ContentPage
    {
        private enum QuranPageLoadTarget
        {
            Default,
            FirstPage,
            LastPage
        }

        private const string LastPositionPageKey = "quran_last_page_position";
        private const string LastPositionLegacyAyahKey = "quran_last_position";
        private const double ExpectedPageAspectRatio = 0.455;
        private const double PortraitDoubleTapZoomScale = 2.2;
        private const double MaximumZoomScale = 6.0;

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
        private int? pendingInitialAyah;
        private bool isLoading;
        private bool isMessengerRegistered;
        private bool hasPackagePages;
        private bool isHoldingPage;
        private bool isLandscapeMode;
        private bool isNavigatingSurahBoundary;
        private bool ignoreCurrentItemChanges;
        private bool isPinching;
        private QuranPageAssetItem? initialTargetPage;
        private int loadVersion;
        private int currentPageIndex;
        private int currentSurahNumber;
        private int currentSurahAyahCount;
        private string currentSurahArabicName = string.Empty;
        private IDispatcherTimer? chromeRevealTimer;
        private IDispatcherTimer? chromeAutoHideTimer;
        private double lastAllocatedWidth;
        private double lastAllocatedHeight;

        public QuranReaderPage(
            Category category,
            SubCategory subCategory,
            DataService dataService,
            QuranSurahData? preloadedSurahData = null,
            Task<QuranSurahData?>? preloadedSurahTask = null,
            int? initialAyah = null)
        {
            InitializeComponent();
            this.category = category;
            this.subCategory = subCategory;
            this.dataService = dataService;
            this.preloadedSurahData = preloadedSurahData;
            this.preloadedSurahTask = preloadedSurahTask;
            this.pendingInitialAyah = initialAyah;
            bookmarkService = new QuranBookmarkService();
            pageAssetService = new QuranPageAssetService();
            lastLoadedLanguage = LocalizationService.GetCurrentLanguage();

            Title = subCategory.Name;
            FlowDirection = LocalizationService.GetFlowDirection();
            UpdateLocalizedHeaderTexts();
            ApplyLocalizedTexts();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            UpdateLocalizedHeaderTexts();
            ApplyLocalizedTexts();

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
                FlowDirection = LocalizationService.GetFlowDirection();
                await ReloadCurrentSubCategoryAsync();
                return;
            }

            if (allPages.Count == 0)
            {
                var ayah = pendingInitialAyah;
                pendingInitialAyah = null;
                await LoadSurahAsync(subCategory.SurahNumber, ayah);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            CancelChromeTimers();
            isHoldingPage = false;
            isPinching = false;
            ignoreCurrentItemChanges = false;
            initialTargetPage = null;

            if (isMessengerRegistered)
            {
                WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
                isMessengerRegistered = false;
            }
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (width <= 0 || height <= 0)
            {
                return;
            }

            var changed = Math.Abs(width - lastAllocatedWidth) > 0.5 || Math.Abs(height - lastAllocatedHeight) > 0.5;
            if (!changed)
            {
                return;
            }

            lastAllocatedWidth = width;
            lastAllocatedHeight = height;

            var nextLandscape = width > height;
            var orientationChanged = nextLandscape != isLandscapeMode;
            isLandscapeMode = nextLandscape;

            if (orientationChanged || allPages.Count > 0)
            {
                ApplyOrientationZoomMode(resetCurrentPage: orientationChanged);
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

        private async Task LoadSurahAsync(int? surahNumber, int? scrollToAyah = null, QuranPageLoadTarget target = QuranPageLoadTarget.Default)
        {
            if (isLoading || surahNumber is null || surahNumber < 1 || surahNumber > 114)
            {
                return;
            }

            isLoading = true;
            ignoreCurrentItemChanges = true;
            initialTargetPage = null;
            var requestedLoadVersion = ++loadVersion;
            LoadingOverlay.IsVisible = true;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var surah = await ResolveSurahDataAsync(surahNumber.Value);
                if (surah == null)
                {
                    await DisplayAlertAsync(AppResources.Error, AppResources.QuranNoInternet, AppResources.OK);
                    return;
                }

                if (requestedLoadVersion != loadVersion)
                {
                    LogQuran($"LoadSurahAsync stale load ignored: surah={surahNumber}");
                    return;
                }

                currentSurahNumber = surah.Number;
                currentSurahAyahCount = surah.NumberOfAyahs;
                currentSurahArabicName = string.IsNullOrWhiteSpace(surah.Name) ? subCategory.Name : surah.Name;

                SurahTitleLabel.Text = subCategory.Name;
                SurahSubtitleLabel.Text = string.Format(GetReaderText("QuranReaderPage_SurahSubtitleFormat"), ToLatinDigits(surah.NumberOfAyahs));
                MushafSurahNameLabel.Text = string.Empty;

                BuildPageItems(surah);
                BindCarouselItems();

                if (requestedLoadVersion != loadVersion)
                {
                    LogQuran($"LoadSurahAsync stale post-bind ignored: surah={surahNumber}");
                    return;
                }

                hasPackagePages = allPages.Any(p => !p.IsImageMissing);
                MissingPackageBorder.IsVisible = !hasPackagePages;

                var initialAyahNumber = scrollToAyah;
                if (initialAyahNumber < 1 || initialAyahNumber > surah.NumberOfAyahs)
                {
                    initialAyahNumber = null;
                }

                var targetPageIndex = ResolveInitialPageIndex(initialAyahNumber, target);
                if (allPages.Count == 0)
                {
                    await DisplayAlertAsync(AppResources.Error, AppResources.QuranNoInternet, AppResources.OK);
                    return;
                }

                targetPageIndex = Math.Clamp(targetPageIndex, 0, allPages.Count - 1);
                initialTargetPage = allPages[targetPageIndex];

                currentPageIndex = targetPageIndex;
                UpdatePageHeader();
                RefreshPageBookmarkIcon();
                if (!allPages[targetPageIndex].IsBoundaryTransition)
                {
                    SaveLastPosition(currentSurahNumber, allPages[targetPageIndex].SequenceNumber);
                }

                ApplyInitialCarouselPosition(targetPageIndex);

                LogQuran($"Loaded surah={surah.Number}, pages={allPages.Count}, hasPackagePages={hasPackagePages}, elapsedMs={stopwatch.ElapsedMilliseconds}");
            }
            catch (Exception ex)
            {
                LogQuran($"LoadSurahAsync failed: {ex}");
                await DisplayAlertAsync(AppResources.Error, AppResources.QuranNoInternet, AppResources.OK);
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
                isLoading = false;
            }
        }

        private void ApplyInitialCarouselPosition(int index)
        {
            if (index < 0 || index >= pageItems.Count)
            {
                ignoreCurrentItemChanges = false;
                initialTargetPage = null;
                return;
            }

            void SetPosition()
            {
                if (index < 0 || index >= pageItems.Count)
                {
                    return;
                }

                try
                {
                    PageCarousel.Position = index;
                }
                catch
                {
                    // Ignore transient failures before the carousel is laid out.
                }

                try
                {
                    PageCarousel.ScrollTo(index, position: ScrollToPosition.Center, animate: false);
                }
                catch
                {
                    // Ignore transient scroll failures during initialization.
                }
            }

            // Force the carousel onto the intended page across several layout passes,
            // because CarouselView briefly defaults to the first item (which may be a
            // previous-surah boundary page) before it has finished measuring.
            SetPosition();
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(120), SetPosition);
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(320), SetPosition);
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(550), () =>
            {
                SetPosition();
                ignoreCurrentItemChanges = false;
                initialTargetPage = null;
            });
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

            var (currentStartPage, currentEndPage) = GetSurahPageRange(surah);

            // Add a "go to previous surah" boundary whenever a previous surah exists so the
            // user can always swipe back into it. The boundary shows the previous surah's
            // last page image (which may be the same physical page when short surahs share
            // a page) and navigates into that surah when the user settles on it.
            if (TryGetPreviousSurahNumber(surah.Number, out var previousSurahNumber))
            {
                var (_, previousEndPage) = GetSurahPageRange(previousSurahNumber);
                if (previousEndPage <= 0)
                {
                    previousEndPage = currentStartPage;
                }

                var previousPageImage = pageAssetService.GetPageImagePath(previousEndPage);
                allPages.Add(new QuranPageAssetItem
                {
                    SequenceNumber = 0,
                    MushafPageNumber = previousEndPage,
                    SurahNumber = surah.Number,
                    TargetSurahNumber = previousSurahNumber,
                    PageImagePath = previousPageImage ?? string.Empty,
                    IsBoundaryTransition = true,
                    IsPreviousSurahBoundary = true,
                    IsImageMissing = string.IsNullOrWhiteSpace(previousPageImage)
                });
            }

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
                    IsImageMissing = string.IsNullOrWhiteSpace(imagePath),
                    MissingTitle = GetReaderText("QuranReaderPage_PageUnavailableTitle"),
                    MissingMessage = GetReaderText("QuranReaderPage_PageUnavailableMessage")
                });
            }

            // Add a "go to next surah" boundary whenever a next surah exists so the user can
            // always swipe forward into it. The boundary shows the next surah's first page
            // image (which may be the same physical page when short surahs share a page) and
            // navigates into that surah when the user settles on it.
            if (TryGetNextSurahNumber(surah.Number, out var nextSurahNumber))
            {
                var (nextStartPage, _) = GetSurahPageRange(nextSurahNumber);
                if (nextStartPage <= 0)
                {
                    nextStartPage = currentEndPage;
                }

                var nextPageImage = pageAssetService.GetPageImagePath(nextStartPage);
                allPages.Add(new QuranPageAssetItem
                {
                    SequenceNumber = groups.Count + 1,
                    MushafPageNumber = nextStartPage,
                    SurahNumber = surah.Number,
                    TargetSurahNumber = nextSurahNumber,
                    PageImagePath = nextPageImage ?? string.Empty,
                    IsBoundaryTransition = true,
                    IsImageMissing = string.IsNullOrWhiteSpace(nextPageImage)
                });
            }
        }

        private (int StartPage, int EndPage) GetSurahPageRange(QuranSurahData surah)
        {
            var firstPage = surah.Ayahs.Count > 0 ? surah.Ayahs.Min(a => a.Page) : 0;
            var lastPage = surah.Ayahs.Count > 0 ? surah.Ayahs.Max(a => a.Page) : 0;

            if (firstPage > 0 && BoundaryPageMap.TryGetValue(surah.Number, out var mapped))
            {
                // Prefer the ayah data when it is present, but fall back to the canonical map
                // if the API did not populate page numbers.
                if (firstPage <= 0)
                {
                    firstPage = mapped.StartPage;
                }

                if (lastPage <= 0)
                {
                    lastPage = mapped.EndPage;
                }
            }

            return (firstPage, lastPage);
        }

        private (int StartPage, int EndPage) GetSurahPageRange(int surahNumber)
        {
            if (BoundaryPageMap.TryGetValue(surahNumber, out var mapped))
            {
                return mapped;
            }

            return (0, 0);
        }

        private void BindCarouselItems()
        {
            pageItems.Clear();
            foreach (var page in allPages)
            {
                pageItems.Add(page);
            }

            PageCarousel.ItemsSource = pageItems;
            ApplyOrientationZoomMode(resetCurrentPage: true);
        }

        private int ResolveInitialPageIndex(int? requestedAyah, QuranPageLoadTarget target)
        {
            if (allPages.Count == 0)
            {
                return 0;
            }

            if (target == QuranPageLoadTarget.LastPage)
            {
                return LastRealPageIndex();
            }

            if (target == QuranPageLoadTarget.FirstPage)
            {
                return FirstRealPageIndex();
            }

            if (requestedAyah.HasValue)
            {
                return FindPageIndexForAyah(requestedAyah.Value);
            }

            var savedPage = GetSavedPagePosition(currentSurahNumber);
            if (savedPage > 0)
            {
                var bySequence = allPages.FindIndex(p => !p.IsBoundaryTransition && p.SequenceNumber == savedPage);
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

            return FirstRealPageIndex();
        }

        private int FirstRealPageIndex()
        {
            var index = allPages.FindIndex(p => !p.IsBoundaryTransition);
            return index >= 0 ? index : 0;
        }

        private int LastRealPageIndex()
        {
            var index = allPages.FindLastIndex(p => !p.IsBoundaryTransition);
            return index >= 0 ? index : 0;
        }

        private int FindPageIndexForAyah(int ayahNumber)
        {
            var index = allPages.FindIndex(p => !p.IsBoundaryTransition && ayahNumber >= p.FirstAyahNumber && ayahNumber <= p.LastAyahNumber);
            return index >= 0 ? index : FirstRealPageIndex();
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
            var currentPage = allPages[currentPageIndex];
            try
            {
                PageCarousel.ScrollTo(currentPage, position: ScrollToPosition.Center, animate: animated);
            }
            catch
            {
                PageCarousel.Position = currentPageIndex;
            }

            if (currentPage.IsBoundaryTransition)
            {
                UpdateCarouselSwipeState();
                return;
            }

            UpdatePageHeader();
            RefreshPageBookmarkIcon();
            SaveLastPosition(currentSurahNumber, currentPage.SequenceNumber);
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

        private async void OnBackToListClicked(object? sender, EventArgs e)
        {
            CancelChromeTimers();
            await Shell.Current.Navigation.PopAsync();
        }

        private async Task NavigateToSurahAsync(int surahNumber, QuranPageLoadTarget target = QuranPageLoadTarget.Default)
        {
            var targetSub = category.Subcategories.FirstOrDefault(s => (s.SurahNumber ?? 0) == surahNumber);
            if (targetSub == null)
            {
                return;
            }

            subCategory = targetSub;
            Title = targetSub.Name;
            await LoadSurahAsync(targetSub.SurahNumber, target: target);
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
            Page? bookmarksPage = null;
            bookmarksPage = new QuranBookmarksPage(async (surahNumber, ayahNumber) =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        // Close the bookmarks page first while it is still the top page,
                        // then reload this reader with the selected bookmark.
                        if (bookmarksPage is not null && Navigation.NavigationStack.Contains(bookmarksPage))
                        {
                            await Navigation.PopAsync();
                        }

                        var targetSub = category.Subcategories.FirstOrDefault(s => (s.SurahNumber ?? 0) == surahNumber);
                        if (targetSub != null)
                        {
                            subCategory = targetSub;
                            Title = targetSub.Name;
                        }

                        await LoadSurahAsync(surahNumber, ayahNumber);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[QuranReader] Bookmark navigation failed: {ex}");
                    }
                });
            });

            await Navigation.PushAsync(bookmarksPage);
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

            if (currentSurahAyahCount > 0 && (ayahNumber < 1 || ayahNumber > currentSurahAyahCount))
            {
                await DisplayAlertAsync(
                    GetReaderText("QuranReaderPage_AyahNotFoundTitle"),
                    string.Format(GetReaderText("QuranReaderPage_AyahNotFoundMessage"), ToLatinDigits(currentSurahAyahCount)),
                    AppResources.OK);
                return;
            }

            var pageIndex = FindPageIndexForAyah(ayahNumber);
            if (pageIndex < 0 || pageIndex >= allPages.Count)
            {
                await DisplayAlertAsync(GetReaderText("QuranReaderPage_AyahNotFoundTitle"), GetReaderText("QuranReaderPage_AyahNotFoundMessage"), AppResources.OK);
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
            if (allPages.Count > 0)
            {
                ResetInactivePageTransforms();
                UpdateCarouselSwipeState();
            }
        }

        private void OnCarouselCurrentItemChanged(object? sender, CurrentItemChangedEventArgs e)
        {
            if (e.CurrentItem is not QuranPageAssetItem page)
            {
                return;
            }

            // During a load the CarouselView briefly defaults its current item to the
            // first entry (which may be a previous-surah boundary page). Ignore those
            // transient changes and re-assert the intended landing page so we never
            // navigate to the wrong surah on open.
            if (ignoreCurrentItemChanges)
            {
                if (initialTargetPage != null && !ReferenceEquals(page, initialTargetPage))
                {
                    var target = initialTargetPage;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (!ReferenceEquals(initialTargetPage, target))
                        {
                            return;
                        }

                        var targetIndex = pageItems.IndexOf(target);
                        if (targetIndex < 0)
                        {
                            return;
                        }

                        try
                        {
                            PageCarousel.Position = targetIndex;
                            PageCarousel.ScrollTo(targetIndex, position: ScrollToPosition.Center, animate: false);
                        }
                        catch
                        {
                            // Ignore transient scroll failures during initialization.
                        }
                    });
                    return;
                }

                ignoreCurrentItemChanges = false;
                initialTargetPage = null;
            }

            if (page.IsBoundaryTransition)
            {
                // A boundary page shows the neighboring surah's edge page image. When the
                // user settles on it (after the initial load has finished), navigate into
                // that surah so swiping continues seamlessly across surah edges instead of
                // dead-ending. Re-entrancy is guarded by isNavigatingSurahBoundary/isLoading.
                currentPageIndex = allPages.FindIndex(p => ReferenceEquals(p, page));
                if (currentPageIndex < 0)
                {
                    currentPageIndex = page.IsPreviousSurahBoundary ? 0 : allPages.Count - 1;
                }

                ResetInactivePageTransforms();
                UpdateCarouselSwipeState();
                UpdateBoundaryPageHeader(page);
                RefreshPageBookmarkIcon();

                if (!isLoading && !isNavigatingSurahBoundary && page.TargetSurahNumber is int targetSurah)
                {
                    var boundaryTarget = page.IsPreviousSurahBoundary
                        ? QuranPageLoadTarget.LastPage
                        : QuranPageLoadTarget.FirstPage;
                    _ = NavigateToBoundarySurahAsync(targetSurah, boundaryTarget);
                }

                return;
            }

            var index = allPages.FindIndex(p => p.SequenceNumber == page.SequenceNumber && !p.IsBoundaryTransition);
            if (index < 0)
            {
                return;
            }

            currentPageIndex = index;
            ResetInactivePageTransforms();
            UpdateCarouselSwipeState();
            UpdatePageHeader();
            RefreshPageBookmarkIcon();
            SaveLastPosition(currentSurahNumber, allPages[currentPageIndex].SequenceNumber);
        }

        private void UpdateBoundaryPageHeader(QuranPageAssetItem boundaryPage)
        {
            var targetSub = category.Subcategories.FirstOrDefault(s => (s.SurahNumber ?? 0) == boundaryPage.TargetSurahNumber);
            if (targetSub != null)
            {
                SurahTitleLabel.Text = targetSub.Name;
            }

            SurahSubtitleLabel.Text = boundaryPage.IsPreviousSurahBoundary
                ? GetReaderText("QuranReaderPage_PreviousSurahSwipeHint")
                : GetReaderText("QuranReaderPage_NextSurahSwipeHint");

            PageIndicatorLabel.Text = string.Empty;
            MushafFooterLabel.Text = string.Empty;
        }

        private static readonly Dictionary<int, (int StartPage, int EndPage)> BoundaryPageMap = CreateBoundaryPageMap();

        private static Dictionary<int, (int StartPage, int EndPage)> CreateBoundaryPageMap()
        {
            // Canonical Madani mushaf page ranges for each surah.
            // Used to detect when adjacent surahs share a physical page so we can
            // avoid duplicated boundary transitions.
            var ranges = new Dictionary<int, (int StartPage, int EndPage)>
            {
                { 1, (1, 1) }, { 2, (2, 49) }, { 3, (50, 76) }, { 4, (77, 106) }, { 5, (106, 127) },
                { 6, (128, 150) }, { 7, (151, 176) }, { 8, (177, 186) }, { 9, (187, 207) }, { 10, (208, 220) },
                { 11, (221, 234) }, { 12, (235, 248) }, { 13, (249, 255) }, { 14, (256, 261) }, { 15, (262, 266) },
                { 16, (267, 281) }, { 17, (282, 293) }, { 18, (294, 304) }, { 19, (305, 312) }, { 20, (313, 321) },
                { 21, (322, 332) }, { 22, (332, 341) }, { 23, (342, 349) }, { 24, (350, 359) }, { 25, (359, 366) },
                { 26, (367, 376) }, { 27, (377, 385) }, { 28, (385, 396) }, { 29, (396, 404) }, { 30, (404, 410) },
                { 31, (411, 414) }, { 32, (415, 417) }, { 33, (418, 427) }, { 34, (428, 434) }, { 35, (434, 440) },
                { 36, (440, 445) }, { 37, (446, 452) }, { 38, (453, 458) }, { 39, (458, 467) }, { 40, (467, 477) },
                { 41, (477, 482) }, { 42, (483, 489) }, { 43, (489, 495) }, { 44, (496, 498) }, { 45, (499, 502) },
                { 46, (502, 506) }, { 47, (507, 510) }, { 48, (511, 515) }, { 49, (516, 517) }, { 50, (518, 520) },
                { 51, (520, 523) }, { 52, (523, 525) }, { 53, (526, 528) }, { 54, (528, 531) }, { 55, (531, 534) },
                { 56, (534, 537) }, { 57, (537, 541) }, { 58, (541, 545) }, { 59, (545, 548) }, { 60, (549, 551) },
                { 61, (551, 552) }, { 62, (553, 554) }, { 63, (554, 555) }, { 64, (556, 557) }, { 65, (557, 559) },
                { 66, (559, 561) }, { 67, (562, 564) }, { 68, (564, 566) }, { 69, (566, 568) }, { 70, (568, 570) },
                { 71, (570, 572) }, { 72, (572, 574) }, { 73, (574, 575) }, { 74, (575, 577) }, { 75, (577, 578) },
                { 76, (578, 580) }, { 77, (580, 581) }, { 78, (582, 583) }, { 79, (583, 584) }, { 80, (585, 586) },
                { 81, (586, 587) }, { 82, (587, 587) }, { 83, (587, 589) }, { 84, (589, 590) }, { 85, (590, 590) },
                { 86, (591, 591) }, { 87, (591, 592) }, { 88, (592, 592) }, { 89, (593, 593) }, { 90, (594, 594) },
                { 91, (594, 595) }, { 92, (595, 595) }, { 93, (596, 596) }, { 94, (596, 596) }, { 95, (597, 597) },
                { 96, (597, 597) }, { 97, (598, 598) }, { 98, (598, 599) }, { 99, (599, 599) }, { 100, (599, 600) },
                { 101, (600, 600) }, { 102, (600, 600) }, { 103, (601, 601) }, { 104, (601, 601) }, { 105, (601, 602) },
                { 106, (602, 602) }, { 107, (602, 602) }, { 108, (603, 603) }, { 109, (603, 603) }, { 110, (603, 603) },
                { 111, (604, 604) }, { 112, (604, 604) }, { 113, (604, 604) }, { 114, (604, 604) }
            };

            return ranges;
        }

        private void OnPageSingleTapped(object? sender, TappedEventArgs e)
        {
            ToggleChrome();
        }

        private void ToggleChrome()
        {
            var show = !TopChrome.IsVisible;
            CancelChromeTimers();
            TopChrome.IsVisible = show;
            BottomChrome.IsVisible = show;

            if (show)
            {
                ScheduleChromeAutoHide();
            }
        }

        private void OnPageDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (!TryResolvePageItem(sender, out var item))
            {
                return;
            }

            var minimumScale = item.MinimumZoomScale;
            if (item.ZoomScale <= minimumScale + 0.05)
            {
                item.ZoomScale = Math.Min(Math.Max(minimumScale * 1.8, PortraitDoubleTapZoomScale), MaximumZoomScale);
            }
            else
            {
                item.ResetTransform();
                item.ZoomScale = minimumScale;
            }

            ClampPan(item);
            UpdateCarouselSwipeState();
            CancelChromeTimers();
        }

        private void OnPagePinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
        {
            if (!TryResolvePageItem(sender, out var item))
            {
                return;
            }

            switch (e.Status)
            {
                case GestureStatus.Started:
                    isPinching = true;
                    item.ZoomStartScale = item.ZoomScale;
                    item.PinchOriginX = e.ScaleOrigin.X;
                    item.PinchOriginY = e.ScaleOrigin.Y;
                    PageCarousel.IsSwipeEnabled = false;
                    CancelChromeTimers();
                    break;

                case GestureStatus.Running:
                    var nextScale = Math.Clamp(item.ZoomScale * e.Scale, item.MinimumZoomScale, MaximumZoomScale);
                    var scaleDelta = nextScale / Math.Max(0.001, item.ZoomScale);
                    item.ZoomScale = nextScale;

                    var containerWidth = Math.Max(1, PageCarousel.Width);
                    var containerHeight = Math.Max(1, PageCarousel.Height);
                    var originX = (e.ScaleOrigin.X - 0.5) * containerWidth;
                    var originY = (e.ScaleOrigin.Y - 0.5) * containerHeight;
                    item.PanX = (item.PanX - originX) * scaleDelta + originX;
                    item.PanY = (item.PanY - originY) * scaleDelta + originY;

                    ClampPan(item);

                    if (nextScale <= item.MinimumZoomScale + 0.02)
                    {
                        item.ResetTransform();
                        item.ZoomScale = item.MinimumZoomScale;
                    }

                    UpdateCarouselSwipeState();
                    break;

                case GestureStatus.Canceled:
                case GestureStatus.Completed:
                    isPinching = false;
                    item.ZoomStartScale = item.ZoomScale;
                    if (item.ZoomScale <= item.MinimumZoomScale + 0.02)
                    {
                        item.ResetTransform();
                        item.ZoomScale = item.MinimumZoomScale;
                    }
                    ClampPan(item);

                    UpdateCarouselSwipeState();
                    break;
            }
        }

        private void OnPagePanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            if (isPinching)
            {
                return;
            }

            if (!TryResolvePageItem(sender, out var item))
            {
                return;
            }

            if (!HasPannableOverflow(item))
            {
                return;
            }

            var isZoomed = item.ZoomScale > item.MinimumZoomScale + 0.02;

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    item.PanStartX = item.PanX;
                    item.PanStartY = item.PanY;
                    if (isZoomed)
                    {
                        PageCarousel.IsSwipeEnabled = false;
                    }
                    CancelChromeTimers();
                    break;

                case GestureStatus.Running:
                    item.PanX = item.PanStartX + e.TotalX;
                    item.PanY = item.PanStartY + e.TotalY;
                    ClampPan(item);
                    break;

                case GestureStatus.Canceled:
                case GestureStatus.Completed:
                    ClampPan(item);
                    UpdateCarouselSwipeState();
                    break;
            }
        }

        private bool HasPannableOverflow(QuranPageAssetItem item)
        {
            var viewportSize = GetViewportRenderSize();
            var scaledWidth = viewportSize.Width * item.ZoomScale;
            var scaledHeight = viewportSize.Height * item.ZoomScale;
            return (scaledWidth - viewportSize.ViewportWidth) > 1 || (scaledHeight - viewportSize.ViewportHeight) > 1;
        }

        private void OnPagePointerPressed(object? sender, PointerEventArgs e)
        {
            isHoldingPage = true;
            BeginRevealChromeOnHold();
        }

        private void OnPagePointerReleased(object? sender, EventArgs e)
        {
            isHoldingPage = false;
            ScheduleChromeAutoHide();
        }

        private void BeginRevealChromeOnHold()
        {
            StopChromeRevealTimer();

            chromeRevealTimer = Dispatcher.CreateTimer();
            chromeRevealTimer.Interval = TimeSpan.FromMilliseconds(400);
            chromeRevealTimer.Tick += (_, _) =>
            {
                StopChromeRevealTimer();

                if (!isHoldingPage)
                {
                    return;
                }

                TopChrome.IsVisible = true;
                BottomChrome.IsVisible = true;
                ScheduleChromeAutoHide();
            };
            chromeRevealTimer.Start();
        }

        private void ScheduleChromeAutoHide()
        {
            StopChromeAutoHideTimer();

            chromeAutoHideTimer = Dispatcher.CreateTimer();
            chromeAutoHideTimer.Interval = TimeSpan.FromMilliseconds(2500);
            chromeAutoHideTimer.Tick += (_, _) =>
            {
                StopChromeAutoHideTimer();
                TopChrome.IsVisible = false;
                BottomChrome.IsVisible = false;
            };
            chromeAutoHideTimer.Start();
        }

        private void CancelChromeTimers()
        {
            StopChromeRevealTimer();
            StopChromeAutoHideTimer();
        }

        private void StopChromeRevealTimer()
        {
            if (chromeRevealTimer is null)
            {
                return;
            }

            chromeRevealTimer.Stop();
            chromeRevealTimer.Tick -= null;
            chromeRevealTimer = null;
        }

        private void StopChromeAutoHideTimer()
        {
            if (chromeAutoHideTimer is null)
            {
                return;
            }

            chromeAutoHideTimer.Stop();
            chromeAutoHideTimer.Tick -= null;
            chromeAutoHideTimer = null;
        }

        private void UpdateCarouselSwipeState()
        {
            var isZoomed = allPages.Count > 0
                && currentPageIndex >= 0
                && currentPageIndex < allPages.Count
                && allPages[currentPageIndex].ZoomScale > allPages[currentPageIndex].MinimumZoomScale + 0.02;

            PageCarousel.IsSwipeEnabled = !isZoomed;
        }

        private async Task NavigateToBoundarySurahAsync(int? surahNumber, QuranPageLoadTarget target)
        {
            if (isNavigatingSurahBoundary || surahNumber is null || surahNumber < 1 || surahNumber > 114)
            {
                return;
            }

            isNavigatingSurahBoundary = true;
            try
            {
                await NavigateToSurahAsync(surahNumber.Value, target);
            }
            finally
            {
                isNavigatingSurahBoundary = false;
            }
        }

        private bool TryGetNextSurahNumber(int surahNumber, out int nextSurahNumber)
        {
            nextSurahNumber = surahNumber + 1;
            if (nextSurahNumber > 114)
            {
                return false;
            }

            var candidate = nextSurahNumber;
            return category.Subcategories.Any(s => (s.SurahNumber ?? 0) == candidate);
        }

        private bool TryGetPreviousSurahNumber(int surahNumber, out int previousSurahNumber)
        {
            previousSurahNumber = surahNumber - 1;
            if (previousSurahNumber < 1)
            {
                return false;
            }

            var candidate = previousSurahNumber;
            return category.Subcategories.Any(s => (s.SurahNumber ?? 0) == candidate);
        }

        private static bool TryResolvePageItem(object? sender, out QuranPageAssetItem item)
        {
            item = null!;

            if (sender is Element element && element.BindingContext is QuranPageAssetItem directItem)
            {
                item = directItem;
                return true;
            }

            if (sender is GestureRecognizer gesture && gesture.Parent is Element parent && parent.BindingContext is QuranPageAssetItem parentItem)
            {
                item = parentItem;
                return true;
            }

            return false;
        }

        private void ResetInactivePageTransforms()
        {
            for (var i = 0; i < allPages.Count; i++)
            {
                if (i == currentPageIndex)
                {
                    continue;
                }

                allPages[i].ResetTransform();
                allPages[i].ZoomScale = allPages[i].MinimumZoomScale;
                AlignPageToTop(allPages[i]);
            }
        }

        private void ApplyOrientationZoomMode(bool resetCurrentPage)
        {
            var minimumScale = CalculateMinimumZoomScale();
            for (var i = 0; i < allPages.Count; i++)
            {
                var page = allPages[i];
                page.MinimumZoomScale = minimumScale;

                if (resetCurrentPage || i != currentPageIndex || page.ZoomScale < minimumScale)
                {
                    page.ResetTransform();
                    page.ZoomScale = minimumScale;
                    AlignPageToTop(page);
                }

                ClampPan(page);
            }

            UpdateCarouselSwipeState();
        }

        private void AlignPageToTop(QuranPageAssetItem item)
        {
            // In landscape the page is taller than the viewport; start reading from
            // the top of the page rather than its vertical center.
            if (!isLandscapeMode)
            {
                return;
            }

            var viewportSize = GetViewportRenderSize();
            var scaledHeight = viewportSize.Height * item.ZoomScale;
            var maxPanY = Math.Max(0, (scaledHeight - viewportSize.ViewportHeight) / 2);
            item.PanY = maxPanY;
            item.PanStartY = maxPanY;
        }

        private double CalculateMinimumZoomScale()
        {
            var viewportSize = GetViewportRenderSize();

            // Portrait keeps the whole page visible (AspectFit). In landscape the
            // page is scaled up so it fills the full width and can be scrolled
            // vertically to keep reading.
            if (!isLandscapeMode || viewportSize.Width <= 0)
            {
                return 1.0;
            }

            var fillWidthScale = viewportSize.ViewportWidth / viewportSize.Width;
            return Math.Clamp(fillWidthScale, 1.0, MaximumZoomScale);
        }

        private (double ViewportWidth, double ViewportHeight, double Width, double Height) GetViewportRenderSize()
        {
            var viewportWidth = Math.Max(1, PageCarousel.Width);
            var viewportHeight = Math.Max(1, PageCarousel.Height);

            var renderedWidth = viewportWidth;
            var renderedHeight = viewportWidth / ExpectedPageAspectRatio;
            if (renderedHeight > viewportHeight)
            {
                renderedHeight = viewportHeight;
                renderedWidth = viewportHeight * ExpectedPageAspectRatio;
            }

            return (viewportWidth, viewportHeight, renderedWidth, renderedHeight);
        }

        private void ClampPan(QuranPageAssetItem item)
        {
            var viewportSize = GetViewportRenderSize();

            var scaledWidth = viewportSize.Width * item.ZoomScale;
            var scaledHeight = viewportSize.Height * item.ZoomScale;
            var maxPanX = Math.Max(0, (scaledWidth - viewportSize.ViewportWidth) / 2);
            var maxPanY = Math.Max(0, (scaledHeight - viewportSize.ViewportHeight) / 2);

            item.PanX = Math.Clamp(item.PanX, -maxPanX, maxPanX);
            item.PanY = Math.Clamp(item.PanY, -maxPanY, maxPanY);
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
                PageIndicatorLabel.Text = string.Format(GetReaderText("QuranReaderPage_PageIndicatorFormat"), "0", "0");
                MushafFooterLabel.Text = string.Empty;
                return;
            }

            var page = allPages[currentPageIndex];
            var readingPages = allPages.Where(p => !p.IsBoundaryTransition).ToList();
            var readingIndex = readingPages.FindIndex(p => p.SequenceNumber == page.SequenceNumber);
            PageIndicatorLabel.Text = string.Format(GetReaderText("QuranReaderPage_PageIndicatorFormat"), ToLatinDigits(readingIndex + 1), ToLatinDigits(readingPages.Count));
            var range = page.FirstAyahNumber == page.LastAyahNumber
                ? ToLatinDigits(page.FirstAyahNumber)
                : ToLatinDigits(page.FirstAyahNumber) + " - " + ToLatinDigits(page.LastAyahNumber);
            MushafFooterLabel.Text = page.MushafPageNumber > 0
                ? string.Format(GetReaderText("QuranReaderPage_FooterCombinedFormat"),
                    string.Format(GetReaderText("QuranReaderPage_FooterPageFormat"), ToLatinDigits(page.MushafPageNumber)),
                    string.Format(GetReaderText("QuranReaderPage_FooterAyahsFormat"), range))
                : string.Format(GetReaderText("QuranReaderPage_FooterAyahsFormat"), range);
        }

        private void UpdateLocalizedHeaderTexts()
        {
            Title = subCategory.Name;
            FlowDirection = LocalizationService.GetFlowDirection();
        }

        private void ApplyLocalizedTexts()
        {
            PrevSurahButton.Text = AppResources.NavChevron;
            PrevSurahButton.Rotation = 180;
            NextSurahButton.Text = AppResources.NavChevron;
            NextSurahButton.Rotation = 0;
            MissingPackageTitleLabel.Text = GetReaderText("QuranReaderPage_PackageUnavailableTitle");
            MissingPackageMessageLabel.Text = GetReaderText("QuranReaderPage_PackageUnavailableMessage");
            RetryPackageButton.Text = GetReaderText("QuranReaderPage_Retry");
            LoadingMessageLabel.Text = GetReaderText("QuranReaderPage_LoadingSurahPages");
            AyahJumpEntry.Placeholder = GetReaderText("QuranReaderPage_AyahPlaceholder");
        }

        private static string GetReaderText(string key)
        {
            return AppResources.ResourceManager.GetString(key, AppResources.Culture) ?? string.Empty;
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

    public class QuranPageAssetItem : INotifyPropertyChanged
    {
        private double zoomScale = 1.0;
        private double panX;
        private double panY;

        public int SequenceNumber { get; set; }
        public int MushafPageNumber { get; set; }
        public int SurahNumber { get; set; }
        public int FirstAyahNumber { get; set; }
        public int LastAyahNumber { get; set; }
        public string PageImagePath { get; set; } = string.Empty;
        public bool IsImageMissing { get; set; }
        public bool IsBoundaryTransition { get; set; }
        public bool IsPreviousSurahBoundary { get; set; }
        public int? TargetSurahNumber { get; set; }
        public string MissingTitle { get; set; } = string.Empty;
        public string MissingMessage { get; set; } = string.Empty;

        public double ZoomScale
        {
            get => zoomScale;
            set
            {
                if (Math.Abs(zoomScale - value) < 0.001)
                {
                    return;
                }

                zoomScale = value;
                OnPropertyChanged();
            }
        }

        public double PanX
        {
            get => panX;
            set
            {
                if (Math.Abs(panX - value) < 0.001)
                {
                    return;
                }

                panX = value;
                OnPropertyChanged();
            }
        }

        public double PanY
        {
            get => panY;
            set
            {
                if (Math.Abs(panY - value) < 0.001)
                {
                    return;
                }

                panY = value;
                OnPropertyChanged();
            }
        }

        public double ZoomStartScale { get; set; } = 1.0;
        public double MinimumZoomScale { get; set; } = 1.0;
        public double PanStartX { get; set; }
        public double PanStartY { get; set; }
        public double PinchOriginX { get; set; } = 0.5;
        public double PinchOriginY { get; set; } = 0.5;

        public void ResetTransform()
        {
            ZoomScale = MinimumZoomScale;
            PanX = 0;
            PanY = 0;
            ZoomStartScale = MinimumZoomScale;
            PanStartX = 0;
            PanStartY = 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
