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
    }
}
