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

        public new bool Add(string value)
        {
            if (Contains("ReadOnly") && value == "NotReplicated")
                return false;
            else if (value == "ReadOnly" && Contains("NotReplicated"))
                Remove("NotReplicated");

            return base.Add(value);
        }

        public override string ToString()
        {
            var tags = this.Select(tag => $"[{tag}]");
            return string.Join(" ", tags);
        }

        public string Signature
        {
            get
            {
                if (Contains("ReadOnly"))
                    Remove("NotReplicated");

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
            var tags = this.ToList();
            tags.ForEach(tag => buffer.WriteElement("Tag", $"[{tag}]", numTabs));
        }

        public void SwitchToPreliminary()
        {
            if (Contains("Deprecated"))
            {
                Add("Preliminary");
                Remove("Deprecated");
            }
        }
    }
}
