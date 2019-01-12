using System;
using System.Collections.Generic;
using System.Reflection;

namespace Roblox.Reflection
{
    public class Descriptor : IComparable
    {
        public string Name;
        public Tags Tags = new Tags();

        public string Summary => Describe(false);
        public string Signature => Describe(true);

        public override string ToString() => Summary;
        
        public string GetDescriptorType()
        {
            string descType = GetType().Name;

            if (descType != "Descriptor")
                descType = descType.Replace("Descriptor", "");

            return descType;
        }

        public virtual string GetSchema(bool detailed = false)
        {
            string schema = "{DescriptorType} {Name}";

            if (detailed)
                schema += " {Tags}";

            return schema;
        }

        public virtual Dictionary<string, object> GetTokens(bool detailed = false)
        {
            var tokens = new Dictionary<string, object>();
            tokens.Add("Name", Name);

            string descType = GetDescriptorType();
            tokens.Add("DescriptorType", descType);

            string tags = Tags.ToString();
            if (detailed && tags.Length > 0)
                tokens.Add("Tags", Tags);

            return tokens;
        }

        public string Describe(bool detailed = false)
        {
            int search = 0;

            var tokens = GetTokens(detailed);
            string desc = GetSchema(detailed);

            while (search < desc.Length)
            {
                int openToken = desc.IndexOf('{', search);
                if (openToken < 0)
                    break;

                int closeToken = desc.IndexOf('}', openToken);
                if (closeToken < 0)
                    break;

                string token = desc.Substring(openToken + 1, closeToken - openToken - 1);
                string value = "";

                if (tokens.ContainsKey(token))
                    value = tokens[token].ToString();

                desc = desc.Replace('{' + token + '}', value);
                search = openToken + value.Length;
            }

            desc = desc.Replace("  ", " ");
            desc = desc.Trim();

            return desc;
        }

        public virtual int CompareTo(object other)
        {
            string label;

            if (other is Descriptor)
            {
                var otherDesc = other as Descriptor;
                label = otherDesc.Name;
            }
            else
            {
                label = other.ToString();
            }

            return string.CompareOrdinal(Name, label);
        }

        public void WriteHtml(ReflectionDumper buffer, int numTabs = 0, bool detailed = true, bool diffMode = false)
        {
            var tokens = GetTokens(detailed);
            tokens.Remove("DescriptorType");

            string schema = GetSchema(detailed);
            string descType = GetDescriptorType();

            string tagClass = descType;

            if (!diffMode && Tags.Contains("Deprecated"))
                tagClass += " deprecated"; // The CSS will strike-through this.

            if (!diffMode && descType != "Class" && descType != "Enum")
                tagClass += " child";

            string tagType = diffMode ? "span" : "div";
            buffer.OpenClassTag(tagClass, numTabs, tagType);
            buffer.NextLine();

            int search = 0;

            while (true)
            {
                int openToken = schema.IndexOf('{', search);
                if (openToken < 0)
                    break;

                int closeToken = schema.IndexOf('}', openToken);
                if (closeToken < 0)
                    break;

                string token = schema.Substring(openToken + 1, closeToken - openToken - 1);
                if (tokens.ContainsKey(token))
                {
                    if (token == "Tags")
                    {
                        Tags.WriteHtml(buffer, numTabs + 1);
                    }
                    else if (token == "Parameters" || token.EndsWith("Type"))
                    {
                        Type type = GetType();

                        foreach (FieldInfo info in type.GetFields())
                        {
                            if (info.FieldType == typeof(Parameters) && token == "Parameters")
                            {
                                Parameters parameters = info.GetValue(this) as Parameters;
                                parameters.WriteHtml(buffer, numTabs + 1);
                                break;
                            }
                            else if (info.FieldType == typeof(LuaType) && token.EndsWith("Type"))
                            {
                                LuaType luaType = info.GetValue(this) as LuaType;
                                luaType.WriteHtml(buffer, numTabs + 1);
                                break;
                            }
                        }
                    }
                    else
                    {
                        string value = tokens[token]
                            .ToString()
                            .Replace("<", "&lt;")
                            .Replace(">", "&gt;")
                            .Trim();

                        if (value.Length > 0)
                        {
                            if (token == "ClassName")
                                token += " " + descType;

                            buffer.OpenClassTag(token, numTabs + 1);
                            buffer.Write(value);
                            buffer.CloseClassTag();
                        }
                    }
                }

                search = closeToken + 1;
            }

            buffer.CloseClassTag(numTabs, tagType);
        }
    }
}
