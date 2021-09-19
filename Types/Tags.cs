using System.Collections.Generic;
using System.Linq;

namespace RobloxApiDumpTool
{
    public class Tags : HashSet<string>
    {
        public Tags(IEnumerable<string> tags = null)
        {
            tags?.ToList().ForEach(tag => Add(tag));
        }

        public void ClearBadData()
        {
            if (!Contains("ReadOnly"))
                return;

            Remove("NotReplicated");
        }

        public void SwitchToPreliminary()
        {
            if (Contains("Deprecated"))
            {
                Add("Preliminary");
                Remove("Deprecated");
            }
        }

        public new bool Add(string value)
        {
            bool result = base.Add(value);
            ClearBadData();

            return result;
        }

        public override string ToString()
        {
            ClearBadData();
            var tags = this.Select(tag => $"[{tag}]");
            return string.Join(" ", tags);
        }

        public string Signature
        {
            get
            {
                if (Count > 0)
                {
                    ClearBadData();
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
            ClearBadData();
            var tags = this.ToList();
            tags.ForEach(tag => buffer.WriteElement("Tag", $"[{tag}]", numTabs));
        }
    }
}
