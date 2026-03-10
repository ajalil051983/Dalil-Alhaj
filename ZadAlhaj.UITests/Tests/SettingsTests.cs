using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System.Threading;

namespace ZadAlhaj.UITests.Tests
{
    [TestFixture]
    public class SettingsTests : AppiumSetup
    {
        private const string Pkg = "com.ilafalkhayr.zadalhaj:id/";

        [Test]
        public void Settings_ShouldAllowChangingLanguageAndTheme()
        {
            // 1. Navigate to Settings
            var settingsButton = _driver.FindElement(By.Id(Pkg + "SettingsButtonID"));
            settingsButton.Click();
            Thread.Sleep(3000);

            // 2. Verify Language Picker exists
            var languagePicker = _driver.FindElement(By.Id(Pkg + "LanguagePickerID"));
            Assert.IsNotNull(languagePicker, "Language picker not found.");

            // 3. Toggle Dark Mode (re-find element right before clicking to avoid stale ref)
            _driver.FindElement(By.Id(Pkg + "DarkModeSwitchID")).Click();
            Thread.Sleep(1500); // Wait for theme re-render

            // 4. Change Font Size - re-find after possible re-render
            _driver.FindElement(By.Id(Pkg + "FontSizeMediumID")).Click();
            Thread.Sleep(500);

            // Go back
            _driver.Navigate().Back();
        }
    }
}
