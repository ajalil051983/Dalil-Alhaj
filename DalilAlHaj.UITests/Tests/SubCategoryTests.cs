using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System.Threading;

namespace DalilAlHaj.UITests.Tests
{
    [TestFixture]
    public class SubCategoryTests : AppiumSetup
    {
        [Test]
        public void SubCategory_ShouldListItems()
        {
            // 1. Navigate to a category (e.g., Umrah)
            var categoryElement = _driver.FindElement(By.XPath("//android.widget.TextView[@text='Umrah']")); 
            categoryElement.Click();
            Thread.Sleep(1000);

            // 2. Check for Collection View of subcategories
            var collection = _driver.FindElement(By.XPath("//*[@content-desc='SubCategoriesCollectionID']"));
            Assert.IsNotNull(collection);

            // 3. Check for at least one sub-item (e.g. Ihram, Tawaf)
            // Searching by text is often easiest for dynamic lists
            var subItem = _driver.FindElement(By.XPath("//android.widget.TextView[@text='Tawaf']"));
            Assert.IsNotNull(subItem);

            // Go Back
            _driver.Navigate().Back();
        }
    }
}
