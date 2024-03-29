﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace RobloxApiDumpTool
{
    public class Parameter
    {
        private const string quote = "\"";

        public LuaType Type;
        public string Name;
        public string Default;

        private static IReadOnlyDictionary<string, string> Defaults = new Dictionary<string, string>()
        {
            { "CFrame", "identity" },
            { "Vector2", "zero" },
            { "Vector3", "zero" },
        };

        public override string ToString()
        {
            if (Default != null && !Type.Name.EndsWith("?"))
                Type.Name += "?";

            string result = $"{Name}: {Type}";
            string category = $"{Type.Category}";

            if (Default != null)
            {
                if (Type.AbsoluteName == "string" || category == "Enum")
                    if (!Default.StartsWith(quote) && !Default.EndsWith(quote))
                        Default = quote + Default + quote;

                if (Type.Category == TypeCategory.DataType && Type.AbsoluteName != "Function")
                    Default = $"{Type.Name}.new()";

                if (Default.Length == 0)
                    return result;

                result += " = " + Default;
            }
            
            return result;
        }

        public void WriteHtml(ReflectionHtml html)
        {
            string name = Name;
            LuaType luaType = Type;
            string paramDef = Default;

            if (paramDef != null && !luaType.Name.EndsWith("?"))
                luaType.Name += "?";

            html.OpenSpan("Parameter", () =>
            {
                if (luaType.Name == "Tuple")
                {
                    html.Symbol("...: ");
                    html.Span("Type", "any");
                }
                else
                {
                    html.Span("ParamName", name);
                    html.Symbol(": ");
                    luaType.WriteHtml(html);
                }
                
                // Write Default
                if (paramDef != null && paramDef != "nil")
                {
                    string typeLbl = luaType.GetSignature();
                    string typeName = luaType.AbsoluteName;
                    html.Symbol(" = ");

                    if (luaType.Category == TypeCategory.DataType && typeName != "Function")
                    {
                        html.Span("Type", typeName);
                        html.Symbol(".");

                        if (Defaults.ContainsKey(typeName))
                        {
                            string def = Defaults[typeName];
                            html.Span("Name", def);
                        }
                        else
                        {
                            html.Span("Name", "new");
                            html.Symbol("()");
                        }
                    }
                    else
                    {
                        if (luaType.Category == TypeCategory.Enum)
                        {
                            html.String(paramDef);
                            return;
                        }

                        typeName = luaType.AbsoluteName;

                        if (luaType.AbsoluteLuauType == "number")
                        {
                            typeName = "number";

                            if (paramDef.StartsWith("-"))
                            {
                                html.Symbol("-");
                                paramDef = paramDef.Substring(1);
                            }
                        }

                        if (typeName.ToLowerInvariant() == "string")
                        {
                            html.String(paramDef);
                            return;
                        }
                        else if (typeName == "Array" || typeName == "Dictionary")
                        {
                            html.Symbol(paramDef);
                            return;
                        }

                        html.Span(luaType.AbsoluteLuauType, paramDef);
                    }
                }
            });
        }
    }

    public class Parameters : List<Parameter>
    {
        public override string ToString()
        {
            string[] parameters = this.Select(param => param.ToString()).ToArray();
            return '(' + string.Join(", ", parameters) + ')';
        }

        public void WriteHtml(ReflectionHtml html, bool diffMode = false)
        {
            string paramsTag = "Parameters";
            IEnumerable<Parameter> parameters = this;

            if (diffMode)
                paramsTag += " change";

            html.OpenSpan(paramsTag, () =>
            {
                html.Symbol("(");

                for (int i = 0; i < Count; i++)
                {
                    var param = this[i];

                    if (i > 0)
                        html.Symbol(", ");

                    param.WriteHtml(html);
                }

                html.Symbol(")");
            });
        }
    }
}