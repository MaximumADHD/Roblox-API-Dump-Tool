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
            switch (value)
            {
                case "ReadOnly":
                {
                    if (Contains("NotReplicated"))
                        Remove("NotReplicated");

                    goto default;
                }
                case "NotReplicated":
                {
                    if (Contains("ReadOnly"))
                        return false;

                    goto default;
                }
                default:
                {
                    return base.Add(value);
                }
            }
        }

        public override string ToString()
        {
            if (Contains("ReadOnly"))
                Remove("NotReplicated");

            var tags = this.Select(tag => $"[{tag}]");
            return string.Join(" ", tags);
        }

        public string Signature
        {
            get
            {
                if (Count > 0)
                {
                    if (Contains("ReadOnly"))
                        Remove("NotReplicated");

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
            if (Contains("ReadOnly"))
                Remove("NotReplicated");

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
