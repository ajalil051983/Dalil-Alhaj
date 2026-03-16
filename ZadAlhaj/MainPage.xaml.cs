using ZadAlhaj.Models;
using ZadAlhaj.Services;
using ZadAlhaj.Pages;
using System.Text.Json;
using ZadAlhaj.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace ZadAlhaj
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

                // Set button text with emoji prefixes (avoids XC0025 compiled binding warnings)
                PrayerTimesButton.Text = $"🕌 {ZadAlhaj.Resources.Localization.AppResources.PrayerTimes}";
                FavoritesButton.Text = $"⭐ {ZadAlhaj.Resources.Localization.AppResources.Favorites}";

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
                        var title = ZadAlhaj.Resources.Localization.AppResources.ChecklistTitle;
                        var progressText = $"{completed}/{total} - {percentage}%";
                        
                        ChecklistButton.Text = $"{title}\n{progressText}";
                        System.Diagnostics.Debug.WriteLine($"UpdateChecklistProgress: Set to '{title}\\n{progressText}'");
                    }
                }
                else
                {
                    var title = ZadAlhaj.Resources.Localization.AppResources.ChecklistTitle;
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
                        var title = ZadAlhaj.Resources.Localization.AppResources.ChecklistTitle;
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
                            ZadAlhaj.Resources.Localization.AppResources.Error, 
                            $"{ex.Message}", 
                            ZadAlhaj.Resources.Localization.AppResources.OK);
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

        private async void OnFavoritesClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new FavoritesPage(dataService));
        }

        private async void OnPrayerTimesClicked(object? sender, EventArgs e)
        {
            // Show loading spinner on the button immediately, hide text
            PrayerTimesButton.IsEnabled = false;
            PrayerTimesButton.TextColor = Colors.Transparent;
            PrayerTimesSpinner.IsVisible = true;
            PrayerTimesSpinner.IsRunning = true;

            try
            {
                await Navigation.PushAsync(new PrayerTimesPage());
            }
            finally
            {
                // Reset button state when returning from PrayerTimesPage
                PrayerTimesButton.IsEnabled = true;
                PrayerTimesButton.TextColor = Colors.White;
                PrayerTimesSpinner.IsVisible = false;
                PrayerTimesSpinner.IsRunning = false;
            }
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
                    MainGrid.BackgroundColor = ThemeColors.PageBackground(isDark);
                    
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
                    SearchBar.TextColor = ThemeColors.PrimaryText(isDark);
                    SearchBar.PlaceholderColor = ThemeColors.PlaceholderText(isDark);
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
                        label.TextColor = ThemeColors.PrimaryText(isDark);
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
                    if (label.TextColor == ThemeColors.PrimaryTextLight || 
                        label.TextColor == ThemeColors.ContentTextLight ||
                        label.TextColor == ThemeColors.PrimaryTextDark ||
                        label.TextColor == null)
                    {
                        var newColor = ThemeColors.PrimaryText(isDark);
                        label.TextColor = newColor;
                        System.Diagnostics.Debug.WriteLine($"UpdateLayoutColors: Label text color set to {newColor}");
                    }
                    else if (label.TextColor == ThemeColors.SecondaryTextLight ||
                             label.TextColor == ThemeColors.SecondaryTextDark)
                    {
                        label.TextColor = ThemeColors.SecondaryText(isDark);
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
                                         currentColor == ThemeColors.CardBackgroundLight || 
                                         currentColor == ThemeColors.CardBackgroundDark ||
                                         currentColor == Colors.Transparent ||
                                         currentColor.ToHex() == "#FFFFFF" ||
                                         currentColor.ToHex() == ThemeColors.CardBackgroundDark.ToHex();

                    if (isNeutralFrame)
                    {
                        var newColor = ThemeColors.CardBackground(isDark);
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
                        searchBar.TextColor = ThemeColors.PrimaryText(isDark);
                        searchBar.PlaceholderColor = ThemeColors.PlaceholderText(isDark);
                        System.Diagnostics.Debug.WriteLine($"UpdateLayoutColors: SearchBar in frame updated");
                    }
                }
                else if (child is SearchBar searchBar)
                {
                    searchBar.TextColor = ThemeColors.PrimaryText(isDark);
                    searchBar.PlaceholderColor = ThemeColors.PlaceholderText(isDark);
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
