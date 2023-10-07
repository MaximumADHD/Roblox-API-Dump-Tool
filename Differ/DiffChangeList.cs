using System;
using System.Collections.Generic;
using System.Linq;

namespace RobloxApiDumpTool
{
    public class DiffChangeList : List<object>
    {
        public string Name { get; private set; }

        public DiffChangeList(string name = "ChangeList")
        {
            Name = name;
        }

        private void PreformatList()
        {
            object lastChange = null;

            foreach (object change in this)
            {
                if (lastChange is Parameters)
                    if (change is LuaType type)
                        type.IsReturnType = true;

                lastChange = change;
            }
        }

        public string ListElements(string separator, string prefix = "")
        {
            PreformatList();

            string[] elements = this
                .Select(elem => prefix + elem.ToString())
                .ToArray();

            return string.Join(separator, elements);
        }

        public override string ToString()
        {
            return ListElements(" ");
        }

        public void WriteHtml(ReflectionHtml html, bool multiline = false)
        {
            Action buildChangeList = new Action(() =>
            {
                PreformatList();

                foreach (object change in this)
                {
                    if (change is Parameters)
                    {
                        var parameters = change as Parameters;
                        parameters.WriteHtml(html, true);
                    }
                    else if (change is LuaType)
                    {
                        var type = change as LuaType;
                        type.WriteHtml(html);
                    }
                    else if (change is Descriptor)
                    {
                        var desc = change as Descriptor;
                        desc.WriteHtml(html);
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

                        if (value.Contains("🧬"))
                            tagClass = "ThreadSafety";
                        else if (value.StartsWith("["))
                            tagClass = "Serialization";
                        else if (value.StartsWith("{"))
                            tagClass = "Security";
                        else if (value.StartsWith("\""))
                            tagClass = "String";
                        else
                            tagClass = change.GetType().Name;

                        if (tagClass == "Security" && value.Contains("None"))
                            tagClass += " darken";

                        html.Span(tagClass, value);
                    }
                }
            });

            if (multiline)
            {
                html.OpenDiv(Name, () => html.OpenSpan("Changelist", buildChangeList));
                return;
            }
            
            html.OpenSpan(Name, buildChangeList);
        }
    }
}