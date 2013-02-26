﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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

            (Driver as ITakesScreenshot).GetScreenshot().SaveAsFile(Path.Combine(ScreenshotDirectory, "failure.png"), ImageFormat.Png);

            Assert.Fail(message);
        }

        public static void Wait(string message, Action action, int tryCount = 1)
        {
            if (tryCount >= MAX_RETRIES)
                return;
           
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Wait caught exception {0} for '{1}', retrying...", e.Message, message);
                
                Thread.Sleep(RETRY_DELAY);

                tryCount += 1;

                Wait(message, action, tryCount);
            }
            
        }

        public static void ShouldSee(By by, bool searchFrames = false)
        {
            Debug.WriteLine(by + " should be visible");

            var frames = Driver.FindElements(By.TagName("iframe"));

            if (frames.Any() && searchFrames)
            {
                if (!SearchForElementInFrames(by, FrameNode.FindFrames(Driver)))
                {
                    Assert.Fail("Could not find element by" + @by);
                }
            }
            else
            {
                Wait(@by + " should be visible", () =>
                    Driver.FindElement(by).Displayed ||
                    Driver.FindElement(by).GetAttribute("visability") == "visible");
            }
        }

        private static bool SearchForElementInFrames(By by, FrameNode node)
        {
            foreach (var n in node.Frames)
            {
                FrameNode.NavigateToNode(Driver, n);

                if (Driver.FindElements(by).Any(x => x.Displayed) ||
                    Driver.FindElement(by).GetAttribute("visability") == "visible")
                {
                    Driver.SwitchTo().DefaultContent();
                    return true;
                }

                if (n.HasFrames)
                {
                    if (SearchForElementInFrames(by, n))
                    {
                        return true;
                    }

                    FrameNode.NavigateToNode(Driver, n);
                }
            }

            return false;
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

        public static void Clicks(By by, bool searchFrames = false)
        {
            Debug.WriteLine("Clicking " + by);

            var frames = Driver.FindElements(By.TagName("iframe"));
            if (frames.Any() && searchFrames)
            {
                var wasPerformed =
                    PerformActionInFrame(() =>
                        {
                            var element = Driver.FindElements(by).SingleOrDefault();
                            return element != null;
                        }, () =>
                        {
                            Wait(@by + " attempting click", () =>
                                {
                                    Driver.FindElement(by).Click();
                                    return true;
                                });

                            Driver.SwitchTo().DefaultContent();
                        }, FrameNode.FindFrames(Driver));

                if (!wasPerformed)
                {
                    Assert.Fail(@by + " was not clicked");
                }
            }
            else
            {
                Wait(@by + " attempting click", () =>
                    {
                        Driver.FindElement(by).Click();
                        return true;
                    });
            }
        }

        public static void Clicks(By by, string text, bool searchFrames = false)
        {
            var frames = Driver.FindElements(By.TagName("iframe"));

            if (frames.Any() && searchFrames)
            {
                var wasPerformed =
                    PerformActionInFrame(() =>
                        {
                            IWebElement element = Driver.FindElements(by).SingleOrDefault(x => x.Text.Contains(text));
                            return element != null;
                        },
                                         () =>
                                             {
                                                 var element =
                                                     Driver.FindElements(by).SingleOrDefault(x => x.Text.Contains(text));
                                                 Wait(@by + " attempting click", () => element.Click());
                                                 Driver.SwitchTo().DefaultContent();
                                             }, FrameNode.FindFrames(Driver));

                if (!wasPerformed)
                {
                    Assert.Fail(@by + " was not clicked");
                }
            }
            else
            {
                IWebElement element = Driver.FindElements(by).SingleOrDefault(x => x.Text.Contains(text));

                Wait("Attempting click", () => element.Click());
            }
        }

        private static bool PerformActionInFrame(Func<bool> condition, Action action, FrameNode node)
        {
            foreach (var n in node.Frames)
            {
                FrameNode.NavigateToNode(Driver, n);

                if (condition())
                {
                    action();
                    return true;
                }

                if (!n.HasFrames) continue;

                if (PerformActionInFrame(condition, action, n))
                {
                    return true;
                }

                FrameNode.NavigateToNode(Driver, n);
            }

            return false;
        }

        public static bool ShouldSeeText(By by, string text, bool searchFrames = false)
        {
            Thread.Sleep(RETRY_DELAY);

            var frames = Driver.FindElements(By.TagName("iframe"));

            if (frames.Any() && searchFrames)
            {
                if (!SearchForTextInFrames(by, text, FrameNode.FindFrames(Driver)))
                {
                    Assert.Fail("Could not find text " + text);
                }
            }
            else
            {
                Wait(@by + " should contain " + text, () => Driver.FindElements(by).Any(x => x.Text.Contains(text) && x.Displayed));
            }

            return false;
        }

        private static bool SearchForTextInFrames(By by, string text,  FrameNode node)
        {
            foreach (var n in node.Frames)
            {
                FrameNode.NavigateToNode(Driver, n);

                if (Driver.FindElements(by).Any(x => x.Text.Contains(text) && x.Displayed))
                {
                    Driver.SwitchTo().DefaultContent();
                    return true;
                }

                if (n.HasFrames)
                {
                    if (SearchForTextInFrames(by, text, n))
                    {
                        return true;
                    }

                    FrameNode.NavigateToNode(Driver, n);
                }
            }

            return false;
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
                var clickThis = new SelectElement(dropDownListBox);

                clickThis.SelectByText(selectText);

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
    }
}