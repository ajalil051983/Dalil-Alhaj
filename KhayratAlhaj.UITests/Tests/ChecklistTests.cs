using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System.Threading;

namespace KhayratAlhaj.UITests.Tests
{
    [TestFixture]
    public class ChecklistTests : AppiumSetup
    {
        private const string Pkg = "com.ilafalkhayr.zadalhaj:id/";

        [Test]
        public void Checklist_ShouldToggleItems_AndUpdateProgress()
        {
            // 1. Navigate to Checklist Page from Main Page
            var checklistButton = _driver.FindElement(By.Id(Pkg + "ChecklistButtonID"));
            checklistButton.Click();
            Thread.Sleep(3000); // Wait for page navigation

            // 2. Find checkbox items in the checklist
            var checkBoxes = _driver.FindElements(By.Id(Pkg + "ChecklistItemCheckBox"));

            Assert.IsNotEmpty(checkBoxes, "No checklist items found.");

            // 3. Click first checkbox
            checkBoxes[0].Click();
            Thread.Sleep(500);

            // 4. Verify the progress bar exists
            var progressBar = _driver.FindElement(By.Id(Pkg + "ChecklistProgressBar"));
            Assert.IsNotNull(progressBar, "Progress bar not found on checklist page.");

            // Go back to main
            _driver.Navigate().Back();
        }
    }
}
