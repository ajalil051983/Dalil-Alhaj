namespace ZadAlhaj
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
            // Add global exception handler
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            
#if DEBUG && ANDROID
            try
            {
                var src = Path.Combine(FileSystem.AppDataDirectory, "ZadAlhaj.db3");
                var downloads = Android.OS.Environment.GetExternalStoragePublicDirectory(
                    Android.OS.Environment.DirectoryDownloads)!.AbsolutePath;
                var dst = Path.Combine(downloads, "ZadAlhaj.db3");
                if (File.Exists(src)) File.Copy(src, dst, overwrite: true);
                System.Diagnostics.Debug.WriteLine($"[DB EXPORTED] {dst}");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB EXPORT FAILED] {ex.Message}"); }
#endif

            // Initialize language
            Services.LocalizationService.InitializeLanguage();
            
            // Initialize theme from preferences
            var isDarkMode = Preferences.Get("dark_mode", false);
            UserAppTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;

#if ANDROID
            // Force Android night mode to match app preference
            // so that AppThemeBinding follows UserAppTheme, not the system setting
            AndroidX.AppCompat.App.AppCompatDelegate.DefaultNightMode = isDarkMode
                ? AndroidX.AppCompat.App.AppCompatDelegate.ModeNightYes
                : AndroidX.AppCompat.App.AppCompatDelegate.ModeNightNo;
#endif
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UNHANDLED EXCEPTION: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                // Ignore ObjectDisposedException for IServiceProvider during app shutdown
                // This is a known MAUI Android issue in ShellFragmentContainer.OnDestroy
                if (ex is ObjectDisposedException && ex.Message.Contains("IServiceProvider"))
                {
                    System.Diagnostics.Debug.WriteLine("Ignoring IServiceProvider disposal exception during shutdown");
                    return;
                }
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"UNOBSERVED TASK EXCEPTION: {e.Exception.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Message: {e.Exception.Message}");
            if (e.Exception.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner exception: {e.Exception.InnerException.Message}");
            }
            e.SetObserved(); // Prevent app crash
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new Pages.LoadingPage());
            window.Title = "Zad Alhaj";
            window.FlowDirection = Services.LocalizationService.GetFlowDirection();
            return window;
        }
    }
}