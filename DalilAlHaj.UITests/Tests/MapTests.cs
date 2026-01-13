using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System.Threading;

namespace DalilAlHaj.UITests.Tests
{
    [TestFixture]
    public class MapTests : AppiumSetup
    {
        [Test]
        public void Map_ShouldLoad()
        {
            // 1. Navigate to Map Page
            var mapButton = _driver.FindElement(By.XPath("//*[@content-desc='MapButtonID']"));
            mapButton.Click();
            Thread.Sleep(1000);

            // 2. Map Control should be present
            var mapControl = _driver.FindElement(By.XPath("//*[@content-desc='MapControlID']"));
            Assert.IsNotNull(mapControl, "Map control not found.");

            // 3. Go Back
            _driver.Navigate().Back();
        }
    }
}
