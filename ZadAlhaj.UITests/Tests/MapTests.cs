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

        private void NavigateToMap()
        {
            var mapButton = _driver.FindElement(By.Id(Pkg + "MapButtonID"));
            mapButton.Click();
            Thread.Sleep(5000); // Increased from 3000ms to 5000ms to allow map initialization
        }

        [Test]
        public void Map_ShouldLoad()
        {
            // 1. Navigate to Map Page
            NavigateToMap();

            // 2. Map Control should be present
            var mapControl = _driver.FindElement(By.Id(Pkg + "MapControlID"));
            Assert.IsNotNull(mapControl, "Map control not found.");

            // 3. Go Back
            _driver.Navigate().Back();
        }

        [Test]
        public void Map_MyLocationButton_ShouldExist()
        {
            // MyLocationButton was added to MapPage to center the map on the user's location.
            // 1. Navigate
            NavigateToMap();

            // 2. Button should be present
            var myLocationButton = _driver.FindElement(By.Id(Pkg + "MyLocationButtonID"));
            Assert.IsNotNull(myLocationButton, "My Location button not found on Map page.");
            Assert.IsTrue(myLocationButton.Displayed, "My Location button should be visible.");

            // 3. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void Map_MapControlAndMyLocationButton_BothVisible()
        {
            // Smoke-test that both the map and the location button load together.
            // 1. Navigate
            NavigateToMap();

            // 2. Verify map control
            var mapControl = _driver.FindElement(By.Id(Pkg + "MapControlID"));
            Assert.IsTrue(mapControl.Displayed, "Map control should be displayed.");

            // 3. Verify my-location button
            var myLocationButton = _driver.FindElement(By.Id(Pkg + "MyLocationButtonID"));
            Assert.IsTrue(myLocationButton.Displayed, "My Location button should be displayed.");

            // 4. Go back
            _driver.Navigate().Back();
        }
    }
}
