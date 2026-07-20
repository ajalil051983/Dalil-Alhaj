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
            PackageUrlEntry.Text = installerService.GetSavedPackageUrl();
        }

        protected override void OnDisappearing()
        {
            installCancellation?.Cancel();
            installCancellation?.Dispose();
            installCancellation = null;
            base.OnDisappearing();
        }

        private async void OnDownloadAndInstallClicked(object? sender, EventArgs e)
        {
            if (isInstalling)
            {
                return;
            }

            var packageUrl = PackageUrlEntry.Text?.Trim() ?? string.Empty;
            installerService.SavePackageUrl(packageUrl);

            await RunInstallAsync(
                cancellationToken => installerService.DownloadAndInstallAsync(
                    packageUrl,
                    new Progress<double>(UpdateProgress),
                    new Progress<string>(UpdateStatus),
                    cancellationToken));
        }

        private async void OnPickZipClicked(object? sender, EventArgs e)
        {
            if (isInstalling)
            {
                return;
            }

            try
            {
                var file = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "اختر ملف ZIP للمصحف"
                });

                if (file == null)
                {
                    return;
                }

                var isZip = string.Equals(Path.GetExtension(file.FileName), ".zip", StringComparison.OrdinalIgnoreCase);
                if (!isZip)
                {
                    await DisplayAlertAsync("ملف غير صالح", "يجب اختيار ملف بصيغة ZIP.", "حسناً");
                    return;
                }

                await RunInstallAsync(cancellationToken => installerService.InstallFromZipAsync(
                    file.FullPath,
                    new Progress<string>(UpdateStatus),
                    cancellationToken));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("خطأ", $"تعذر اختيار الملف: {ex.Message}", "حسناً");
            }
        }

        private async Task RunInstallAsync(Func<CancellationToken, Task<QuranPackageInstallResult>> installAction)
        {
            if (isInstalling)
            {
                return;
            }

            isInstalling = true;
            installCancellation = new CancellationTokenSource();

            InstallProgressBar.IsVisible = true;
            InstallProgressBar.Progress = 0;
            StatusLabel.Text = "جاري التنفيذ...";

            try
            {
                var result = await installAction(installCancellation.Token);
                if (!result.IsSuccess)
                {
                    await DisplayAlertAsync("فشل التثبيت", result.Message, "حسناً");
                    return;
                }

                StatusLabel.Text = result.Message;
                InstallProgressBar.Progress = 1;
                await DisplayAlertAsync("تم", result.Message, "متابعة");

                await Navigation.PopAsync();

                if (onInstalled != null)
                {
                    await onInstalled();
                }
            }
            finally
            {
                installCancellation.Dispose();
                installCancellation = null;
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
    }
}
