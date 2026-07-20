using KhayratAlhaj.Services;

namespace KhayratAlhaj.Models
{
    public class DhikrItem
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string EnTranslation { get; set; } = string.Empty;
        public string FrTranslation { get; set; } = string.Empty;
        public int InitialTimes { get; set; } = 1;

        public string DisplayText
        {
            get
            {
                var language = LocalizationService.GetCurrentLanguage();
                return GetTextForLanguage(language);
            }
        }

        public string GetTextForLanguage(string language)
        {
            return language switch
            {
                "en" => !string.IsNullOrWhiteSpace(EnTranslation) ? EnTranslation : Text,
                "fr" => !string.IsNullOrWhiteSpace(FrTranslation) ? FrTranslation : Text,
                _ => !string.IsNullOrWhiteSpace(Text) ? Text : EnTranslation
            };
        }
    }
}