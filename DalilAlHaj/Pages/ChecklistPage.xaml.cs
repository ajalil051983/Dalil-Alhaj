using System.Collections.ObjectModel;
using System.Text.Json;
using DalilAlHaj.Resources.Localization;
using DalilAlHaj.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace DalilAlHaj.Pages
{
    public class ChecklistItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }

    public partial class ChecklistPage : ContentPage
    {
        private const string ChecklistKey = "hajj_checklist";
        private ObservableCollection<ChecklistItem> checklistItems = new();
        private bool isApplyingTheme = false;
        private AppTheme? lastAppliedTheme = null;

        public ChecklistPage()
        {
            InitializeComponent();
            FlowDirection = Services.LocalizationService.GetFlowDirection();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Load data asynchronously to prevent UI blocking
            if (checklistItems.Count == 0)
            {
                LoadChecklist();
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
            
                this.BackgroundColor = isDark ? Color.FromArgb("#1C1C1E") : Color.FromArgb("#F5F5F5");
            
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

            ChecklistCollection.ItemsSource = checklistItems;
            UpdateProgress();
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
