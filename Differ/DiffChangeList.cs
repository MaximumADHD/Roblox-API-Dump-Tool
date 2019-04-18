using System.Collections.Generic;
using System.Linq;

namespace Roblox.Reflection
{
    public class DiffChangeList : List<object>
    {
        public string Name { get; private set; }

        public DiffChangeList(string name = "ChangeList")
        {
            Name = name;
        }

        public string ListElements(string separator, string prefix = "")
        {
            string[] elements = this
                .Select(elem => prefix + elem.ToString())
                .ToArray();

            return string.Join(separator, elements);
        }

        public override string ToString()
        {
            return ListElements(" ");
        }

        public void WriteHtml(ReflectionDumper buffer, bool multiline = false, Descriptor.HtmlConfig config = null)
        {
            if (config == null)
                config = new Descriptor.HtmlConfig();

            int numTabs;

            if (multiline)
            {
                buffer.OpenClassTag(Name, 1, "div");
                buffer.NextLine();

                buffer.OpenClassTag("ChangeList", 2);
                numTabs = 3;
            }
            else
            {
                buffer.OpenClassTag(Name, 1);
                numTabs = 2;
            }

            if (config.NumTabs == 0)
                config.NumTabs = numTabs;

            buffer.NextLine();

            foreach (object change in this)
            {
                int stack = numTabs;

                if (change is Parameters)
                {
                    var parameters = change as Parameters;
                    parameters.WriteHtml(buffer, numTabs, true);
                }
                else if (change is LuaType)
                {
                    var type = change as LuaType;
                    type.WriteHtml(buffer, numTabs);
                }
                else if (change is Descriptor)
                {
                    var desc = change as Descriptor;
                    desc.WriteHtml(buffer, config);
                }
                else
                {
                    string value;

                    if (change is Security)
                    {
                        var security = change as Security;
                        value = security.Describe(true);
                    }
                    else
                    {
                        value = change.ToString();
                    }

                    string tagClass;

                    if (value.StartsWith("{") && value.EndsWith("}"))
                        tagClass = "Security";
                    else if (value.StartsWith("[") && value.EndsWith("]"))
                        tagClass = "Serialization";
                    else if (value.StartsWith("\"") && value.EndsWith("\""))
                        tagClass = "String";
                    else
                        tagClass = change.GetType().Name;

                    if (tagClass == "Security" && value == "{None}")
                        tagClass += " darken";

                    buffer.WriteElement(tagClass, value, numTabs);
                }
            }

            buffer.CloseClassTag(numTabs - 1);

            if (multiline)
            {
                buffer.CloseClassTag(1, "div");
            }
        }
    }
}