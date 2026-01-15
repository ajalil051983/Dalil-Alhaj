using NUnit.Framework;
using OpenQA.Selenium;

namespace DalilAlHaj.UITests.Tests
{
    [TestFixture]
    public class CategoriesTests : AppiumSetup
    {
        [Test]
        public void Categories_ShouldLoadAndDisplay()
        {
            // Wait for the main page to load
            Assert.IsNotNull(_driver.Context);

            // Find the list of categories (Assuming CollectionView or ListView has AutomationId or identifiable elements)
            // Since we don't have AutomationIds in the xaml provided, we search by text or class logic
            
            // Example: Check for a known category name like "Hajj" or "Umrah" (Adjust based on your json data)
            // Using XPath to find a TextView with text
            var categoryElement = _driver.FindElement(By.XPath("//android.widget.TextView[@text='Hajj']")); // Replace 'Hajj' with actual category name from categories.json
            
            Assert.IsNotNull(categoryElement, "Category 'Hajj' was not found on the main page.");
            Assert.IsTrue(categoryElement.Displayed, "Category 'Hajj' is not visible.");
        }

        [Test]
        public void SelectingCategory_ShouldNavigateToSubCategories()
        {
             // 1. Click on a category
            var categoryElement = _driver.FindElement(By.XPath("//android.widget.TextView[@text='Hajj']"));
            categoryElement.Click();

            // 2. Verified navigation occurred - check for SubCategory page title or specific element
            // Assuming the page title changes or a specific subcategory appears
            var subCategoryPageTitle = _driver.FindElement(By.XPath("//android.widget.TextView[@text='Hajj']")); // Verify title matches
            Assert.IsTrue(subCategoryPageTitle.Displayed);
        }
    }
}
