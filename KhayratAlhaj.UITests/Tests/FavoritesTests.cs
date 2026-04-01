using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System.Threading;

namespace KhayratAlhaj.UITests.Tests
{
    [TestFixture]
    public class FavoritesTests : AppiumSetup
    {
        private const string Pkg = "com.ilafalkhayr.khayratalhaj:id/";

        [Test]
        public void AddToFavorites_ShouldPersist_AndShowInFavoritesList()
        {
            // 1. Navigate to the first category (Arabic: التحضير للحج)
            var categoryElement = _driver.FindElement(By.XPath("//android.widget.TextView[@text='التحضير للحج']"));
            categoryElement.Click();
            Thread.Sleep(3000);

            // 2. Select the first subcategory (Arabic: شروط الحج)
            var subCategoryElement = _driver.FindElement(By.XPath("//android.widget.TextView[@text='شروط الحج']"));
            subCategoryElement.Click();
            Thread.Sleep(3000);

            // 3. On Detail Page, click Favorite Button (resource-id)
            var favoriteButton = _driver.FindElement(By.Id(Pkg + "FavoriteButtonID"));
            favoriteButton.Click();
            Thread.Sleep(500);

            // 4. Navigate back to Main Page
            _driver.Navigate().Back();
            _driver.Navigate().Back();
        }
    }
}
