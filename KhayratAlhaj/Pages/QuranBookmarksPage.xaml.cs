using KhayratAlhaj.Services;

namespace KhayratAlhaj.Pages
{
    public partial class QuranBookmarksPage : ContentPage
    {
        private readonly QuranBookmarkService bookmarkService;
        private readonly Func<int, int, Task> onBookmarkSelected;

        public QuranBookmarksPage(Func<int, int, Task> onBookmarkSelected)
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

            // Let the caller decide whether to pop or push a new page.
            // This avoids the bookmarks page popping the reader that was just opened.
            await onBookmarkSelected(item.SurahNumber, item.AyahNumber);
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
