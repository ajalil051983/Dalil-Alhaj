using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System.Threading;

namespace ZadAlhaj.UITests.Tests
{
    [TestFixture]
    public class MapTests : AppiumSetup
    {
        private const string Pkg = "com.ilafalkhayr.zadalhaj:id/";

        [Test]
        public void Map_ShouldLoad()
        {
            // 1. Navigate to Map Page
            var mapButton = _driver.FindElement(By.Id(Pkg + "MapButtonID"));
            mapButton.Click();
            Thread.Sleep(3000);

            // 2. Map Control should be present
            var mapControl = _driver.FindElement(By.Id(Pkg + "MapControlID"));
            Assert.IsNotNull(mapControl, "Map control not found.");

            // 3. Go Back
            _driver.Navigate().Back();
        }
    }
}
