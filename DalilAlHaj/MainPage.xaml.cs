using DalilAlHaj.Models;
using DalilAlHaj.Services;
using DalilAlHaj.Pages;
using System.Text.Json;
using DalilAlHaj.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace DalilAlHaj
{
    // Simple class for deserializing checklist progress
    public class ChecklistItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }

    public partial class MainPage : ContentPage
    {
        private readonly DataService dataService;
        private const string ChecklistKey = "hajj_checklist";
        private string? lastLoadedLanguage = null;
        private bool isDisposed = false;
        private bool isApplyingTheme = false; // Prevent redundant theme applications
        private AppTheme? lastAppliedTheme = null; // Track last applied theme

        public MainPage()
        {
            try
            {
                InitializeComponent();
                
                // Set flow direction based on current language
                FlowDirection = LocalizationService.GetFlowDirection();
                
                // Apply theme-aware colors
                ApplyThemeColors();
                
                dataService = new DataService();
                // Don't call LoadCategories here - it will be called in OnAppearing
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in MainPage constructor: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Don't set isDisposed or clear ItemsSource when just navigating
            // Only when the page is actually being removed from navigation stack
            
            // Unsubscribe from theme changes
            WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
            
            System.Diagnostics.Debug.WriteLine("MainPage.OnDisappearing: Page hidden but keeping data");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            isDisposed = false;
            
            try
            {
                System.Diagnostics.Debug.WriteLine("MainPage.OnAppearing: Starting...");
                
                // Update flow direction in case language changed
                FlowDirection = LocalizationService.GetFlowDirection();
                System.Diagnostics.Debug.WriteLine($"MainPage.OnAppearing: FlowDirection set to {FlowDirection}");
                
                // Subscribe to theme changes
                WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, async (recipient, message) =>
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ApplyThemeColors();
                    });
                });
                
                // Check if theme changed while page was hidden and apply it
                ApplyThemeColors();
                
                UpdateChecklistProgress();
                
                // Always reload categories when page appears (or if language changed)
                var currentLanguage = LocalizationService.GetCurrentLanguage();
                System.Diagnostics.Debug.WriteLine($"MainPage.OnAppearing: Current language = {currentLanguage}, Last loaded = {lastLoadedLanguage}");
                
                if (lastLoadedLanguage != currentLanguage || CategoriesCollection?.ItemsSource == null)
                {
                    System.Diagnostics.Debug.WriteLine("MainPage.OnAppearing: Loading categories...");
                    LoadCategories();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MainPage.OnAppearing: Categories already loaded.");
                    // Theme colors will be applied automatically via system AppTheme and bindings
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in MainPage.OnAppearing: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void UpdateChecklistProgress()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("UpdateChecklistProgress: Starting...");
                
                if (ChecklistButton == null)
                {
                    System.Diagnostics.Debug.WriteLine("UpdateChecklistProgress: ChecklistButton is NULL!");
                    return;
                }
                
                var json = Preferences.Get(ChecklistKey, string.Empty);
                if (!string.IsNullOrEmpty(json))
                {
                    var checklist = JsonSerializer.Deserialize<List<ChecklistItem>>(json);
                    if (checklist != null)
                    {
                        var completed = checklist.Count(item => item.IsCompleted);
                        var total = checklist.Count;
                        var percentage = total > 0 ? (int)((double)completed / total * 100) : 0;
                        
                        // Format: "✅ قائمة مهام الحج\n2/16 - 20%"
                        var title = DalilAlHaj.Resources.Localization.AppResources.ChecklistTitle;
                        var progressText = $"{completed}/{total} - {percentage}%";
                        
                        ChecklistButton.Text = $"{title}\n{progressText}";
                        System.Diagnostics.Debug.WriteLine($"UpdateChecklistProgress: Set to '{title}\\n{progressText}'");
                    }
                }
                else
                {
                    var title = DalilAlHaj.Resources.Localization.AppResources.ChecklistTitle;
                    ChecklistButton.Text = $"{title}\n0/0 - 0%";
                    System.Diagnostics.Debug.WriteLine($"UpdateChecklistProgress: Set to '{title}\\n0/0 - 0%'");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateChecklistProgress: {ex.Message}");
                
                try
                {
                    if (ChecklistButton != null)
                    {
                        var title = DalilAlHaj.Resources.Localization.AppResources.ChecklistTitle;
                        ChecklistButton.Text = $"{title}\n0/0 - 0%";
                    }
                }
                catch
                {
                    // Ignore secondary error
                }
            }
        }

        private async void OnSearchBarPressed(object? sender, EventArgs e)
        {
            var searchText = SearchBar.Text;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                await Navigation.PushAsync(new SearchPage(dataService, searchText));
            }
        }

        private async void LoadCategories()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("LoadCategories: Starting...");
                
                if (isDisposed)
                {
                    System.Diagnostics.Debug.WriteLine("LoadCategories: Page is disposed, skipping...");
                    return;
                }
                
                if (dataService == null)
                {
                    System.Diagnostics.Debug.WriteLine("LoadCategories: dataService is NULL!");
                    return;
                }
                
                var categories = await dataService.GetCategoriesAsync();
                System.Diagnostics.Debug.WriteLine($"LoadCategories: Loaded {categories?.Count ?? 0} categories");
                
                // Check if page was disposed during async operation
                if (isDisposed || CategoriesCollection == null)
                {
                    System.Diagnostics.Debug.WriteLine("LoadCategories: Page disposed or CategoriesCollection is NULL!");
                    return;
                }
                
                CategoriesCollection.ItemsSource = categories;
                lastLoadedLanguage = LocalizationService.GetCurrentLanguage();
                System.Diagnostics.Debug.WriteLine($"LoadCategories: Completed. Language: {lastLoadedLanguage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (!isDisposed)
                {
                    try
                    {
                        await DisplayAlertAsync(
                            DalilAlHaj.Resources.Localization.AppResources.Error, 
                            $"{ex.Message}", 
                            DalilAlHaj.Resources.Localization.AppResources.OK);
                    }
                    catch (Exception alertEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error showing alert: {alertEx.Message}");
                    }
                }
            }
        }

        private async void OnCategoryTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is Category category)
            {
                await Navigation.PushAsync(new SubCategoryPage(category, dataService));
            }
        }

        private async void OnMapClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new MapPage());
        }

        private async void OnSearchClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new SearchPage(dataService));
        }

        private async void OnChecklistClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new ChecklistPage());
        }

        private async void OnSettingsClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new SettingsPage());
        }

        private void ApplyThemeColors()
        {
            // Prevent redundant theme applications
            if (isApplyingTheme)
            {
                System.Diagnostics.Debug.WriteLine("ApplyThemeColors: Already applying theme, skipping...");
                return;
            }

            var currentTheme = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;
            var isDark = currentTheme == AppTheme.Dark || 
                         (currentTheme == AppTheme.Unspecified && 
                          Application.Current?.RequestedTheme == AppTheme.Dark);

            // Skip if theme hasn't changed
            var effectiveTheme = isDark ? AppTheme.Dark : AppTheme.Light;
            if (lastAppliedTheme == effectiveTheme)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyThemeColors: Theme unchanged ({effectiveTheme}), skipping...");
                return;
            }

            try
            {
                isApplyingTheme = true;
                lastAppliedTheme = effectiveTheme;
                
                System.Diagnostics.Debug.WriteLine($"ApplyThemeColors: Applying theme - isDark = {isDark}, UserAppTheme = {currentTheme}");

                // Update MainGrid background
                if (MainGrid != null)
                {
                    MainGrid.BackgroundColor = isDark 
                        ? Color.FromArgb("#1C1C1E") 
                        : Color.FromArgb("#F5F5F5");
                    
                    System.Diagnostics.Debug.WriteLine($"ApplyThemeColors: MainGrid background set to {MainGrid.BackgroundColor}");
                    
                    // Update ScrollView and its content
                    if (MainGrid.Children.Count > 1 && MainGrid.Children[1] is ScrollView scrollView)
                    {
                        UpdateScrollViewColors(scrollView, isDark);
                    }
                }
                
                // Update SearchBar colors directly
                if (SearchBar != null)
                {
                    SearchBar.TextColor = isDark ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#2C3E50");
                    SearchBar.PlaceholderColor = isDark ? Color.FromArgb("#98989D") : Color.FromArgb("#95A5A6");
                    System.Diagnostics.Debug.WriteLine($"ApplyThemeColors: SearchBar colors set - Text: {SearchBar.TextColor}, Placeholder: {SearchBar.PlaceholderColor}");
                }
            }
            finally
            {
                isApplyingTheme = false;
            }
        }

        private void UpdateScrollViewColors(ScrollView scrollView, bool isDark)
        {
            if (scrollView.Content is VerticalStackLayout stack)
            {
                // Update the "Choose Topic" label directly if it exists
                foreach (var child in stack.Children)
                {
                    if (child is Label label && label.FontSize == 20 && label.FontAttributes.HasFlag(FontAttributes.Bold))
                    {
                        label.TextColor = isDark ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#2C3E50");
                    }
                }
                
                UpdateLayoutColors(stack, isDark);
            }
        }

