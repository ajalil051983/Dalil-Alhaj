using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using KhayratAlhaj.Resources.Localization;
using KhayratAlhaj.Messages;
using KhayratAlhaj.Services;
using KhayratAlhaj.Models;
using CommunityToolkit.Mvvm.Messaging;

namespace KhayratAlhaj.Pages
{
    public class ChecklistItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }

        [JsonIgnore]
        public int CategoryId { get; set; }

        [JsonIgnore]
        public int SubCategoryId { get; set; }

        [JsonIgnore]
        public bool HasNavigationTarget => CategoryId > 0;
    }

    public partial class ChecklistPage : ContentPage
    {
        private const string ChecklistKey = "hajj_checklist";
        private ObservableCollection<ChecklistItem> checklistItems = new();
        private bool isApplyingTheme = false;
        private AppTheme? lastAppliedTheme = null;
        private readonly DataService dataService;

        // (categoryId, subCategoryId) for each of the 16 default checklist items.
        // Aligned with the current categories.json structure:
        //   Category 1 "Preparation for Hajj": 101 Definition, 102 Preparation before flying
        //     (health/clothing/hygiene/money/logistics/spiritual prep), 104 Documents & vaccinations.
        //   Category 2 "Hajj Rituals": 201-208 cover the rites in chronological order.
        // subCategoryId == 0 means navigate to the category list page only.
        private static readonly (int CategoryId, int SubCategoryId)[] ItemNavMap =
        {
            (1, 101),  // 1  - Learn rules of Hajj
            (1, 102),  // 2  - Repentance / spiritual preparation
            (1, 104),  // 3  - Prepare documents
            (1, 102),  // 4  - Prepare ihram clothes & luggage
            (1, 102),  // 5  - Medicines & first aid
            (2, 201),  // 6  - Ihram from miqat
            (2, 202),  // 7  - Enter Mecca & Tawaf
            (2, 202),  // 8  - Sa'i between Safa and Marwah
            (2, 203),  // 9  - Go to Mina (Day of Tarwiyah)
            (2, 204),  // 10 - Station at Arafat
            (2, 205),  // 11 - Pass night at Muzdalifah
            (2, 206),  // 12 - Stone Jamrat al-Aqabah (Day of Nahr)
            (2, 206),  // 13 - Shaving / shortening (Day of Nahr)
            (2, 206),  // 14 - Tawaf al-Ifadah (Day of Nahr)
            (2, 207),  // 15 - Stoning during days of Tashreeq
            (2, 208),  // 16 - Farewell Tawaf
        };

        public ChecklistPage(DataService dataService)
        {
            InitializeComponent();
            FlowDirection = Services.LocalizationService.GetFlowDirection();
            this.dataService = dataService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Ensure page direction follows current language on every appearance.
            FlowDirection = Services.LocalizationService.GetFlowDirection();
            
            // Load data asynchronously to prevent UI blocking
            if (checklistItems.Count == 0)
            {
                LoadChecklist();
            }
            else
            {
                // If language changed while page remained alive, refresh localized text immediately.
                LocalizeChecklistItems();
                ChecklistCollection.ItemsSource = null;
                ChecklistCollection.ItemsSource = checklistItems;
                UpdateProgress();
            }
            
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
            
                // Theme colors now handled by AppThemeBinding in XAML with static resources
                // No need to manually update CollectionView items
            }
            finally
            {
                isApplyingTheme = false;
            }
        }

        // Removed UpdateLayoutColors - theme now handled efficiently via XAML AppThemeBinding with static resources

        private void LoadChecklist()
        {
            // Try to load saved checklist
            var json = Preferences.Get(ChecklistKey, string.Empty);
            
            if (!string.IsNullOrEmpty(json))
            {
                checklistItems = JsonSerializer.Deserialize<ObservableCollection<ChecklistItem>>(json) 
                    ?? GetDefaultChecklist();
            }
            else
            {
                checklistItems = GetDefaultChecklist();
            }

            // Saved checklist stores previously rendered text. Re-apply localized
            // titles/descriptions for the current language while keeping completion state.
            LocalizeChecklistItems();

            ApplyCategoryMappings();
            ChecklistCollection.ItemsSource = checklistItems;
            UpdateProgress();
        }

        private void LocalizeChecklistItems()
        {
            var localizedDefaults = GetDefaultChecklist();
            var count = Math.Min(checklistItems.Count, localizedDefaults.Count);

            for (int i = 0; i < count; i++)
            {
                checklistItems[i].Title = localizedDefaults[i].Title;
                checklistItems[i].Description = localizedDefaults[i].Description;
            }
        }

        private ObservableCollection<ChecklistItem> GetDefaultChecklist()
        {
            // Titles and descriptions localized via resource keys
            return new ObservableCollection<ChecklistItem>
            {
                new ChecklistItem { Title = AppResources.ChecklistItem1Title, Description = AppResources.ChecklistItem1Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem2Title, Description = AppResources.ChecklistItem2Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem3Title, Description = AppResources.ChecklistItem3Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem4Title, Description = AppResources.ChecklistItem4Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem5Title, Description = AppResources.ChecklistItem5Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem6Title, Description = AppResources.ChecklistItem6Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem7Title, Description = AppResources.ChecklistItem7Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem8Title, Description = AppResources.ChecklistItem8Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem9Title, Description = AppResources.ChecklistItem9Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem10Title, Description = AppResources.ChecklistItem10Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem11Title, Description = AppResources.ChecklistItem11Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem12Title, Description = AppResources.ChecklistItem12Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem13Title, Description = AppResources.ChecklistItem13Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem14Title, Description = AppResources.ChecklistItem14Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem15Title, Description = AppResources.ChecklistItem15Desc },
                new ChecklistItem { Title = AppResources.ChecklistItem16Title, Description = AppResources.ChecklistItem16Desc }
            };
        }

        private void OnCheckChanged(object? sender, CheckedChangedEventArgs e)
        {
            SaveChecklist();
            UpdateProgress();
        }

        private void ApplyCategoryMappings()
        {
            for (int i = 0; i < checklistItems.Count && i < ItemNavMap.Length; i++)
            {
                checklistItems[i].CategoryId    = ItemNavMap[i].CategoryId;
                checklistItems[i].SubCategoryId = ItemNavMap[i].SubCategoryId;
            }
        }

        private async void OnNavigateClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is ChecklistItem item && item.CategoryId > 0)
            {
                var category = await dataService.GetCategoryByIdAsync(item.CategoryId);
                if (category == null) return;

                if (item.SubCategoryId > 0)
                {
                    var sub = category.Subcategories.FirstOrDefault(sc => sc.Id == item.SubCategoryId);
                    if (sub != null)
                    {
                        await Navigation.PushAsync(new ContentDetailPage(category, sub));
                        return;
                    }
                }

                await Navigation.PushAsync(new SubCategoryPage(category, dataService));
            }
        }

        private void SaveChecklist()
        {
            var json = JsonSerializer.Serialize(checklistItems);
            Preferences.Set(ChecklistKey, json);
        }

        private void UpdateProgress()
        {
            var completed = checklistItems.Count(item => item.IsCompleted);
            var total = checklistItems.Count;
            var percentage = total > 0 ? (double)completed / total : 0;
            
            ProgressLabel.Text = string.Format(AppResources.CompletedOf, completed, total);
            MainProgressBar.Progress = percentage;
            PercentageLabel.Text = $"{percentage:P0}";
        }
    }
}
