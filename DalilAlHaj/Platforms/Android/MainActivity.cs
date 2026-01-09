using Android.App;
using Android.Content.PM;
using Android.OS;

namespace DalilAlHaj
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Initialize platform-specific services
            Platform.Init(this, savedInstanceState);
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
