using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System.Threading;

namespace ZadAlhaj.UITests.Tests
{
    [TestFixture]
    public class SubCategoryTests : AppiumSetup
    {
        private const string Pkg = "com.ilafalkhayr.zadalhaj:id/";

        [Test]
        public void SubCategory_ShouldListItems()
        {
            // 1. Navigate to the first category (Arabic: التحضير للحج)
            var categoryElement = _driver.FindElement(By.XPath("//android.widget.TextView[@text='التحضير للحج']"));
            categoryElement.Click();
            Thread.Sleep(3000);

            // 2. Check for Collection View of subcategories
            var collection = _driver.FindElement(By.Id(Pkg + "SubCategoriesCollectionID"));
            Assert.IsNotNull(collection, "SubCategories collection not found.");

            // 3. Check for at least one sub-item (Arabic: شروط الحج)
            var subItem = _driver.FindElement(By.XPath("//android.widget.TextView[@text='شروط الحج']"));
            Assert.IsNotNull(subItem, "Subcategory 'شروط الحج' not found.");

            // Go Back
            _driver.Navigate().Back();
        }
    }
}
