using ZadAlhaj.Models;
using ZadAlhaj.Services;
using ZadAlhaj.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace ZadAlhaj.Pages
{
    public class SearchResult
    {
        public Category Category { get; set; } = null!;
        public SubCategory SubCategory { get; set; } = null!;
    public string CategoryName => Category.Name;
    public string SubCategoryName => SubCategory.Name;
        public string Icon => SubCategory.Icon;
        public string ContentPreview => SubCategory.Content.Length > 100 
            ? SubCategory.Content.Substring(0, 100) + "..." 
            : SubCategory.Content;
    }

    public partial class SearchPage : ContentPage
    {
        private readonly DataService dataService;
        private List<Category> allCategories = new();
        private string? initialSearchText;
        private bool isApplyingTheme = false;
        private AppTheme? lastAppliedTheme = null;

        public SearchPage(DataService dataService, string? searchText = null)
        {
            InitializeComponent();
            FlowDirection = LocalizationService.GetFlowDirection();
            this.dataService = dataService;
            this.initialSearchText = searchText;
            LoadData();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ApplyThemeColors();
            
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
            if (isApplyingTheme) return;

            var currentTheme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;
            var isDark = currentTheme == AppTheme.Dark;
            var effectiveTheme = isDark ? AppTheme.Dark : AppTheme.Light;
            
            if (lastAppliedTheme == effectiveTheme) return;

            try
            {
                isApplyingTheme = true;
                lastAppliedTheme = effectiveTheme;
            
                this.BackgroundColor = ThemeColors.PageBackground(isDark);
            
                // Update SearchInput colors directly
                if (SearchInput != null)
                {
                    SearchInput.TextColor = ThemeColors.PrimaryText(isDark);
                    SearchInput.PlaceholderColor = ThemeColors.PlaceholderText(isDark);
                }
            
                // Update results label
                if (ResultsLabel != null)
                {
                    ResultsLabel.TextColor = ThemeColors.SecondaryText(isDark);
                }
            
                // Update content
                if (this.Content is VerticalStackLayout stack)
                {
                    UpdateLayoutColors(stack, isDark);
                }
            }
            finally
            {
                isApplyingTheme = false;
            }
        }

#pragma warning disable CS0618 // Frame is obsolete
        private void UpdateLayoutColors(Layout layout, bool isDark)
        {
            foreach (var child in layout.Children)
            {
                if (child is Frame frame)
                {
                    // Search bar frame and result frames
                    frame.BackgroundColor = ThemeColors.CardBackground(isDark);
                    
                    if (frame.Content is Layout frameLayout)
                    {
                        UpdateLayoutColors(frameLayout, isDark);
                    }
                }
                else if (child is Label label)
                {
                    // Match both light and dark values so dark→light transitions also work
                    if (label.TextColor == ThemeColors.PrimaryTextLight || 
                        label.TextColor == ThemeColors.ContentTextLight ||
                        label.TextColor == ThemeColors.PrimaryTextDark)
                    {
                        label.TextColor = ThemeColors.PrimaryText(isDark);
                    }
                    else if (label.TextColor == ThemeColors.SecondaryTextLight ||
                             label.TextColor == ThemeColors.SecondaryTextDark)
                    {
                        label.TextColor = ThemeColors.SecondaryText(isDark);
                    }
                    else if (label.TextColor == Color.FromArgb("#3498DB"))
                    {
                        // Keep blue for category names
                    }
                }
                else if (child is SearchBar searchBar)
                {
                    searchBar.TextColor = ThemeColors.PrimaryText(isDark);
                    searchBar.PlaceholderColor = ThemeColors.PlaceholderText(isDark);
                }
                else if (child is CollectionView)
                {
                    // CollectionView items will be updated via data template
                }
                else if (child is Layout nestedLayout)
                {
                    UpdateLayoutColors(nestedLayout, isDark);
                }
            }
        }
#pragma warning restore CS0618

        private async void LoadData()
        {
            allCategories = await dataService.GetCategoriesAsync();
            
            // If initial search text was provided, perform search
            if (!string.IsNullOrWhiteSpace(initialSearchText))
            {
                SearchInput.Text = initialSearchText;
                PerformSearch(initialSearchText);
            }
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                ResultsCollection.ItemsSource = null;
                ResultsLabel.Text = ZadAlhaj.Resources.Localization.AppResources.SearchPrompt;
            }
        }

        private void OnSearchClicked(object? sender, EventArgs e)
        {
            PerformSearch(SearchInput.Text);
        }

        private void PerformSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                ResultsCollection.ItemsSource = null;
                ResultsLabel.Text = ZadAlhaj.Resources.Localization.AppResources.SearchPrompt;
                return;
            }

            var results = new List<SearchResult>();
            query = query.Trim().ToLower();

            foreach (var category in allCategories)
            {
                foreach (var subCategory in category.Subcategories)
                {
                    if (subCategory.NameAr.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        subCategory.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new SearchResult
                        {
                            Category = category,
                            SubCategory = subCategory
                        });
                    }
                }
            }

            ResultsCollection.ItemsSource = results;
            ResultsLabel.Text = string.Format(ZadAlhaj.Resources.Localization.AppResources.Results, results.Count);
        }

        private async void OnResultTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is SearchResult result)
            {
                await Navigation.PushAsync(new ContentDetailPage(result.Category, result.SubCategory));
            }
        }
    }
}
