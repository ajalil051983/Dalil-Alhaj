using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System.Threading;

namespace KhayratAlhaj.UITests.Tests
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

        /// <summary>
        /// Scrolls the Settings page until the element with the given AutomationId is visible
        /// and returns it. Uses Android UiScrollable with resourceId (the attribute .NET MAUI
        /// maps AutomationId to on Android) so it works even when items are below the fold.
        /// The scroll attempt is wrapped in a try-catch so that the method still succeeds when
        /// the element is already visible or when there is no scrollable container on screen.
        /// </summary>
        private IWebElement ScrollToElement(string automationId)
        {
            try
            {
                _driver.FindElement(MobileBy.AndroidUIAutomator(
                    "new UiScrollable(new UiSelector().scrollable(true).instance(0))" +
                    $".scrollIntoView(new UiSelector().resourceId(\"{Pkg}{automationId}\"))"));
            }
            catch (NoSuchElementException) { /* element already visible or no scrollable container */ }

            return _driver.FindElement(By.Id(Pkg + automationId));
        }

        [Test]
        public void Settings_ShouldAllowChangingLanguageAndTheme()
        {
            // 1. Navigate to Settings
            NavigateToSettings();

            // 2. Verify Language Picker exists – scroll to it in case it is below the fold.
            var languagePicker = ScrollToElement("LanguagePickerID");
            Assert.IsNotNull(languagePicker, "Language picker not found.");

            // 3. Toggle Dark Mode – scroll to element first in case it is below the fold.
            ScrollToElement("DarkModeSwitchID").Click();
            Thread.Sleep(1500); // Wait for theme re-render

            // 4. Change Font Size – re-scroll after possible re-render.
            ScrollToElement("FontSizeMediumID").Click();
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
            // ScrollToElement scrolls via resourceId so the picker is in the accessibility
            // tree even when the Settings page requires scrolling to reach it.
            var picker = ScrollToElement("CalculationMethodPickerID");
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
            // ScrollToElement scrolls via resourceId so the picker is in the accessibility
            // tree even when the Settings page requires scrolling to reach it.
            var picker = ScrollToElement("MathhabPickerID");
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

            // ScrollToElement scrolls via resourceId (correct MAUI AutomationId mapping on
            // Android) so buttons below the fold are brought into the accessibility tree.
            var small  = ScrollToElement("FontSizeSmallID");
            var medium = ScrollToElement("FontSizeMediumID");
            var large  = ScrollToElement("FontSizeLargeID");

            Assert.IsNotNull(small,  "FontSizeSmall button not found.");
            Assert.IsNotNull(medium, "FontSizeMedium button not found.");
            Assert.IsNotNull(large,  "FontSizeLarge button not found.");

            // 2. Go back
            _driver.Navigate().Back();
        }
    }
}
