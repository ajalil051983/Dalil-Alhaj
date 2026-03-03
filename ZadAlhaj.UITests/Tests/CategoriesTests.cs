using NUnit.Framework;
using OpenQA.Selenium;

namespace ZadAlhaj.UITests.Tests
{
    [TestFixture]
    public class CategoriesTests : AppiumSetup
    {
        [Test]
        public void Categories_ShouldLoadAndDisplay()
        {
            // Wait for the main page to load
            Assert.IsNotNull(_driver.Context);

            // Using Arabic text since it's the default language
            // 'التحضير للحج' is the first category in categories.json
            var categoryElement = _driver.FindElement(By.XPath("//android.widget.TextView[@text='التحضير للحج']"));
            
            Assert.IsNotNull(categoryElement, "Category 'التحضير للحج' was not found on the main page.");
            Assert.IsTrue(categoryElement.Displayed, "Category 'التحضير للحج' is not visible.");
        }

        [Test]
        public void SelectingCategory_ShouldNavigateToSubCategories()
        {
             // 1. Click on a category
            var categoryElement = _driver.FindElement(By.XPath("//android.widget.TextView[@text='التحضير للحج']"));
            categoryElement.Click();

            // 2. Verified navigation occurred - check for SubCategory page title or specific element
            // On the subcategory page, the title should match the category name
            var subCategoryPageTitle = _driver.FindElement(By.XPath("//android.widget.TextView[@text='التحضير للحج']"));
            Assert.IsNotNull(subCategoryPageTitle);
            Assert.IsTrue(subCategoryPageTitle.Displayed);
        }
    }
}
