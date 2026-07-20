using KhayratAlhaj.Services;

namespace KhayratAlhaj.Pages
{
    public partial class QuranBookmarksPage : ContentPage
    {
        private readonly QuranBookmarkService bookmarkService;
        private readonly Action<int, int> onBookmarkSelected;

        public QuranBookmarksPage(Action<int, int> onBookmarkSelected)
        {
            InitializeComponent();
            FlowDirection = LocalizationService.GetFlowDirection();
            bookmarkService = new QuranBookmarkService();
            this.onBookmarkSelected = onBookmarkSelected;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            FlowDirection = LocalizationService.GetFlowDirection();
            LoadBookmarks();
        }

        private void LoadBookmarks()
        {
            var items = bookmarkService.GetAll()
                .Select(b => new QuranBookmarkViewItem
                {
                    SurahNumber = b.SurahNumber,
                    AyahNumber = b.AyahNumber,
                    Title = b.DisplayTitle,
                    Preview = b.AyahText
                })
                .ToList();

            BookmarksCollection.ItemsSource = items;
            EmptyState.IsVisible = items.Count == 0;
        }

        private async void OnBookmarkTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is not QuranBookmarkViewItem item)
            {
                return;
            }

            onBookmarkSelected(item.SurahNumber, item.AyahNumber);
            await Navigation.PopAsync();
        }
    }

    public class QuranBookmarkViewItem
    {
        public int SurahNumber { get; set; }
        public int AyahNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Preview { get; set; } = string.Empty;
    }
}
