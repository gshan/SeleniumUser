using OpenQA.Selenium;
using System.Collections.Generic;
using System.Linq;

namespace SeleniumUser
{
    public class FrameNode
    {
        public FrameNode(FrameNode parent)
        {
            this.Frames = new List<FrameNode>();

            this.Parent = parent;
        }

        public FrameNode Parent 
        { 
            get; 
            set; 
        }

        public string Src 
        { 
            get; 
            set; 
        }

        public string Id 
        { 
            get; 
            set; 
        }

        public string Name 
        { 
            get; 
            set; 
        }

        public List<FrameNode> Frames 
        { 
            get; 
            protected set; 
        }

        public bool HasFrames
        {
            get 
            { 
                return Frames.Any(); 
            }
        }

        public bool HasParent 
        { 
            get 
            { 
                return this.Parent != null; 
            } 
        }

        public bool HasFrame(string src)
        {
            return this.Frames.Any(x => x.Src == src);
        }

        public FrameNode Add(string id, string name, string src)
        {
            var branch = new FrameNode(this)
            {
                Id = id,
                Name = name,
                Src = src
            };

            Frames.Add(branch);

            return branch;
        }

        public static FrameNode FindFrames(IWebDriver driver, FrameNode branch = null)
        {
            var iFrame = By.TagName("iframe");

            if (branch == null)
                branch = new FrameNode(null);

            NavigateToNode(driver, branch);

            var frames = driver.FindElements(iFrame);
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

                        if (driver.FindElements(iFrame).Any())
                        {
                            FindFrames(driver, childBranch);

                            NavigateToNode(driver, branch);
                        }
                    }
                }
            }

            return branch;
        }

        public static void NavigateToNode(IWebDriver driver, FrameNode node)
        {
            var path = new List<FrameNode>();

            do
            {
                path.Add(node);
                node = node.Parent;

            } while (node != null && node.HasParent);

            path.Reverse();

            driver.SwitchTo().DefaultContent();

            foreach (var n in path)
            {
                // navigate to iFrame
                var element = driver.FindElements(By.TagName("iframe"))
                    .SingleOrDefault(x => 
                        x.GetAttribute("src") == n.Src && 
                        x.GetAttribute("id") == n.Id && 
                        x.GetAttribute("name") == n.Name);

                if (element != null)
                {
                    driver.SwitchTo().Frame(element);
                }
            }
        }
    }
}
