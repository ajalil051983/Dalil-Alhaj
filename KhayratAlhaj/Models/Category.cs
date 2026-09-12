using KhayratAlhaj.Services;

namespace KhayratAlhaj.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string NameFr { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = "#3498DB";
        public List<SubCategory> Subcategories { get; set; } = new();

        // The icon can be an emoji or an image file (e.g. "quran_icon.svg" packaged as a MauiImage).
        public bool IconIsImage
            => Icon.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || Icon.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
        public ImageSource? IconImageSource => IconIsImage ? ImageSource.FromFile(Icon) : null;

        public string Name
        {
            get
            {
                try
                {
                    var lang = LocalizationService.GetCurrentLanguage();
                    return lang switch
                    {
                        "en" => !string.IsNullOrEmpty(NameEn) ? NameEn : NameAr,
                        "fr" => !string.IsNullOrEmpty(NameFr) ? NameFr : NameAr,
                        _ => !string.IsNullOrEmpty(NameAr) ? NameAr : NameEn
                    };
                }
                catch
                {
                    return NameAr ?? NameEn ?? NameFr ?? string.Empty;
                }
            }
        }
    }

    public class SubCategory
    {
        public int Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string NameFr { get; set; } = string.Empty;
        public string Icon { get; set; } = "📖";
        public string Content { get; set; } = string.Empty;
        public int? SurahNumber { get; set; }
        public string ApiLookupName { get; set; } = string.Empty;
        public bool HasAudioAr { get; set; } = false;
        public bool HasAudioEn { get; set; } = false;
        public bool HasAudioFr { get; set; } = false;

        // The icon can be an emoji or an image file (e.g. "quran_icon.svg" packaged as a MauiImage).
        public bool IconIsImage
            => Icon.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || Icon.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
        public ImageSource? IconImageSource => IconIsImage ? ImageSource.FromFile(Icon) : null;

        public bool HasAudioForCurrentLanguage
        {
            get
            {
                try
                {
                    return LocalizationService.GetCurrentLanguage() switch
                    {
                        "en" => HasAudioEn,
                        "fr" => HasAudioFr,
                        _ => HasAudioAr
                    };
                }
                catch
                {
                    return HasAudioAr;
                }
            }
        }

        public string Name
        {
            get
            {
                try
                {
                    var lang = LocalizationService.GetCurrentLanguage();
                    return lang switch
                    {
                        "en" => !string.IsNullOrEmpty(NameEn) ? NameEn : NameAr,
                        "fr" => !string.IsNullOrEmpty(NameFr) ? NameFr : NameAr,
                        _ => !string.IsNullOrEmpty(NameAr) ? NameAr : NameEn
                    };
                }
                catch
                {
                    return NameAr ?? NameEn ?? NameFr ?? string.Empty;
                }
            }
        }
    }
}
