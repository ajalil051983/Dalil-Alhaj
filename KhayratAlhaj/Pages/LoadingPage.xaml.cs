namespace KhayratAlhaj.Pages
{
    public partial class LoadingPage : ContentPage
    {
        private bool navigationStarted;

        public LoadingPage()
        {
            InitializeComponent();

            // Set flow direction
            FlowDirection = Services.LocalizationService.GetFlowDirection();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (navigationStarted)
            {
                return;
            }

            navigationStarted = true;

            // Let the loading page render briefly before swapping to the main shell.
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(1500), NavigateToMainPage);
        }

        private void NavigateToMainPage()
        {
            try
            {
                if (Application.Current is null || navigationStarted is false)
                {
                    return;
                }

                var shell = new AppShell();

                if (Application.Current.Windows.Count > 0 && Application.Current.Windows[0] is not null)
                {
                    Application.Current.Windows[0].Page = shell;
                }
                else if (Application.Current.MainPage != shell)
                {
                    Application.Current.MainPage = shell;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadingPage] Navigation failed: {ex}");
            }
        }
    }
}
