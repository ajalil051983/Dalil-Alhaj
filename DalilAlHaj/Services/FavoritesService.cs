using System.Text.Json;

namespace DalilAlHaj.Services
{
    public class FavoritesService
    {
        private const string FavoritesKey = "favorites";
        private HashSet<int> favoriteIds = new();

        public FavoritesService()
        {
            LoadFavorites();
        }

        public bool IsFavorite(int subCategoryId)
        {
            return favoriteIds.Contains(subCategoryId);
        }

        public void AddFavorite(int subCategoryId)
        {
            favoriteIds.Add(subCategoryId);
            SaveFavorites();
        }

        public void RemoveFavorite(int subCategoryId)
        {
            favoriteIds.Remove(subCategoryId);
            SaveFavorites();
        }

        public void ToggleFavorite(int subCategoryId)
        {
            if (IsFavorite(subCategoryId))
                RemoveFavorite(subCategoryId);
            else
                AddFavorite(subCategoryId);
        }

        public List<int> GetAllFavorites()
        {
            return favoriteIds.ToList();
        }

        private void LoadFavorites()
        {
            var json = Preferences.Get(FavoritesKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                favoriteIds = JsonSerializer.Deserialize<HashSet<int>>(json) ?? new HashSet<int>();
            }
        }

        private void SaveFavorites()
        {
            var json = JsonSerializer.Serialize(favoriteIds);
            Preferences.Set(FavoritesKey, json);
        }
    }
}