#pragma warning disable CS0618 // Frame is obsolete
        private void UpdateLayoutColors(Layout layout, bool isDark)
        {
            foreach (var child in layout.Children)
            {
                if (child is Label label)
                {
                    // Update text colors (but skip category name labels which are white on colored backgrounds)
                    if (label.TextColor == Color.FromArgb("#2C3E50") || 
                        label.TextColor == Color.FromArgb("#34495E") ||
                        label.TextColor == Color.FromArgb("#F5F5F5") ||
                        label.TextColor == null)
                    {
                        var newColor = isDark ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#2C3E50");
                        label.TextColor = newColor;
                        System.Diagnostics.Debug.WriteLine($"UpdateLayoutColors: Label text color set to {newColor}");
                    }
                    else if (label.TextColor == Color.FromArgb("#7F8C8D") ||
                             label.TextColor == Color.FromArgb("#AEAEB2"))
                    {
                        label.TextColor = isDark ? Color.FromArgb("#AEAEB2") : Color.FromArgb("#7F8C8D");
                    }
                }
                else if (child is Frame frame)
                {
                    // Update search bar frame and other white frames
                    // Don't update category frames (they have custom colors from binding)
                    var currentColor = frame.BackgroundColor;
                    
                    System.Diagnostics.Debug.WriteLine($"UpdateLayoutColors: Frame current color: {currentColor}");
                    
                    // Check if it's a white/transparent frame or already a dark frame - update it
                    // Skip frames with custom colors (categories have hex colors like #3498DB, etc.)
                    bool isNeutralFrame = currentColor == null || 
                                         currentColor == Colors.White || 
                                         currentColor == Color.FromArgb("#2C2C2E") ||
                                         currentColor == Colors.Transparent ||
                                         currentColor.ToHex() == "#FFFFFF" ||
                                         currentColor.ToHex() == "#2C2C2E";
                    
                    if (isNeutralFrame)
                    {
                        var newColor = isDark ? Color.FromArgb("#2C2C2E") : Colors.White;
                        frame.BackgroundColor = newColor;
                        System.Diagnostics.Debug.WriteLine($"UpdateLayoutColors: Frame background set to {newColor}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"UpdateLayoutColors: Skipping frame with custom color {currentColor}");
                    }
                    
                    if (frame.Content is Layout frameLayout)
                    {
                        UpdateLayoutColors(frameLayout, isDark);
                    }
                    else if (frame.Content is SearchBar searchBar)
                    {
                        searchBar.TextColor = isDark ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#2C3E50");
                        searchBar.PlaceholderColor = isDark ? Color.FromArgb("#98989D") : Color.FromArgb("#95A5A6");
                        System.Diagnostics.Debug.WriteLine($"UpdateLayoutColors: SearchBar in frame updated");
                    }
                }
                else if (child is SearchBar searchBar)
                {
                    searchBar.TextColor = isDark ? Color.FromArgb("#F5F5F5") : Color.FromArgb("#2C3E50");
                    searchBar.PlaceholderColor = isDark ? Color.FromArgb("#98989D") : Color.FromArgb("#95A5A6");
                    System.Diagnostics.Debug.WriteLine($"UpdateLayoutColors: SearchBar updated");
                }
                else if (child is CollectionView)
                {
                    // CollectionView (categories) - items use data binding, keep their custom colors
                    System.Diagnostics.Debug.WriteLine($"UpdateLayoutColors: Skipping CollectionView");
                }
                else if (child is Layout nestedLayout)
                {
                    UpdateLayoutColors(nestedLayout, isDark);
                }
            }
        }
#pragma warning restore CS0618
    }
}
