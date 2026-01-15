using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System.Threading;

namespace DalilAlHaj.UITests.Tests
{
    [TestFixture]
    public class FavoritesTests : AppiumSetup
    {
        [Test]
        public void AddToFavorites_ShouldPersist_AndShowInFavoritesList()
        {
            // 1. Navigate to a category and subcategory
            var categoryElement = _driver.FindElement(By.XPath("//android.widget.TextView[@text='Umrah']")); // Replace with valid Category
            categoryElement.Click();

            Thread.Sleep(1000); // Small wait for animation

            // 2. Select a subcategory
            var subCategoryElement = _driver.FindElement(By.XPath("//android.widget.TextView[@text='Tawaf']")); // Replace with valid SubCategory
            subCategoryElement.Click();

            Thread.Sleep(1000);

            // 3. On Detail Page, Click Favorite Button
            // Using XPath for content-desc (AutomationId) as AppiumBy might be resolving issues
            var favoriteButton = _driver.FindElement(By.XPath("//*[@content-desc='FavoriteButtonID']"));
            favoriteButton.Click();

            // 4. Navigate back to Main Page
            _driver.Navigate().Back();
            _driver.Navigate().Back();

            // 5. Go to Settings or Favorites Page (Assuming access via TabBar or Button)
            // If it's a menu item:
            // var menuButton = _driver.FindElementByAccessibilityId("MenuButton");
            // menuButton.Click();
            // var favoritesLink = _driver.FindElementByXPath("//*[@text='Favorites']");
            // favoritesLink.Click();

            // NOTE: Update logic to match actual navigation to favorites
        }
    }
}
