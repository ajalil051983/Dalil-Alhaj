using NUnit.Framework;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium;
using System.Threading;

namespace KhayratAlhaj.UITests.Tests
{
    [TestFixture]
    public class SearchTests : AppiumSetup
    {
        private const string Pkg = "com.ilafalkhayr.zadalhaj:id/";

        [Test]
        public void SearchBar_ShouldExistAndAcceptText()
        {
            // 1. Verify SearchBar exists on MainPage
            var searchBar = _driver.FindElement(By.Id(Pkg + "SearchBarID"));
            Assert.IsNotNull(searchBar, "SearchBar not found on Main Page.");
            Assert.IsTrue(searchBar.Displayed, "SearchBar is not displayed.");

            // 2. Type into SearchBar (UiAutomator2 handles focus automatically)
            searchBar.Clear();
            searchBar.SendKeys("الحج");
            Thread.Sleep(500);

            // 3. Verify the text was entered
            var text = searchBar.Text;
            Assert.IsTrue(text.Contains("الحج"), $"SearchBar text expected to contain 'الحج' but was '{text}'.");

            // 4. Hide keyboard
            try { _driver.HideKeyboard(); } catch { }

            // 5. Verify SearchBarFrame container also exists
            var searchFrame = _driver.FindElement(By.Id(Pkg + "SearchBarFrameID"));
            Assert.IsNotNull(searchFrame, "SearchBar frame not found.");
        }
    }
}
