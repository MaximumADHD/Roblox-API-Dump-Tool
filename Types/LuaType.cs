using System;
using System.Collections.Generic;

namespace RobloxApiDumpTool
{
    public enum TypeCategory
    {
        Primitive,
        Class,
        Enum,
        Group,
        DataType
    }

    public class LuaType
    {
        public string Name;
        public TypeCategory Category;
        public bool IsReturnType = false;

        public override string ToString() => GetSignature();

        private static IReadOnlyDictionary<string, string> LuauTypes = new Dictionary<string, string>()
        {
            { "Dictionary", "{ [string]: any }" },
            { "Map", "{ [string]: any }" },
            { "Array", "{ any }" },

            { "Objects", "{ Instance }" },
            { "Tuple", "...any" },
            { "Function", "((...any) -> ...any)" },
            { "OptionalCoordinateFrame", "CFrame?" },

            { "Content", "string" },
            { "ProtectedString", "string" },

            { "null", "()" },
            { "void", "()" },

            { "int", "number" },
            { "int64", "number" },
            { "float", "number" },
            { "double", "number" },

            { "bool", "boolean" },
            { "Variant", "any" },
        };

        public string LuauType
        {
            get
            {
                if (LuauTypes.ContainsKey(Name))
                    return LuauTypes[Name];

                return Name;
            }
        }

        public string GetSignature(bool ignoreReturnType = false)
        {
            string result;

            if (Category != TypeCategory.Enum)
                result = Name;
            else
                result = $"{Category}.{Name}";

            return result;
        }

        public void WriteHtml(ReflectionHtml html)
        {
            string typeVal = GetSignature(true);
            bool optional = false;

            if (typeVal.StartsWith("Enum."))
            {
                html.Span("Type", "Enum");
                html.Symbol(".");

                typeVal = typeVal.Substring(5);
            }

            if (typeVal.EndsWith("?"))
            {
                optional = true;
                Name = Name.Replace("?", "");
            }

            switch (Name)
            {
                case "Array":
                {
                    html.Symbol("{ ");
                    html.Span("Type", "any");
                    html.Symbol(" }");
                    break;
                }
                case "Dictionary":
                case "Map":
                {
                    html.Symbol("{ [");
                    html.Span("Type", "string");
                    html.Symbol("]: ");
                    html.Span("Type", "any");
                    html.Symbol(" }");
                    break;
                }
                case "Objects":
                {
                    html.Symbol("{ ");
                    html.Span("Type", "Instance");
                    html.Symbol(" }");
                    break;
                }
                case "Tuple":
                {
                    html.Symbol("...");
                    html.Span("Type", "any");
                    break;
                }
                case "Function":
                {
                    if (optional)
                        html.Symbol("((...");
                    else
                        html.Symbol("(...");

                    html.Span("Type", "any");
                    html.Symbol(") -> ...");
                    html.Span("Type", "any");

                    if (optional)
                        html.Symbol(")");

                    break;
                }
                case "null":
                case "void":
                {
                    html.Symbol("()");
                    break;
                }
                default:
                {
                    html.Span("Type", LuauType);
                    break;
                }
            }

            if (!optional)
                return;

            html.Symbol("?");
        }
    }
}
