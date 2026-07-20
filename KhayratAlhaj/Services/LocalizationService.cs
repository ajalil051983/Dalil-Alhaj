using System.Globalization;
using KhayratAlhaj.Resources.Localization;

namespace KhayratAlhaj.Services
{
    public static class LocalizationService
    {
        private const string LanguageKey = "app_language";

        public static void SetLanguage(string languageCode)
        {
            Preferences.Set(LanguageKey, languageCode);
            var culture = new CultureInfo(languageCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            AppResources.Culture = culture;
        }

        public static string GetCurrentLanguage()
        {
            return Preferences.Get(LanguageKey, "ar");
        }

        public static void InitializeLanguage()
        {
            var savedLanguage = GetCurrentLanguage();
            SetLanguage(savedLanguage);
        }

        public static FlowDirection GetFlowDirection()
        {
            var language = GetCurrentLanguage();
            return language == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }
    }
}
