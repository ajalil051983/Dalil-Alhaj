namespace DalilAlHaj.Pages
{
    public partial class LoadingPage : ContentPage
    {
        public LoadingPage()
        {
            InitializeComponent();
            
            // Set flow direction
            FlowDirection = Services.LocalizationService.GetFlowDirection();
            
            NavigateToMainPage();
        }

        private async void NavigateToMainPage()
        {
            // Show loading page for 2 seconds
            await Task.Delay(2000);
            
            // Navigate to main page using the Window
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }
        }
    }
}
