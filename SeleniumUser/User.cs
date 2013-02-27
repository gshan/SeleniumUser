using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

namespace SeleniumUser
{
    public static class User
    {
        static User()
        { 
            ScreenshotDirectory = @"c:\temp\screenshots\";
        }

        public static IWebDriver Driver { get; set; }

        public static string ScreenshotDirectory { get; set; }

        private static int MAX_RETRIES = 15;

        private static int RETRY_DELAY = 500;

        public static void Wait(string message, Func<bool> condition)
        {
            for (var i = 0; i < MAX_RETRIES; i++)
            {
                try
                {
                    var result = condition();
                    if (result) return;

                    Debug.WriteLine("Wait condition '{0}' is false, retrying...", message);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Wait caught exception {0} for '{1}', retrying...", e.Message, message);
                }

                Thread.Sleep(RETRY_DELAY);
            }

            FailTest(message);
        }

        public static void Wait(string message, Action action, int tryCount = 1)
        {
            if (tryCount >= MAX_RETRIES)
            {
                FailTest(message);

                return;
            }
           
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Wait caught exception {0} for '{1}', retrying...", e.Message, message);
            }

            Thread.Sleep(RETRY_DELAY);

            tryCount += 1;

            Wait(message, action, tryCount);
        }

        private static void FailTest(string message)
        {
            (Driver as ITakesScreenshot).GetScreenshot().SaveAsFile(Path.Combine(ScreenshotDirectory, "failure.png"), ImageFormat.Png);

            Assert.Fail(message);
        }

        public static void ShouldSee(By by)
        {
            Debug.WriteLine(by + " should be visible");

            Wait(@by + " should be visible", () =>
                Driver.FindElements(by).Any(x => x.Displayed) ||
                Driver.FindElements(by).Any(x => x.GetAttribute("visability") == "visible"));
        }

        public static void ShouldSeeText(By by, string text)
        {
            Debug.WriteLine(@by + " should contain " + text);

            Wait(@by + " should contain " + text, () => 
                Driver.FindElements(by).Any(x => x.Text.Contains(text) && x.Displayed));
        }

        public static void ShouldNotSee(By by)
        {
            Debug.WriteLine(by + " should not be visible");

            Wait(@by + " should not be visible", () =>
            {
                try
                {
                    Driver.FindElement(by);
                }
                catch
                {
                    return true;
                }

                return !Driver.FindElement(by).Displayed;
            });
        }

        public static void Clicks(By by)
        {
            Debug.WriteLine("Clicking " + by);

            Wait(@by + " attempting click", () =>
                {
                    Driver.FindElement(by).Click();
                    return true;
                });
        }

        public static void Clicks(By by, string text)
        {
            var element = Driver.FindElements(by).SingleOrDefault(x => x.Text.Contains(text));

            Wait("Attempting click", () => element.Click());
        }

        public static void InputText(By by, string text)
        {
            Debug.WriteLine("Inputing " + by);

            Wait(@by + " should be intputable", () =>
                {
                    Driver.FindElement(by).SendKeys(text);
                    return true;
                });
        }

        public static void MakeSelection(By by, string selectText)
        {
            Debug.WriteLine("Making selection on " + by);
     
            Wait(@by + " should be selectable", () =>
                {
                    var dropDownListBox = Driver.FindElement(by);
                    var element = new SelectElement(dropDownListBox);

                    element.SelectByText(selectText);

                    return true;
                });
        }

        public static void ClearsInput(By by)
        {
            Debug.WriteLine("Clearing " + by);

            Wait(@by + " should be clearable", () =>
                {
                    Driver.FindElement(by).Clear();
                    return true;
                });
        }

        private static bool HasElement(By by)
        {
            var element = Driver.FindElements(by).SingleOrDefault();
            return element != null;
        }
    }
}