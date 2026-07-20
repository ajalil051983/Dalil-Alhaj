namespace KhayratAlhaj
{
    public partial class AppShell : Shell
    {
        private bool _startupPermissionsRequested = false;

        public AppShell()
        {
            try
            {
                InitializeComponent();

                // Set flow direction based on language
                FlowDirection = Services.LocalizationService.GetFlowDirection();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing AppShell: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Request notification + location permissions once after main UI is visible.
            if (!_startupPermissionsRequested)
            {
                _startupPermissionsRequested = true;
                _ = RequestStartupPermissionsAsync();
            }
        }

        private async Task RequestStartupPermissionsAsync()
        {
            // Small delay so the Shell UI is fully settled before showing permission dialogs.
            await Task.Delay(1500);

            // --- Location permission ---
            // Request here so prayer times can auto-detect position on first use
            // without making the user manually tap "Use my location" first.
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }
                System.Diagnostics.Debug.WriteLine(
                    $"[AppShell] Location permission: {status}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AppShell] Location permission error: {ex.Message}");
            }
        }
    }
}
