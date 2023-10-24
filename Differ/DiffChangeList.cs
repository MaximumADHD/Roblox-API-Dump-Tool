using System;
using System.Collections.Generic;
using System.Linq;

namespace RobloxApiDumpTool
{
    public class DiffChangeList : List<object>
    {
        public string Name { get; private set; }
        public string Prefix { get; private set; }

        public DiffChangeList(string name = "ChangeList", string prefix = "")
        {
            Name = name;
            Prefix = prefix;
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

        public void WriteHtml(ReflectionHtml html, bool multiline = false)
        {
            var writeCell = new Action<object, object>((change, prevChange) =>
            {
                if (change is Parameters)
                {
                    var parameters = change as Parameters;
                    parameters.WriteHtml(html, true);
                }
                else if (change is LuaType)
                {
                    if (prevChange is Parameters)
                        html.Symbol("-> ");

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

                    if (tagClass == "String")
                    {
                        html.String(value);
                        return;
                    }

                    html.Span(tagClass, value);
                }
            });

            var buildChangeList = new Action(() =>
            {
                object prevChange = null;

                if (multiline)
                {
                    html.OpenStack("span", "ChangeListPrefix", () =>
                    {
                        html.Text(Prefix);
                        html.Symbol(": ");
                    });
                }
                else
                {
                    html.Text($" {Prefix.Trim()} ");
                }
                
                foreach (object change in this)
                {
                    var writeItem = new Action(() =>
                    {
                        writeCell(change, prevChange);
                        prevChange = change;
                    });

                    if (multiline)
                    {
                        html.OpenStack("span", "ChangeListItem", writeItem);
                        continue;
                    }

                    writeItem();
                }
            });

            if (multiline)
            {
                html.OpenStack("div", "ChangeList", buildChangeList);
                return;
            }
            
            html.OpenSpan(Name, buildChangeList);
        }
    }
}