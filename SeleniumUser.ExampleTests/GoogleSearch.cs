using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.IE;
using SeleniumUser.ExampleTests.Pages;

namespace SeleniumUser.ExampleTests
{
    [TestClass]
    public class GoogleSearch
    {
        private IWebDriver Driver { get; set; }

        [TestInitialize]
        public void Setup()
        {
            Driver = new InternetExplorerDriver();
            new InternetExplorerOptions { IntroduceInstabilityByIgnoringProtectedModeSettings = true };
            Driver.Navigate().GoToUrl("http://google.com");
            User.Driver = Driver;
        }

        [TestCleanup]
        public void TearDown()
        {
            Driver.Quit();
        }

        [TestMethod]
        public void Search_Returns_Results()
        {
            var page = new SearchPage()
                .SetSearchField("apple")
                .Search()
                .ShouldSeeResult("Apple");
        }
    }
}
