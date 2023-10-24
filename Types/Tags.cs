using System.Collections.Generic;
using System.Linq;

namespace RobloxApiDumpTool
{
    public class Tags : List<string>
    {
        public Tags(IEnumerable<string> tags = null)
        {
            tags?.ToList().ForEach(tag => Add(tag));
            Sort();
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

        public new void Add(string value)
        {
            base.Add(value);
            ClearBadData();
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

        public void WriteHtml(ReflectionHtml html)
        {
            ClearBadData();
            Sort();
            
            for (int i = 0; i < Count; i++)
            {
                if (i > 0)
                    html.Symbol(" ");

                html.Span("Tag", $"[{this[i]}]");
            }
        }
    }
}
