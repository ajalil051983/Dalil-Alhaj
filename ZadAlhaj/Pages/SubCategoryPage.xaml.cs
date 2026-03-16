using ZadAlhaj.Models;
using ZadAlhaj.Services;
using ZadAlhaj.Messages;
using CommunityToolkit.Mvvm.Messaging;
#if ANDROID
using AndroidView = Android.Views.View;
using AndroidViewGroup = Android.Views.ViewGroup;
#endif

namespace ZadAlhaj.Pages
{
    public partial class SubCategoryPage : ContentPage
    {
        private readonly Category category;
        private readonly DataService dataService;
        private bool isApplyingTheme = false;
        private AppTheme? lastAppliedTheme = null;

        public SubCategoryPage(Category category, DataService dataService)
        {
            InitializeComponent();
            FlowDirection = LocalizationService.GetFlowDirection();
            this.category = category;
            this.dataService = dataService;
            
            Title = category.Name;
            LoadSubCategories();
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
#pragma warning restore CS0618
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
            
                // Update TopicsLabel directly
                if (TopicsLabel != null)
                {
                    TopicsLabel.TextColor = ThemeColors.PrimaryText(isDark);
                }
            
                if (this.Content is ScrollView scrollView && 
                    scrollView.Content is VerticalStackLayout stack)
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
                    // Keep category header frame colored, update subcategory frames
                    // HeaderFrame is now a Border, so all frames should be themed
                    frame.BackgroundColor = ThemeColors.CardBackground(isDark);

                    if (frame.Content is Layout frameLayout)
                    {
                        UpdateLayoutColors(frameLayout, isDark);
                    }
                }
                else if (child is Label label)
                {
                    // Skip header labels
                    if (label == CategoryIcon || label == CategoryTitle)
                    {
                        continue;
                    }
                    
                    if (label.TextColor == ThemeColors.PrimaryTextLight || 
                        label.TextColor == ThemeColors.ContentTextLight ||
                        label.TextColor == ThemeColors.PrimaryTextDark ||
                        label.TextColor == null)
                    {
                        label.TextColor = ThemeColors.PrimaryText(isDark);
                    }
                }
                else if (child is CollectionView collectionView)
                {
                    // Update CollectionView item templates
                    UpdateCollectionViewItems(collectionView, isDark);
                }
                else if (child is Layout nestedLayout)
                {
                    UpdateLayoutColors(nestedLayout, isDark);
                }
            }
        }
        
        private void UpdateCollectionViewItems(CollectionView collectionView, bool isDark)
        {
#if ANDROID
            // Get the handler for the CollectionView to access rendered items
            if (collectionView.Handler?.PlatformView is AndroidView androidView)
            {
                UpdateAndroidViewRecursive(androidView, isDark);
            }
#endif
        }

#if ANDROID
        private void UpdateAndroidViewRecursive(AndroidView view, bool isDark)
        {
            if (view is AndroidViewGroup viewGroup)
            {
                for (int i = 0; i < viewGroup.ChildCount; i++)
                {
                    var child = viewGroup.GetChildAt(i);
                    if (child != null)
                    {
                        UpdateAndroidViewRecursive(child, isDark);
                    }
                }
            }
            
            // Try to get the MAUI element from the platform view
            if (view.GetType().GetProperty("VirtualView")?.GetValue(view) is View mauiView)
            {
                UpdateMauiElement(mauiView, isDark);
            }
        }
#endif
        
        private void UpdateMauiElement(View element, bool isDark)
        {
            if (element is Frame frame)
            {
                frame.BackgroundColor = ThemeColors.CardBackground(isDark);

                                if (frame.Content is Layout layout)
                {
                    UpdateLayoutColors(layout, isDark);
                }
            }
            else if (element is Layout layout)
            {
                UpdateLayoutColors(layout, isDark);
            }
        }
#pragma warning restore CS0618

        private void LoadSubCategories()
        {
            // Set header colors and content
            HeaderFrame.BackgroundColor = Color.FromArgb(category.Color);
            CategoryIcon.Text = category.Icon;
            CategoryTitle.Text = category.Name;
            CategoryTitle.TextColor = Colors.White; // Header text always white

            // Load subcategories
            SubCategoriesCollection.ItemsSource = category.Subcategories;
            
            // Apply theme colors after items are loaded
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100); // Small delay to ensure items are rendered
                ApplyThemeColors();
            });
        }

        private async void OnSubCategoryTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is SubCategory subCategory)
            {
                await Navigation.PushAsync(new ContentDetailPage(category, subCategory));
            }
        }
    }
}
