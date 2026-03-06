using NUnit.Framework;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZadAlhaj.UITests
{
    public class AppiumSetup
    {
        protected AndroidDriver _driver;

        [SetUp]
        public void Setup()
        {
            // Check if Appium server is running
            if (!IsAppiumServerRunning())
            {
                Assert.Fail("Appium server is not running at http://127.0.0.1:4723/. Please start Appium server before running tests.");
            }

            // Placeholder: Typically this is part of your CI or local config.
            // Ensure you have built the app in Release or Debug mode.
            // Adjust the path to where your .apk is generated.
            // Using Release APK to avoid Fast Deployment issues (Debug APK doesn't bundle assemblies)
            var projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../"));
            var appPath = Path.Combine(projectRoot, "ZadAlhaj", "bin", "Release", "net10.0-android", "com.ilafalkhayr.zadalhaj-Signed.apk");
            
            if (!File.Exists(appPath))
            {
                Assert.Fail($"APK file not found at: {appPath}. Please build the Android app before running tests.");
            }

            var driverOptions = new AppiumOptions();
            driverOptions.PlatformName = "Android";
            driverOptions.DeviceName = "emulator-5554";
            driverOptions.AutomationName = "UiAutomator2";
            
            // Use the pre-installed Release app instead of letting Appium install it
            // (Debug APK uses Fast Deployment and crashes; Release APK must be installed manually first)
            driverOptions.AddAdditionalAppiumOption("appPackage", "com.ilafalkhayr.zadalhaj");
            driverOptions.AddAdditionalAppiumOption("appActivity", "crc640e514d85339b6ec1.MainActivity");
            driverOptions.AddAdditionalAppiumOption("appWaitActivity", "crc640e514d85339b6ec1.MainActivity");
            driverOptions.AddAdditionalAppiumOption("appWaitDuration", 60000);
            driverOptions.AddAdditionalAppiumOption("autoGrantPermissions", true);
            driverOptions.AddAdditionalAppiumOption("noReset", true); // Don't reinstall, use existing app
            driverOptions.AddAdditionalAppiumOption("forceAppLaunch", true); // Force restart app each test to start from MainPage

            // Appium 2.x defaults to / root path, removing /wd/hub
            // Increased timeout to 3 minutes to allow for emulator start and app installation
            _driver = new AndroidDriver(new Uri("http://127.0.0.1:4723/"), driverOptions, TimeSpan.FromMinutes(3));
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }

        private bool IsAppiumServerRunning()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var response = client.GetAsync("http://127.0.0.1:4723/status").Result;
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        [TearDown]
        public void TearDown()
        {
            _driver?.Quit();
        }
    }
}