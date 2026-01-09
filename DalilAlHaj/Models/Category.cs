using DalilAlHaj.Services;

namespace DalilAlHaj.Models
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
        public bool HasAudio { get; set; } = false;

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
