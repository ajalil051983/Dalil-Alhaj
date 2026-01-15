using NUnit.Framework;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using System;
using System.IO;

namespace DalilAlHaj.UITests
{
    public class AppiumSetup
    {
        protected AndroidDriver _driver;

        [SetUp]
        public void Setup()
        {
            // Placeholder: Typically this is part of your CI or local config.
            // Ensure you have built the app in Release or Debug mode.
            // Adjust the path to where your .apk is generated.
            // Using absolute path for reliability during testing
            var projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../"));
            var appPath = Path.Combine(projectRoot, "DalilAlHaj/bin/Debug/net10.0-android/com.companyname.dalilalhaj-Signed.apk");
            
            var driverOptions = new AppiumOptions();
            driverOptions.PlatformName = "Android";
            driverOptions.DeviceName = "emulator-5554";
            driverOptions.App = appPath;
            driverOptions.AutomationName = "UiAutomator2";
            driverOptions.AddAdditionalAppiumOption("appWaitActivity", "*"); // Wait for any activity to start
            driverOptions.AddAdditionalAppiumOption("appWaitDuration", 30000); // Wait longer for app launch

            // Appium 2.x defaults to / root path, removing /wd/hub
            _driver = new AndroidDriver(new Uri("http://127.0.0.1:4723/"), driverOptions);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }

        [TearDown]
        public void TearDown()
        {
            _driver?.Quit();
        }
    }
}