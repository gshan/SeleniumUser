using OpenQA.Selenium;
using System;

namespace SeleniumUser
{
    public class FrameContext : IDisposable
    {
        private IWebDriver Driver { get; set; }

        /// <summary>
        /// Switches the web driver context to the specified iFrame.
        /// If the iFrame is contained within another iFrame, specifiy the path to the iFrame.
        /// </summary>
        /// <param name="driver">Web driver</param>
        /// <param name="frameNames">Frame names or IDs</param>
        public FrameContext(IWebDriver driver, params string[] frameNames)
        {
            this.Driver = driver;

            NavigateToFrame(frameNames);
        }

        private void NavigateToFrame(params string[] frameNames)
        {
            foreach (var frameName in frameNames)
            {
                this.Driver.SwitchTo().Frame(frameName);
            }
        }

        public void Dispose()
        {
            this.Driver.SwitchTo().DefaultContent();
        }
    }
}
