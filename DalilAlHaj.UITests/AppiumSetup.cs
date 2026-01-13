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
            var appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../DalilAlHaj/bin/Debug/net10.0-android/com.companyname.dalilalhaj.apk");
            
            var driverOptions = new AppiumOptions();
            driverOptions.AddAdditionalAppiumOption(MobileCapabilityType.PlatformName, "Android");
            driverOptions.AddAdditionalAppiumOption(MobileCapabilityType.DeviceName, "emulator-5554");
            driverOptions.AddAdditionalAppiumOption(MobileCapabilityType.App, appPath);
            driverOptions.AddAdditionalAppiumOption(MobileCapabilityType.AutomationName, "UiAutomator2");

            _driver = new AndroidDriver(new Uri("http://localhost:4723/wd/hub"), driverOptions);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }

        [TearDown]
        public void TearDown()
        {
            _driver?.Quit();
        }
    }
}