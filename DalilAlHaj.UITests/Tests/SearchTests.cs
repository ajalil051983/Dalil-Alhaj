using NUnit.Framework;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium;

namespace DalilAlHaj.UITests.Tests
{
    [TestFixture]
    public class SearchTests : AppiumSetup
    {
        [Test]
        public void Search_ShouldFilterCategories()
        {
            // 1. Find Search Bar
            // Using XPath for content-desc (AutomationId)
            var searchBar = _driver.FindElement(By.XPath("//*[@content-desc='SearchBarID']"));
            
            // 2. Type text
            searchBar.SendKeys("Hajj");
            // Trigger search if needed (Press Enter)
            // _driver.PressKeyCode(AndroidKeyCode.Enter);

            // 3. Verify results
            var result = _driver.FindElement(By.XPath("//android.widget.TextView[@text='Hajj']"));
            Assert.IsNotNull(result);
        }
    }
}
