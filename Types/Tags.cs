using System.Collections.Generic;
using System.Linq;

namespace Roblox.Reflection
{
    public class Tags : HashSet<string>
    {
        public Tags(IEnumerable<string> tags = null)
        {
            tags?.ToList().ForEach(tag => Add(tag));
        }

        public override string ToString()
        {
            string[] tags = this.Select(tag => '[' + tag + ']').ToArray();
            return string.Join(" ", tags);
        }

        public string Signature
        {
            get
            {
                if (Count > 0)
                {
                    string label = "Tag";
                    if (Count > 1)
                        label += "s";

                    return label + ' ' + ToString();
                }

                return "";
            }
        }

        public void WriteHtml(ReflectionDumper buffer, int numTabs = 0)
        {
            foreach (string tag in this)
            {
                buffer.OpenClassTag("Tag", numTabs);
                buffer.Write('[' + tag + ']');
                buffer.CloseClassTag();
            }
        }
    }
}
