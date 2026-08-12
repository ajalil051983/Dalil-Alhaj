using KhayratAlhaj.Resources.Localization;
using KhayratAlhaj.Services;

namespace KhayratAlhaj.Pages
{
    public partial class QuranPackagePage : ContentPage
    {
        private readonly QuranPackageInstallerService installerService;
        private readonly Func<Task>? onInstalled;
        private bool isInstalling;
        private CancellationTokenSource? installCancellation;

        public QuranPackagePage(Func<Task>? onInstalled = null)
        {
            InitializeComponent();
            installerService = new QuranPackageInstallerService();
            this.onInstalled = onInstalled;
            FlowDirection = LocalizationService.GetFlowDirection();
            ApplyLocalizedTexts();
        }

        protected override void OnDisappearing()
        {
            var cancellation = installCancellation;
            installCancellation = null;
            cancellation?.Cancel();
            cancellation?.Dispose();
            base.OnDisappearing();
        }

        private async void OnDownloadAndInstallClicked(object? sender, EventArgs e)
        {
            if (isInstalling)
            {
                return;
            }

            var packageUrl = installerService.GetSavedPackageUrl();

            await RunInstallAsync(
                cancellationToken => installerService.DownloadAndInstallAsync(
                    packageUrl,
                    new Progress<double>(UpdateProgress),
                    new Progress<string>(UpdateStatus),
                    cancellationToken));
        }

        private async Task RunInstallAsync(Func<CancellationToken, Task<QuranPackageInstallResult>> installAction)
        {
            if (isInstalling)
            {
                return;
            }

            isInstalling = true;
            var cancellation = new CancellationTokenSource();
            installCancellation = cancellation;

            InstallProgressBar.IsVisible = true;
            InstallProgressBar.Progress = 0;
            StatusLabel.Text = GetText("QuranPackagePage_InProgress");

            try
            {
                var result = await installAction(cancellation.Token);
                if (!result.IsSuccess)
                {
                    await DisplayAlertAsync(GetText("QuranPackagePage_InstallFailedTitle"), result.Message, GetText("Common_OK", "OK"));
                    return;
                }

                StatusLabel.Text = result.Message;
                InstallProgressBar.Progress = 1;
                await DisplayAlertAsync(GetText("QuranPackagePage_SuccessTitle"), result.Message, GetText("QuranPackagePage_Continue"));

                await Navigation.PopAsync();

                if (onInstalled != null)
                {
                    await onInstalled();
                }
            }
            finally
            {
                if (ReferenceEquals(installCancellation, cancellation))
                {
                    installCancellation = null;
                }

                cancellation.Dispose();
                isInstalling = false;
            }
        }

        private void UpdateProgress(double value)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                InstallProgressBar.IsVisible = true;
                InstallProgressBar.Progress = Math.Clamp(value, 0, 1);
            });
        }

        private void UpdateStatus(string value)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = value;
            });
        }

        private void ApplyLocalizedTexts()
        {
            Title = GetText("QuranPackagePage_Title");
            IntroLabel.Text = GetText("QuranPackagePage_Intro");
            DescriptionLabel.Text = GetText("QuranPackagePage_Description");
            DownloadInstallButton.Text = GetText("QuranPackagePage_DownloadInstall");
        }

        private static string GetText(string key)
        {
            return GetText(key, string.Empty);
        }

        private static string GetText(string key, string fallback)
        {
            return AppResources.ResourceManager.GetString(key, AppResources.Culture) ?? fallback;
        }
    }
}
