using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Threading;

namespace KhayratAlhaj.UITests.Tests
{
    /// <summary>
    /// Tests for prayer-time notification scheduling, the notification toggle in Settings,
    /// and the tap-to-navigate-to-PrayerTimesPage behaviour.
    ///
    /// NotificationService schedules 7 days ahead (DaysAhead = 7) for both an arrival
    /// notification and a 5-minute reminder per prayer per day (~84 total AlarmManager
    /// entries), guarded by a SemaphoreSlim so only one scheduling run can run at a time.
    /// Tests that involve enabling the switch must allow extra time for this to complete.
    /// </summary>
    [TestFixture]
    public class NotificationTests : AppiumSetup
    {
        /// <summary>
        /// Extra delay (ms) to allow ScheduleMultiDayNotificationsAsync to finish.
        /// 7 days × 6 prayers × 2 notifications = 84 AlarmManager calls on a background
        /// thread; emulators can be slow, so we allow up to 6 seconds.
        /// </summary>
        private const int MultiDaySchedulingWaitMs = 6000;
        private const string Pkg = "com.ilafalkhayr.zadalhaj:id/";

        #region Helpers

        /// <summary>
        /// Navigate from MainPage → Settings and wait for it to load.
        /// </summary>
        private void NavigateToSettings()
        {
            var settingsButton = _driver.FindElement(By.Id(Pkg + "SettingsButtonID"));
            settingsButton.Click();
            Thread.Sleep(3000);
        }

        /// <summary>
        /// Scrolls the page until the element with the given AutomationId is visible and
        /// returns it. Uses Android UiScrollable with resourceId (the attribute .NET MAUI
        /// maps AutomationId to on Android) so it works even when MAUI has not yet rendered
        /// off-screen items into the accessibility tree. The scroll attempt is wrapped in a
        /// try-catch so that the method still succeeds when the element is already visible
        /// or when there is no scrollable container on screen.
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

        /// <summary>
        /// Navigate from MainPage → PrayerTimesPage and wait for it to fully load.
        /// </summary>
        private void NavigateToPrayerTimesPage()
        {
            var originalTimeout = _driver.Manage().Timeouts().ImplicitWait;
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
            try
            {
                try
                {
                    var prayerTimesButton = _driver.FindElement(By.Id(Pkg + "PrayerTimesButtonID"));
                    prayerTimesButton.Click();
                }
                catch (NoSuchElementException ex)
                {
                    var pageSource = _driver.PageSource;
                    System.Diagnostics.Debug.WriteLine(
                        $"Failed to find PrayerTimesButtonID. Page source:\n{pageSource}");
                    throw new Exception(
                        $"PrayerTimesButtonID not found. App may not have launched properly. Original error: {ex.Message}");
                }

                // Wait until city name is populated (loading overlay hidden, content rendered).
                // 45 s accounts for GPS timeout on slow emulators.
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(45));
                wait.Until(d =>
                {
                    try
                    {
                        var cityLabel = d.FindElement(By.Id(Pkg + "CityNameLabelID"));
                        return cityLabel != null
                            && !string.IsNullOrEmpty(cityLabel.Text)
                            && cityLabel.Text != "...";
                    }
                    catch { return false; }
                });
            }
            finally
            {
                _driver.Manage().Timeouts().ImplicitWait = originalTimeout;
            }
        }

        #endregion

        // ─── Notification toggle in Settings ───────────────────────────

