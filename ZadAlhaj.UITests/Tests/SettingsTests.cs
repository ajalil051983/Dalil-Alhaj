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

        private void NavigateToSettings()
        {
            var settingsButton = _driver.FindElement(By.Id(Pkg + "SettingsButtonID"));
            settingsButton.Click();
            Thread.Sleep(3000);
        }

        [Test]
        public void Settings_ShouldAllowChangingLanguageAndTheme()
        {
            // 1. Navigate to Settings
            NavigateToSettings();

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

        [Test]
        public void Settings_CalculationMethodPicker_ShouldExistAndBeInteractable()
        {
            // CalculationMethodPicker was added to SettingsPage for prayer time method selection.
            // 1. Navigate to Settings
            NavigateToSettings();

            // 2. Verify picker exists
            var picker = _driver.FindElement(By.Id(Pkg + "CalculationMethodPickerID"));
            Assert.IsNotNull(picker, "Calculation method picker not found in Settings.");
            Assert.IsTrue(picker.Displayed, "Calculation method picker should be visible.");

            // 3. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void Settings_MathhabPicker_ShouldExistAndBeInteractable()
        {
            // MathhabPicker (Shafi'i / Hanafi) was added to SettingsPage.
            // 1. Navigate to Settings
            NavigateToSettings();

            // 2. Verify picker exists
            var picker = _driver.FindElement(By.Id(Pkg + "MathhabPickerID"));
            Assert.IsNotNull(picker, "Mathhab picker not found in Settings.");
            Assert.IsTrue(picker.Displayed, "Mathhab picker should be visible.");

            // 3. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void Settings_FontSizeButtons_AllThreeShouldExist()
        {
            // Verify all three font-size buttons (Small / Medium / Large) are present.
            // 1. Navigate to Settings
            NavigateToSettings();

            var small  = _driver.FindElement(By.Id(Pkg + "FontSizeSmallID"));
            var medium = _driver.FindElement(By.Id(Pkg + "FontSizeMediumID"));
            var large  = _driver.FindElement(By.Id(Pkg + "FontSizeLargeID"));

            Assert.IsNotNull(small,  "FontSizeSmall button not found.");
            Assert.IsNotNull(medium, "FontSizeMedium button not found.");
            Assert.IsNotNull(large,  "FontSizeLarge button not found.");

            // 2. Go back
            _driver.Navigate().Back();
        }
    }
}
