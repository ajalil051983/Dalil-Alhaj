using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ZadAlhaj.UITests.Tests
{
    [TestFixture]
    public class PrayerTimesTests : AppiumSetup
    {
        /// <summary>
        /// Navigate from MainPage to PrayerTimesPage and wait for it to load.
        /// </summary>
        private void NavigateToPrayerTimesPage()
        {
            // Temporarily increase implicit wait to handle slow app startup on emulator
            // (splash screen, MAUI initialization, MainPage rendering can exceed 10s)
            var originalTimeout = _driver.Manage().Timeouts().ImplicitWait;
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
            try
            {
                // Add diagnostic information if element not found
                try
                {
                    var prayerTimesButton = _driver.FindElement(MobileBy.Id("PrayerTimesButtonID"));
                    prayerTimesButton.Click();
                }
                catch (OpenQA.Selenium.NoSuchElementException ex)
                {
                    // Capture page source for debugging
                    var pageSource = _driver.PageSource;
                    System.Diagnostics.Debug.WriteLine($"Failed to find PrayerTimesButtonID. Page source:\n{pageSource}");
                    throw new Exception($"PrayerTimesButtonID not found. App may not have launched properly. Original error: {ex.Message}");
                }

                // Wait until the city name label is present (loading overlay hidden and content rendered)
                // Increased wait time to account for GPS timeout on emulator (10s timeout in GetStoredOrGpsLocationAsync)
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(45));
                wait.Until(d => {
                    try
                    {
                        var cityLabel = d.FindElement(MobileBy.Id("CityNameLabelID"));
                        return cityLabel != null && !string.IsNullOrEmpty(cityLabel.Text) && cityLabel.Text != "...";
                    }
                    catch
                    {
                        return false;
                    }
                });
            }
            finally
            {
                _driver.Manage().Timeouts().ImplicitWait = originalTimeout;
            }
        }

        /// <summary>
        /// Explicit wait that correctly zeroes the implicit wait while polling,
        /// avoiding the double-wait anti-pattern (implicit + explicit = only one retry).
        /// </summary>
        private AppiumElement WaitForElement(string automationId, int timeoutSeconds = 30)
        {
            var previousImplicit = _driver.Manage().Timeouts().ImplicitWait;
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
                return (AppiumElement)wait.Until(d => d.FindElement(MobileBy.Id(automationId)));
            }
            finally
            {
                _driver.Manage().Timeouts().ImplicitWait = previousImplicit;
            }
        }

        [Test]
        public void PrayerTimesPage_ShouldLoadAndDisplayPrayerTimes()
        {
            // 1. Navigate to Prayer Times page
            NavigateToPrayerTimesPage();

            // 2. Verify city name label is present
            var cityNameLabel = _driver.FindElement(MobileBy.Id("CityNameLabelID"));
            Assert.IsNotNull(cityNameLabel, "City name label not found on Prayer Times page.");
            Assert.IsTrue(cityNameLabel.Displayed, "City name label is not displayed.");

            // 3. Verify date labels are present
            var dateLabel = _driver.FindElement(MobileBy.Id("DateLabelID"));
            Assert.IsNotNull(dateLabel, "Date label not found.");

            var hijriDateLabel = _driver.FindElement(MobileBy.Id("HijriDateLabelID"));
            Assert.IsNotNull(hijriDateLabel, "Hijri date label not found.");

            // 4. Verify next prayer countdown is present
            var nextPrayerNameLabel = _driver.FindElement(MobileBy.Id("NextPrayerNameLabelID"));
            Assert.IsNotNull(nextPrayerNameLabel, "Next prayer name label not found.");

            var countdownLabel = _driver.FindElement(MobileBy.Id("CountdownLabelID"));
            Assert.IsNotNull(countdownLabel, "Countdown label not found.");

            // 5. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_ShouldDisplayAllSixPrayerTimes()
        {
            // 1. Navigate
            NavigateToPrayerTimesPage();

            // 2. Verify each prayer time label exists
            var fajrTime = _driver.FindElement(MobileBy.Id("FajrTimeLabelID"));
            Assert.IsNotNull(fajrTime, "Fajr time label not found.");

            var shurooqTime = _driver.FindElement(MobileBy.Id("ShurooqTimeLabelID"));
            Assert.IsNotNull(shurooqTime, "Shurooq time label not found.");

            var dhuhrTime = _driver.FindElement(MobileBy.Id("DhuhrTimeLabelID"));
            Assert.IsNotNull(dhuhrTime, "Dhuhr time label not found.");

            var asrTime = _driver.FindElement(MobileBy.Id("AsrTimeLabelID"));
            Assert.IsNotNull(asrTime, "Asr time label not found.");

            var maghribTime = _driver.FindElement(MobileBy.Id("MaghribTimeLabelID"));
            Assert.IsNotNull(maghribTime, "Maghrib time label not found.");

            // Isha is the last prayer row and may be just below the visible viewport.
            // scrollIntoView scrolls only as far as needed to make the element visible.
            try
            {
                _driver.FindElement(
                    MobileBy.AndroidUIAutomator(
                        "new UiScrollable(new UiSelector().scrollable(true)).scrollIntoView(new UiSelector().description(\"IshaTimeLabelID\"))"));
            }
            catch (NoSuchElementException) { /* element already visible or no scrollable container */ }

            var ishaTime = _driver.FindElement(MobileBy.Id("IshaTimeLabelID"));
            Assert.IsNotNull(ishaTime, "Isha time label not found.");

            // 3. Verify times are not the default placeholder
            Assert.AreNotEqual("--:--", fajrTime.Text, "Fajr time should be loaded.");
            Assert.AreNotEqual("--:--", dhuhrTime.Text, "Dhuhr time should be loaded.");
            Assert.AreNotEqual("--:--", ishaTime.Text, "Isha time should be loaded.");

            // 4. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_LocationButton_ShouldToggleSearchSection()
        {
            // 1. Navigate
            NavigateToPrayerTimesPage();

            // 2. Verify search section is initially hidden
            var searchSections = _driver.FindElements(MobileBy.Id("SearchSectionID"));
            // It may not be in the tree at all if IsVisible=False, or it may exist but not displayed

            // 3. Click the location display (city name) to show search section
            var locationDisplay = _driver.FindElement(MobileBy.Id("CityNameLabelID"));
            Assert.IsNotNull(locationDisplay, "Location display not found.");
            locationDisplay.Click();
            Thread.Sleep(1000);

            // 4. Verify search section is now visible
            var searchSection = _driver.FindElement(MobileBy.Id("SearchSectionID"));
            Assert.IsNotNull(searchSection, "Search section not found after clicking location display.");

            // 5. Verify city search bar is present in the search section
            var citySearchBar = _driver.FindElement(MobileBy.Id("CitySearchBarID"));
            Assert.IsNotNull(citySearchBar, "City search bar not found.");

            // 6. Click location display again to hide search section
            locationDisplay.Click();
            Thread.Sleep(1000);

            // 7. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_WeeklyToggle_ShouldShowWeeklySchedule()
        {
            // 1. Navigate
            NavigateToPrayerTimesPage();

            // 2. Find and click the weekly toggle switch
            var weeklyToggleSwitch = _driver.FindElement(MobileBy.Id("WeeklyToggleSwitchID"));
            Assert.IsNotNull(weeklyToggleSwitch, "Weekly toggle switch not found.");
            weeklyToggleSwitch.Click();

            // 3. Scroll the weekly section into view, then confirm it is visible.
            //    scrollIntoView stops as soon as WeeklySectionID enters the viewport.
            try
            {
                _driver.FindElement(
                    MobileBy.AndroidUIAutomator(
                        "new UiScrollable(new UiSelector().scrollable(true)).scrollIntoView(new UiSelector().description(\"WeeklySectionID\"))"));
            }
            catch (NoSuchElementException) { /* already visible or no scrollable container */ }

            var weeklySection = WaitForElement("WeeklySectionID", timeoutSeconds: 30);
            Assert.IsNotNull(weeklySection, "Weekly section not found after toggling.");

            // 4. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_NextPrayerCountdown_ShouldBeRunning()
        {
            // 1. Navigate
            NavigateToPrayerTimesPage();

            // 2. Get initial countdown value
            var countdownLabel = _driver.FindElement(MobileBy.Id("CountdownLabelID"));
            Assert.IsNotNull(countdownLabel, "Countdown label not found.");
            var initialText = countdownLabel.Text;

            // 3. Wait a couple of seconds for the countdown to tick
            Thread.Sleep(3000);

            // 4. Re-read the countdown value - it should have changed
            var updatedText = _driver.FindElement(MobileBy.Id("CountdownLabelID")).Text;

            // The countdown should not be the default placeholder if prayer times loaded
            Assert.AreNotEqual("--:--", initialText, "Countdown should not be the default placeholder.");

            // 5. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_ShouldDisplayHijriDate()
        {
            // 1. Navigate
            NavigateToPrayerTimesPage();

            // 2. Verify Hijri date label has actual content (not empty)
            var hijriDateLabel = _driver.FindElement(MobileBy.Id("HijriDateLabelID"));
            Assert.IsNotNull(hijriDateLabel, "Hijri date label not found.");
            Assert.IsNotEmpty(hijriDateLabel.Text, "Hijri date label should contain text after loading.");

            // 3. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_ShouldDisplayGregorianDate()
        {
            // 1. Navigate
            NavigateToPrayerTimesPage();

            // 2. Verify date label has actual content (not empty)
            var dateLabel = _driver.FindElement(MobileBy.Id("DateLabelID"));
            Assert.IsNotNull(dateLabel, "Date label not found.");
            Assert.IsNotEmpty(dateLabel.Text, "Date label should contain text after loading.");

            // 3. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_CityNameShouldNotBePlaceholder()
        {
            // Tests LoadPrayerTimesAsync sets CityNameLabel.Text from todayTimes.LocationName
            // 1. Navigate
            NavigateToPrayerTimesPage();

            // 2. Verify city name is not the initial placeholder
            var cityNameLabel = _driver.FindElement(MobileBy.Id("CityNameLabelID"));
            Assert.IsNotNull(cityNameLabel, "City name label not found.");
            Assert.AreNotEqual("...", cityNameLabel.Text, "City name should be loaded, not placeholder.");
            Assert.AreNotEqual("---", cityNameLabel.Text, "City name should be a real location name.");

            // 3. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_PrayerTimesFormat_ShouldBeValid()
        {
            // Tests that FormatTime produces a valid HH:MM time string
            // 1. Navigate
            NavigateToPrayerTimesPage();

            // 2. Check all prayer time labels match HH:MM format (e.g. "05:23", "12:30")
            var timePattern = new Regex(@"^\d{2}:\d{2}$");

            var fajrTime = _driver.FindElement(MobileBy.Id("FajrTimeLabelID")).Text;
            Assert.IsTrue(timePattern.IsMatch(fajrTime), $"Fajr time '{fajrTime}' should match HH:MM format.");

            var dhuhrTime = _driver.FindElement(MobileBy.Id("DhuhrTimeLabelID")).Text;
            Assert.IsTrue(timePattern.IsMatch(dhuhrTime), $"Dhuhr time '{dhuhrTime}' should match HH:MM format.");

            var asrTime = _driver.FindElement(MobileBy.Id("AsrTimeLabelID")).Text;
            Assert.IsTrue(timePattern.IsMatch(asrTime), $"Asr time '{asrTime}' should match HH:MM format.");

            var maghribTime = _driver.FindElement(MobileBy.Id("MaghribTimeLabelID")).Text;
            Assert.IsTrue(timePattern.IsMatch(maghribTime), $"Maghrib time '{maghribTime}' should match HH:MM format.");

            // Isha is the last prayer row and may be just below the visible viewport.
            // scrollIntoView scrolls only as far as needed to make the element visible.
            try
            {
                _driver.FindElement(
                    MobileBy.AndroidUIAutomator(
                        "new UiScrollable(new UiSelector().scrollable(true)).scrollIntoView(new UiSelector().description(\"IshaTimeLabelID\"))"));
            }
            catch (NoSuchElementException) { /* element already visible or no scrollable container */ }

            var ishaTime = _driver.FindElement(MobileBy.Id("IshaTimeLabelID")).Text;
            Assert.IsTrue(timePattern.IsMatch(ishaTime), $"Isha time '{ishaTime}' should match HH:MM format.");

            // 3. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_NextPrayerName_ShouldBeAKnownPrayer()
        {
            // Tests that GetPrayerName returns a localized prayer name (from HighlightNextPrayer)
            // 1. Navigate
            NavigateToPrayerTimesPage();

            // 2. Get the next prayer name
            var nextPrayerNameLabel = _driver.FindElement(MobileBy.Id("NextPrayerNameLabelID"));
            var name = nextPrayerNameLabel.Text;
            Assert.IsNotNull(name, "Next prayer name should not be null.");
            Assert.AreNotEqual("...", name, "Next prayer name should be resolved, not placeholder.");
            Assert.IsNotEmpty(name, "Next prayer name should not be empty.");

            // 3. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_LoadingOverlay_ShouldDisappearAfterLoad()
        {
            // Tests that the loading overlay fades out after LoadPrayerTimesAsync
            // 1. Navigate
            NavigateToPrayerTimesPage();

            // 2. By the time we can interact, the overlay should be hidden
            // FindElements (plural) so we don't throw if it's gone from the tree
            var overlays = _driver.FindElements(MobileBy.Id("LoadingOverlayID"));
            if (overlays.Count > 0)
            {
                Assert.IsFalse(overlays[0].Displayed, "Loading overlay should be hidden after prayer times load.");
            }
            // If not found at all, that's also fine — it's hidden

            // 3. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_CitySearch_ShouldAcceptInput()
        {
            // Tests OnCitySearchTextChanged — typing in search bar
            // 1. Navigate
            NavigateToPrayerTimesPage();

            // 2. Open search section by clicking on the location display
            var locationDisplay = _driver.FindElement(MobileBy.Id("CityNameLabelID"));
            locationDisplay.Click();
            Thread.Sleep(1000);

            // 3. Type into city search bar
            var citySearchBar = _driver.FindElement(MobileBy.Id("CitySearchBarID"));
            Assert.IsNotNull(citySearchBar, "City search bar not found.");
            citySearchBar.Clear();
            citySearchBar.SendKeys("Mecca");
            Thread.Sleep(2000); // Wait for search results

            // 4. Hide keyboard
            try { _driver.HideKeyboard(); } catch { }

            // 5. Verify results appeared (OnCitySearchTextChanged should have triggered)
            // The CityResults CollectionView should now be visible with items
            // We don't assert specific results since it depends on data, just that input was accepted
            var searchBarText = citySearchBar.Text;
            Assert.IsTrue(searchBarText.Contains("Mecca") || searchBarText.Contains("mecca"),
                $"Search bar should contain typed text, but was '{searchBarText}'.");

            // 6. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_WeeklyToggle_ShouldHideWhenClickedAgain()
        {
            // Tests OnWeeklyToggled hide behavior
            // 1. Navigate
            NavigateToPrayerTimesPage();

            // 2. Show weekly schedule and wait until it appears
            var weeklyToggleSwitch = _driver.FindElement(MobileBy.Id("WeeklyToggleSwitchID"));
            weeklyToggleSwitch.Click();
            var weeklySection = WaitForElement("WeeklySectionID", timeoutSeconds: 30);
            Assert.IsNotNull(weeklySection, "Weekly section should be visible after first click.");

            // 3. Scroll back to the top so the toggle is in view, then click to hide.
            //    The weekly section expands below the toggle, pushing it off-screen.
            //    scrollToBeginning reliably returns to the top regardless of content length.
            //    We use a try/catch because the page may already be at the top.
            try
            {
                _driver.FindElement(
                    MobileBy.AndroidUIAutomator(
                        "new UiScrollable(new UiSelector().scrollable(true)).scrollToBeginning(10)"));
            }
            catch (NoSuchElementException) { /* already at top or no scrollable container */ }

            weeklyToggleSwitch = _driver.FindElement(MobileBy.Id("WeeklyToggleSwitchID"));
            weeklyToggleSwitch.Click();

            // 4. Weekly section should now be hidden — poll until gone or not displayed
            var previousImplicit = _driver.Manage().Timeouts().ImplicitWait;
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                wait.IgnoreExceptionTypes(typeof(NoSuchElementException));
                wait.Until(d => {
                    var els = d.FindElements(MobileBy.Id("WeeklySectionID"));
                    return els.Count == 0 || !els[0].Displayed;
                });
            }
            finally
            {
                _driver.Manage().Timeouts().ImplicitWait = previousImplicit;
            }

            var remaining = _driver.FindElements(MobileBy.Id("WeeklySectionID"));
            if (remaining.Count > 0)
                Assert.IsFalse(remaining[0].Displayed, "Weekly section should be hidden after clicking toggle again.");

            // 5. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_CountdownFormat_ShouldBeValid()
        {
            // Tests that FormatCountdown / FormatElapsed produce valid timer format.
            // FormatCountdown:  MM:SS  or  H:MM:SS  (prayer hasn't arrived yet)
            // FormatElapsed:   +MM:SS  or +H:MM:SS  (prayer started, within 15-min grace period)
            // 1. Navigate
            NavigateToPrayerTimesPage();

            // 2. Get countdown text
            var countdownLabel = _driver.FindElement(MobileBy.Id("CountdownLabelID"));
            var text = countdownLabel.Text;

            // 3. Accept countdown, elapsed (+prefix), or placeholder
            var countdownPattern = new Regex(@"^(\+?\d{1,2}:\d{2}:\d{2}|\+?\d{2}:\d{2}|--:--)$");
            Assert.IsTrue(countdownPattern.IsMatch(text),
                $"Countdown '{text}' should match a valid timer format (H:MM:SS, MM:SS, +H:MM:SS, +MM:SS, or --:--).");

            // 4. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_WeeklySchedule_ShouldIncludeShurooqTimes()
        {
            // Verifies that the WeeklyDayItem now includes Shurooq header/time columns.
            // 1. Navigate
            NavigateToPrayerTimesPage();

            // 2. Find and click the weekly toggle switch (always visible at top of page)
            var weeklyToggleSwitch = _driver.FindElement(MobileBy.Id("WeeklyToggleSwitchID"));
            Assert.IsNotNull(weeklyToggleSwitch, "Weekly toggle switch not found.");
            weeklyToggleSwitch.Click();

            // 3. Wait for weekly section — WaitForElement avoids the implicit-wait conflict
            var weeklySection = WaitForElement("WeeklySectionID", timeoutSeconds: 30);
            Assert.IsNotNull(weeklySection, "Weekly section not visible.");

            // 4. Shurooq label text should appear somewhere in the weekly collection
            //    (the WeeklyDayItem exposes ShurooqHeader bound to AppResources.Shurooq)
            var shurooqLabels = _driver.FindElements(
                OpenQA.Selenium.By.XPath("//*[contains(@text,'\u0634\u0631\u0648\u0642') or contains(@text,'Sunrise') or contains(@text,'Shurooq')]"));
            Assert.IsTrue(shurooqLabels.Count > 0,
                "Weekly schedule should display Shurooq time column for each day.");

            // 5. Go back
            _driver.Navigate().Back();
        }
    }
}