        [Test]
        public void Settings_PrayerNotificationSwitch_ShouldExist()
        {
            // 1. Navigate to Settings
            NavigateToSettings();

            // 2. Verify the prayer notification switch is present
            var notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            Assert.IsNotNull(notifSwitch, "Prayer notification switch not found in Settings.");
            Assert.IsTrue(notifSwitch.Displayed, "Prayer notification switch should be visible.");

            // 3. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void Settings_PrayerNotificationSwitch_ShouldDefaultToEnabled()
        {
            // 1. Navigate to Settings
            NavigateToSettings();

            // 2. Prayer notifications are enabled by default (Preferences default = true)
            var notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            Assert.IsNotNull(notifSwitch, "Prayer notification switch not found.");

            // On Android the Switch text/checked attribute should be "true"
            var isChecked = notifSwitch.GetAttribute("checked");
            Assert.AreEqual("true", isChecked,
                "Prayer notification switch should be ON by default.");

            // 3. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void Settings_PrayerNotificationSwitch_CanBeToggledOff()
        {
            // 1. Navigate to Settings
            NavigateToSettings();

            // 2. Toggle the switch OFF
            var notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            var wasBefore = notifSwitch.GetAttribute("checked");
            notifSwitch.Click();
            Thread.Sleep(1500);

            // 3. Verify the switch state changed
            notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            var isAfter = notifSwitch.GetAttribute("checked");
            Assert.AreNotEqual(wasBefore, isAfter,
                "Prayer notification switch state should change after clicking.");

            // 4. Toggle back to restore original state
            notifSwitch.Click();
            Thread.Sleep(1000);

            // 5. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void Settings_PrayerNotificationSwitch_CanBeToggledOnAgain()
        {
            // 1. Navigate to Settings
            NavigateToSettings();

            // 2. Turn OFF
            var notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            if (notifSwitch.GetAttribute("checked") == "true")
            {
                notifSwitch.Click();
                Thread.Sleep(1500);
            }

            // 3. Turn back ON
            notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            Assert.AreEqual("false", notifSwitch.GetAttribute("checked"),
                "Switch should be OFF before toggling back ON.");
            notifSwitch.Click();
            // Allow extra time for 7-day multi-day scheduling to complete
            Thread.Sleep(MultiDaySchedulingWaitMs);

            // 4. Verify it is ON again
            notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            Assert.AreEqual("true", notifSwitch.GetAttribute("checked"),
                "Prayer notification switch should be ON after toggling back.");

            // 5. Go back
            _driver.Navigate().Back();
        }

        // ─── PrayerTimesPage reachability (notification target) ────────

        [Test]
        public void PrayerTimesPage_ShouldBeReachableFromMainPage()
        {
            // Ensures PrayerTimesPage — the navigation target for notifications —
            // can load successfully and displays valid data.

            // 1. Navigate to PrayerTimesPage
            NavigateToPrayerTimesPage();

            // 2. Verify key elements are present
            var cityNameLabel = _driver.FindElement(By.Id(Pkg + "CityNameLabelID"));
            Assert.IsNotNull(cityNameLabel, "CityNameLabel should exist on PrayerTimesPage.");
            Assert.IsTrue(cityNameLabel.Displayed, "CityNameLabel should be visible.");

            var nextPrayerName = _driver.FindElement(By.Id(Pkg + "NextPrayerNameLabelID"));
            Assert.IsNotNull(nextPrayerName, "NextPrayerNameLabel should exist.");
            Assert.IsNotEmpty(nextPrayerName.Text, "Next prayer name should have text.");

            var countdownLabel = _driver.FindElement(By.Id(Pkg + "CountdownLabelID"));
            Assert.IsNotNull(countdownLabel, "CountdownLabel should exist.");
            Assert.IsNotEmpty(countdownLabel.Text, "Countdown should have text.");

            // 3. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void PrayerTimesPage_AllPrayerTimesLoaded_AfterNotificationsEnabled()
        {
            // Verifies that when notifications are enabled, all six prayer times load,
            // confirming the same data the notification service would use to schedule.

            // 1. Make sure notifications are enabled in Settings
            NavigateToSettings();
            var notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            if (notifSwitch.GetAttribute("checked") != "true")
            {
                notifSwitch.Click();
                Thread.Sleep(1500);
            }
            _driver.Navigate().Back();

            // 2. Navigate to PrayerTimesPage
            NavigateToPrayerTimesPage();

            // 3. Verify each prayer time label has loaded (not placeholder)
            var prayers = new[]
            {
                "FajrTimeLabelID",
                "ShurooqTimeLabelID",
                "DhuhrTimeLabelID",
                "AsrTimeLabelID",
                "MaghribTimeLabelID",
                "IshaTimeLabelID"
            };

            foreach (var id in prayers)
            {
                // ScrollToElement scrolls via resourceId (correct MAUI AutomationId mapping on
                // Android) so off-screen items are brought into the accessibility tree before
                // the assertion reads their Text property.
                var label = ScrollToElement(id);
                Assert.IsNotNull(label, $"{id} not found on PrayerTimesPage.");
                Assert.AreNotEqual("--:--", label.Text,
                    $"{id} should display a real time, not the placeholder.");
            }

            // 4. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void Settings_DisableNotifications_ThenReEnable_PrayerTimesStillLoad()
        {
            // Disabling then re-enabling notifications should reschedule correctly.
            // We verify indirectly by checking that PrayerTimesPage still loads data.

            // 1. Disable notifications
            NavigateToSettings();
            var notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            if (notifSwitch.GetAttribute("checked") == "true")
            {
                notifSwitch.Click();
                Thread.Sleep(1500);
            }

            // 2. Re-enable notifications
            notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            notifSwitch.Click();
            // NotificationService schedules 7 days ahead (~84 notifications) on a background
            // thread. Allow enough time for all AlarmManager.Set calls to complete.
            Thread.Sleep(MultiDaySchedulingWaitMs);

            _driver.Navigate().Back();

            // 3. Navigate to PrayerTimesPage
            NavigateToPrayerTimesPage();

            // 4. Verify at least Fajr and Isha display a time – use ScrollToElement so rows
            //    that are below the fold are scrolled into the accessibility tree first.
            var fajr = ScrollToElement("FajrTimeLabelID");
            Assert.AreNotEqual("--:--", fajr.Text, "Fajr time should be loaded after re-enabling notifications.");

            var isha = ScrollToElement("IshaTimeLabelID");
            Assert.AreNotEqual("--:--", isha.Text, "Isha time should be loaded after re-enabling notifications.");

            // 5. Go back
            _driver.Navigate().Back();
        }

        // ─── New NotificationService-specific tests ──────────────────

        [Test]
        public void Settings_PrayerNotificationSwitch_IsEnabledPersistsAfterNavigation()
        {
            // NotificationService.IsEnabled is stored in Preferences, so the switch
            // state should survive navigating away from Settings and returning.

            // 1. Navigate to Settings and ensure notifications are ON
            NavigateToSettings();
            var notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            if (notifSwitch.GetAttribute("checked") != "true")
            {
                notifSwitch.Click();
                Thread.Sleep(MultiDaySchedulingWaitMs);
            }
            _driver.Navigate().Back();

            // 2. Navigate away (PrayerTimesPage) and come back
            NavigateToPrayerTimesPage();
            _driver.Navigate().Back();

            // 3. Re-open Settings
            NavigateToSettings();

            // 4. Switch should still be ON — Preferences are persisted
            notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            Assert.AreEqual("true", notifSwitch.GetAttribute("checked"),
                "Prayer notification switch should remain ON after navigating away and returning.");

            // 5. Go back
            _driver.Navigate().Back();
        }

        [Test]
        public void Settings_EnableNotifications_SwitchIsReEnabledAfterMultiDayScheduling()
        {
            // OnPrayerNotificationToggled disables the switch while scheduling runs,
            // then re-enables it in a finally block via MainThread.BeginInvokeOnMainThread.
            // After multi-day scheduling finishes the switch must be interactable again.

            // 1. Navigate to Settings
            NavigateToSettings();

            // 2. Turn OFF then ON to trigger multi-day scheduling
            var notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            if (notifSwitch.GetAttribute("checked") == "true")
            {
                notifSwitch.Click();
                Thread.Sleep(1500);
            }
            notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            notifSwitch.Click(); // triggers ScheduleMultiDayNotificationsAsync for 7 days
            Thread.Sleep(MultiDaySchedulingWaitMs);

            // 3. Switch must be re-enabled (enabled attribute = "true" or not "false")
            notifSwitch = _driver.FindElement(By.Id(Pkg + "PrayerNotificationSwitchID"));
            var isEnabled = notifSwitch.GetAttribute("enabled");
            Assert.AreNotEqual("false", isEnabled,
                "Prayer notification switch should be interactable again after multi-day scheduling completes.");

            // 4. Go back
            _driver.Navigate().Back();
        }

        // ─── Notification tap → PrayerTimesPage (simulated) ───────────

        [Test]
        public void NotificationTap_PrayerTimesPageShouldRenderCorrectly()
        {
            // We cannot programmatically trigger a real notification tap in Appium,
            // but we CAN verify that the page the notification would navigate to
            // renders correctly and is not broken. This is the smoke test for the
            // notification → PrayerTimesPage flow.

            // 1. Navigate directly to PrayerTimesPage (same page the notification opens)
            NavigateToPrayerTimesPage();

            // 2. Verify the countdown section (at the top – normally visible)
            var nextPrayerName = _driver.FindElement(By.Id(Pkg + "NextPrayerNameLabelID"));
            Assert.IsTrue(nextPrayerName.Displayed, "Next prayer name should be visible.");

            var countdownLabel = _driver.FindElement(By.Id(Pkg + "CountdownLabelID"));
            Assert.IsTrue(countdownLabel.Displayed, "Countdown should be visible.");
            Assert.AreNotEqual("--:--", countdownLabel.Text,
                "Countdown should show a real timer, not the placeholder.");

            // 3. Verify all prayer rows – scroll each one into view first because MAUI
            //    does not render off-screen items into the accessibility tree.
            var fajr = ScrollToElement("FajrTimeLabelID");
            Assert.IsTrue(fajr.Displayed, "Fajr row should be visible.");

            var dhuhr = ScrollToElement("DhuhrTimeLabelID");
            Assert.IsTrue(dhuhr.Displayed, "Dhuhr row should be visible.");

            var isha = ScrollToElement("IshaTimeLabelID");
            Assert.IsTrue(isha.Displayed, "Isha row should be visible.");

            // 4. Verify dates – also scroll into view
            var dateLabel = ScrollToElement("DateLabelID");
            Assert.IsNotEmpty(dateLabel.Text, "Gregorian date should be shown.");

            var hijriLabel = ScrollToElement("HijriDateLabelID");
            Assert.IsNotEmpty(hijriLabel.Text, "Hijri date should be shown.");

            // 5. Go back
            _driver.Navigate().Back();
        }
    }
}
