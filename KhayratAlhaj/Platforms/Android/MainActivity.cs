using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using System.Diagnostics.CodeAnalysis;

namespace KhayratAlhaj
{
    [Activity(Name = "com.ilafalkhayr.zadalhaj.MainActivity", Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Locale | ConfigChanges.LayoutDirection)]
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int NotificationPermissionRequestCode = 9001;
        private bool _notificationPermissionRequested = false;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Initialize platform-specific services
            Platform.Init(this, savedInstanceState);
        }

        protected override void OnPostResume()
        {
            base.OnPostResume();

            // Request POST_NOTIFICATIONS at the earliest point where the Activity is
            // fully interactive (after onResume + all pending fragments are executed).
            if (!_notificationPermissionRequested && Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                _notificationPermissionRequested = true;

                if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.PostNotifications)
                    != Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(this,
                        new[] { Android.Manifest.Permission.PostNotifications },
                        NotificationPermissionRequestCode);
                }
            }
        }

        protected override void OnDestroy()
        {
            try
            {
                base.OnDestroy();
            }
            catch (ObjectDisposedException ex) when (ex.Message.Contains("IServiceProvider"))
            {
                // Ignore IServiceProvider disposal exception during activity destruction
                // This is a known MAUI Android issue in ShellFragmentContainer
                System.Diagnostics.Debug.WriteLine($"Caught and ignored IServiceProvider disposal in OnDestroy: {ex.Message}");
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
