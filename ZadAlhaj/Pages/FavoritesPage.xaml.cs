using ZadAlhaj.Models;
using ZadAlhaj.Services;
using ZadAlhaj.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace ZadAlhaj.Pages
{
    // ---------------------------------------------------------------------------
    // View-model for one row in the favorites list
    // ---------------------------------------------------------------------------
    public class FavoriteItem
    {
        public required Category Category { get; init; }
        public required SubCategory SubCategory { get; init; }

        public string SubCategoryName => SubCategory.Name;
        public string CategoryName => Category.Name;
        public string Icon => SubCategory.Icon;
        public string CategoryColor => Category.Color;
    }

    // ---------------------------------------------------------------------------
    // FavoritesPage
    // ---------------------------------------------------------------------------
    public partial class FavoritesPage : ContentPage
    {
        private readonly DataService _dataService;
        private readonly FavoritesService _favoritesService;
        private bool _isApplyingTheme;
        private AppTheme? _lastAppliedTheme;

        public FavoritesPage(DataService dataService)
        {
            InitializeComponent();
            FlowDirection = LocalizationService.GetFlowDirection();
            _dataService = dataService;
            _favoritesService = new FavoritesService();
            // Title is set via XAML binding to AppResources.FavoritesTitle
        }

        // -----------------------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------------------

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            ApplyThemeColors();

            WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, async (_, _) =>
            {
                await MainThread.InvokeOnMainThreadAsync(ApplyThemeColors);
            });

            await LoadFavoritesAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
        }

        // -----------------------------------------------------------------------
        // Data loading
        // -----------------------------------------------------------------------

        private async Task LoadFavoritesAsync()
        {
            try
            {
                var favoriteIds = _favoritesService.GetAllFavorites();

                if (favoriteIds.Count == 0)
                {
                    ShowEmptyState();
                    return;
                }

                // Build a flat lookup: subCategoryId → (category, subCategory)
                var allCategories = await _dataService.GetCategoriesAsync();
                var items = new List<FavoriteItem>();

                foreach (var category in allCategories)
                {
                    foreach (var sub in category.Subcategories)
                    {
                        if (favoriteIds.Contains(sub.Id))
                        {
                            items.Add(new FavoriteItem
                            {
                                Category = category,
                                SubCategory = sub
                            });
                        }
                    }
                }

                if (items.Count == 0)
                {
                    ShowEmptyState();
                    return;
                }

                FavoritesCollection.ItemsSource = items;
                FavoritesScroll.IsVisible = true;
                EmptyState.IsVisible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FavoritesPage] Error loading favorites: {ex.Message}");
                ShowEmptyState();
            }
        }

        private void ShowEmptyState()
        {
            FavoritesScroll.IsVisible = false;
            EmptyState.IsVisible = true;
        }

        // -----------------------------------------------------------------------
        // Navigation
        // -----------------------------------------------------------------------

        private async void OnFavoriteItemTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is FavoriteItem item)
            {
                await Navigation.PushAsync(new ContentDetailPage(item.Category, item.SubCategory));
            }
        }

        // -----------------------------------------------------------------------
        // Theming
        // -----------------------------------------------------------------------

        private void ApplyThemeColors()
        {
            if (_isApplyingTheme) return;

            var currentTheme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;
            var isDark = currentTheme == AppTheme.Dark;
            var effectiveTheme = isDark ? AppTheme.Dark : AppTheme.Light;

            if (_lastAppliedTheme == effectiveTheme) return;

            try
            {
                _isApplyingTheme = true;
                _lastAppliedTheme = effectiveTheme;

                BackgroundColor = isDark
                    ? Color.FromArgb("#1C1C1E")
                    : Color.FromArgb("#F5F5F5");

                if (FavoritesTitleLabel != null)
                    FavoritesTitleLabel.TextColor = isDark
                        ? Color.FromArgb("#F5F5F5")
                        : Color.FromArgb("#2C3E50");
            }
            finally
            {
                _isApplyingTheme = false;
            }
        }
    }
}
