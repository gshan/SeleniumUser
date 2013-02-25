﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeleniumUser
{
    public class User
    {
        public static IWebDriver Driver { get; set; }

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

                    Debug.WriteLine(string.Format("Wait condition '{0}' is false, retrying...", message));
                }
                catch (Exception e)
                {
                    Debug.WriteLine(string.Format("Wait caught exception {0} for '{1}', retrying...", e.Message, message));
                }

                Thread.Sleep(RETRY_DELAY);
            }

            (Driver as ITakesScreenshot).GetScreenshot().SaveAsFile(@"c:\temp\screenshots\failure.png", System.Drawing.Imaging.ImageFormat.Png);

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

        public static void ShouldSee(By by)
        {
            Debug.WriteLine(by + " should be visible");
            Wait(@by + " should be visible", () => Driver.FindElement(by).Displayed);
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

            ReadOnlyCollection<IWebElement> frames = Driver.FindElements(By.TagName("iframe"));

            if (frames.Any() && searchFrames)
            {
                var wasPerformed =
                    PerformActionInFrame(() =>
                        {
                            IWebElement element = Driver.FindElements(by).SingleOrDefault();
                            return element != null;
                        }, () =>
                        {
                            Wait(@by + " attempting click", () =>
                                {
                                    Driver.FindElement(by).Click();
                                    return true;
                                });

                            Driver.SwitchTo().DefaultContent();
                        }, FindFrames(null));

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
            ReadOnlyCollection<IWebElement> frames = Driver.FindElements(By.TagName("iframe"));

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
                                                 IWebElement element =
                                                     Driver.FindElements(by).SingleOrDefault(x => x.Text.Contains(text));
                                                 Wait(@by + " attempting click", () => element.Click());
                                                 Driver.SwitchTo().DefaultContent();
                                             }, FindFrames(null));

                Driver.SwitchTo().DefaultContent();

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

        private static bool PerformActionInFrame(Func<bool> condition, Action action, FrameBranch branch)
        {
            foreach (FrameBranch b in branch.Frames)
            {
                NavigateToBranch(b);

                if (condition())
                {
                    action();
                    return true;
                }

                if (!b.HasFrames()) continue;

                if (PerformActionInFrame(condition, action, b))
                {
                    return true;
                }

                NavigateToBranch(b);
            }

            return false;
        }

        public static bool ShouldSeeText(By by, string text, bool searchFrames = false)
        {
            Thread.Sleep(RETRY_DELAY);

            var frames = Driver.FindElements(By.TagName("iframe"));

            if (frames.Any() && searchFrames)
            {
                if (!SearchForTextInFrames(by, text, FindFrames(null)))
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

        private static bool SearchForTextInFrames(By by, string text,  FrameNode branch)
        {
            foreach (var b in branch.Frames)
            {
                NavigateToNode(b);

                if (Driver.FindElements(by).Any(x => x.Text.Contains(text) && x.Displayed))
                {
                    Driver.SwitchTo().DefaultContent();
                    return true;
                }

                if (b.HasFrames)
                {
                    if (SearchForTextInFrames(by, text, b))
                    {
                        return true;
                    }

                    NavigateToNode(b);
                }
            }

            return false;
        }
        
        public static bool ShouldSeeElement(By by, string tagId, bool searchFrames = false)
        {
            Thread.Sleep(RETRY_DELAY);

            ReadOnlyCollection<IWebElement> frames = Driver.FindElements(By.TagName("iframe"));

            if (frames.Any() && searchFrames)
            {
                if (!SearchForElementInFrames(by, tagId, FindFrames(null)))
                {
                    Assert.Fail("Could not find element with Id " + tagId);
                }
            }
            else
            {
                Wait(@by + " should see element with Id " + tagId,
                     () =>
                     Driver.FindElements(by)
                           .Any(
                               x =>
                               x.GetAttribute("id") == tagId &&
                               (x.Displayed || x.GetAttribute("visability") == "visible")));
            }

            return false;
        }

        private static bool SearchForElementInFrames(By by, string tagId, FrameBranch branch)
        {
            foreach (FrameBranch b in branch.Frames)
            {
                NavigateToBranch(b);

                if (Driver.FindElements(by).Any(x => x.GetAttribute("id") == tagId && x.Displayed))
                {
                    Driver.SwitchTo().DefaultContent();
                    return true;
                }

                if (b.HasFrames())
                {
                    if (SearchForElementInFrames(by, tagId, b))
                    {
                        return true;
                    }

                    NavigateToBranch(b);
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

        private static FrameNode FindFrames(FrameNode branch)
        {
            var iFrame = By.TagName("iframe");

            if (branch == null)
                branch = new FrameNode(null);

            NavigateToNode(branch);

            var frames = Driver.FindElements(iFrame);
            if (frames.Any())
            {
                foreach (var frame in frames)
                {
                    var src = frame.GetAttribute("src");
                    var name = frame.GetAttribute("name");
                    var id = frame.GetAttribute("id");

                    var hasFrame = branch.HasFrame(src);

                    if (!hasFrame && !string.IsNullOrEmpty(src))
                    {
                        var childBranch = branch.Add(id, name, src);

                        if (Driver.FindElements(iFrame).Any())
                        {
                            FindFrames(childBranch);

                            NavigateToNode(branch);
                        }
                    }
                }
            }

            return branch;
        }

        private static void NavigateToNode(FrameNode node)
        {
            var path = new List<FrameNode>();

            do
            {
                path.Add(node);

                node = node.Parent;

            } while (node != null && node.HasParent);

            path.Reverse();

            Driver.SwitchTo().DefaultContent();

            foreach (var src in path)
            {
                // navigate to iFrame
                var element = Driver.FindElements(By.TagName("iframe"))
                    .SingleOrDefault(x => x.GetAttribute("src") == node.Src && x.GetAttribute("id") == node.Id && x.GetAttribute("name") == node.Name);

                if (element != null)
                {
                    Driver.SwitchTo().Frame(element);
                }
            }
        }
    }
}
