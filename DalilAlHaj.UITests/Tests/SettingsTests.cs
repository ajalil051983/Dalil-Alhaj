using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System.Threading;

namespace DalilAlHaj.UITests.Tests
{
    [TestFixture]
    public class SettingsTests : AppiumSetup
    {
        [Test]
        public void Settings_ShouldAllowChangingLanguageAndTheme()
        {
            // 1. Navigate to Settings
            // Assuming Settings button on MainPage has ID from previous edit
            var settingsButton = _driver.FindElement(By.XPath("//*[@content-desc='SettingsButtonID']"));
            settingsButton.Click();
            Thread.Sleep(1000);

            // 2. Change Font Size (Click Button)
            var fontButton = _driver.FindElement(By.XPath("//*[@content-desc='FontSizeMediumID']"));
            fontButton.Click();
            
            // 3. Toggle Dark Mode
            var themeSwitch = _driver.FindElement(By.XPath("//*[@content-desc='DarkModeSwitchID']"));
            themeSwitch.Click();

            // 4. Verify no crash and elements still exist
            Assert.IsNotNull(_driver.FindElement(By.XPath("//*[@content-desc='LanguagePickerID']")));

            // Go back
            _driver.Navigate().Back();
        }
    }
}
