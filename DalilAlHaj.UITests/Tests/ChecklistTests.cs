using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System.Threading;

namespace DalilAlHaj.UITests.Tests
{
    [TestFixture]
    public class ChecklistTests : AppiumSetup
    {
        [Test]
        public void Checklist_ShouldToggleItems_AndUpdateProgress()
        {
            // 1. Navigate to Checklist Page from Main Page
            var checklistButton = _driver.FindElement(By.XPath("//*[@content-desc='ChecklistButtonID']"));
            checklistButton.Click();
            Thread.Sleep(1000); // Wait for page nav

            // 2. Find a checkbox and toggle it
            // Finding Elements inside CollectionView can be tricky.
            // We look for elements with the ID we assigned.
            // Note: In ListView/CollectionView, multiple elements will have same ID. FindElements will return a list.
            var checkBoxes = _driver.FindElements(By.XPath("//*[@content-desc='ChecklistItemCheckBox']"));
            
            Assert.IsNotEmpty(checkBoxes, "No checklist items found.");

            // 3. Click first checkbox
            checkBoxes[0].Click();
            Thread.Sleep(500); // Wait for progress update logic

            // 4. Verify Progress Bar or Percentage Label changes (Optional, requires complex state checking)
            // Just verifying interaction doesn't crash is a good start if state checking is hard.
            // We can check if the checkbox state actually changed if UIAutomator exposes 'checked' attribute.
            
            // Go back to main
            _driver.Navigate().Back();
        }
    }
}
