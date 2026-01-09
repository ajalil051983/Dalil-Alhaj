using DalilAlHaj.Models;
using DalilAlHaj.Services;
using DalilAlHaj.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace DalilAlHaj.Pages
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
            
                this.BackgroundColor = isDark ? Color.FromArgb("#1C1C1E") : Color.FromArgb("#F5F5F5");
            
                // Update SearchInput colors directly
                if (SearchInput != null)
                {
                    SearchInput.TextColor = isDark ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#2C3E50");
                    SearchInput.PlaceholderColor = isDark ? Color.FromArgb("#98989D") : Color.FromArgb("#95A5A6");
                }
            
                // Update results label
                if (ResultsLabel != null)
                {
                    ResultsLabel.TextColor = isDark ? Color.FromArgb("#AEAEB2") : Color.FromArgb("#7F8C8D");
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
                    frame.BackgroundColor = isDark ? Color.FromArgb("#2C2C2E") : Colors.White;
                    
                    if (frame.Content is Layout frameLayout)
                    {
                        UpdateLayoutColors(frameLayout, isDark);
                    }
                }
                else if (child is Label label)
                {
                    if (label.TextColor == Color.FromArgb("#2C3E50") || 
                        label.TextColor == Color.FromArgb("#34495E"))
                    {
                        label.TextColor = isDark ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#2C3E50");
                    }
                    else if (label.TextColor == Color.FromArgb("#7F8C8D"))
                    {
                        label.TextColor = isDark ? Color.FromArgb("#AEAEB2") : Color.FromArgb("#7F8C8D");
                    }
                    else if (label.TextColor == Color.FromArgb("#3498DB"))
                    {
                        // Keep blue for category names
                    }
                }
                else if (child is SearchBar searchBar)
                {
                    searchBar.TextColor = isDark ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#2C3E50");
                    searchBar.PlaceholderColor = isDark ? Color.FromArgb("#98989D") : Color.FromArgb("#95A5A6");
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
                ResultsLabel.Text = DalilAlHaj.Resources.Localization.AppResources.SearchPrompt;
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
                ResultsLabel.Text = DalilAlHaj.Resources.Localization.AppResources.SearchPrompt;
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
            ResultsLabel.Text = string.Format(DalilAlHaj.Resources.Localization.AppResources.Results, results.Count);
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
