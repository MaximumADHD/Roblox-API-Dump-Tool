using System.Collections.Generic;
using System.Linq;

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
        public LuaType[] SubTypes = new LuaType[0];

        public override string ToString() => GetSignature();

        private static IReadOnlyDictionary<string, string> LuauTypes = new Dictionary<string, string>()
        {
            { "Dictionary", "{ [string]: any }" },
            { "Map", "{ [string]: any }" },
            { "Array", "{ any }" },

            { "Objects", "{ Instance }" },
            { "Function", "((...any) -> ...any)" },
            { "OptionalCoordinateFrame", "CFrame?" },
            { "CoordinateFrame", "CFrame" },

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

        public bool Optional
        {
            get => Name.EndsWith("?") || LuauType.EndsWith("?");

            set
            {
                if (value)
                {
                    if (Name.EndsWith("?"))
                        return;

                    Name += "?";
                }
                else
                {
                    if (!Name.EndsWith("?"))
                        return;

                    Name = AbsoluteName;
                }
            }
        }

        public string LuauType
        {
            get
            {
                if (LuauTypes.ContainsKey(AbsoluteName))
                    return LuauTypes[AbsoluteName];

                if (AbsoluteName == "Tuple")
                {
                    string typeName = "...any";

                    if (SubTypes.Any())
                    {
                        var names = SubTypes.Select(type => type.AbsoluteLuauType);
                        typeName = $"({string.Join(", ", names)})";
                    }

                    return typeName;
                }

                return Name;
            }
        }

        public string AbsoluteName => Name.Replace("?", "");
        public string AbsoluteLuauType => LuauType.Replace("?", "");

        public string GetSignature()
        {
            string result;

            if (Category == TypeCategory.Enum)
                result = $"{Category}.{Name}";
            else
                result = LuauType;

            if (Optional && !result.EndsWith("?"))
                result += "?";
            else if (!Optional && Name == "Function")
                result = "(...any) -> ...any";

            return result;
        }

        public void WriteHtml(ReflectionHtml html)
        {
            if (Category == TypeCategory.Enum)
            {
                html.Span("Type", "Enum");
                html.Symbol(".");
            }

            switch (AbsoluteName)
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
                    if (SubTypes.Any())
                    {
                        html.Symbol("(");

                        for (int i = 0; i < SubTypes.Length; i++)
                        {
                            var subType = SubTypes[i];

                            if (i > 0)
                                html.Symbol(", ");

                            subType.WriteHtml(html);
                        }

                        html.Symbol(")");
                    }
                    else
                    {
                        html.Symbol("...");
                        html.Span("Type", "any");
                    }

                    break;
                }
                case "Function":
                {
                    if (Optional)
                        html.Symbol("(");

                    html.Symbol("(...");
                    html.Span("Type", "any");

                    html.Symbol(") -> ...");
                    html.Span("Type", "any");

                    if (Optional)
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
                    html.Span("Type", AbsoluteLuauType);
                    break;
                }
            }

            if (!Optional)
                return;

            html.Symbol("?");
        }
    }
}
