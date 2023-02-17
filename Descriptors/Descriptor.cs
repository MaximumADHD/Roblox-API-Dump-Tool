using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RobloxApiDumpTool
{
    // This defines the priority of each descriptor type.
    public enum TypePriority
    {
        Class,
        Property,
        Function,
        Event,
        Callback,
        Enum,
        EnumItem,
        LegacyName,
        Tag,

        Unknown = -1
    }

    public class Descriptor : IComparable
    {
        public string Name;
        public string Default = "";
        public Dictionary<string, string> Metadata = new Dictionary<string, string>();


        [JsonProperty("Tags")]
        private JArray JsonTags
        {
            get => null;

            set
            {
                Tags.Clear();
                Metadata.Clear();

                foreach (var element in value)
                {
                    if (element is JObject obj)
                    {
                        Metadata = obj.ToObject<Dictionary<string, string>>();
                        continue;
                    }

                    string tag = element.ToString();
                    Tags.Add(tag);
                }

                Tags.ClearBadData();
            }
        }

        [JsonIgnore]
        public Tags Tags = new Tags();

        public string Summary => Describe(false);
        public string Signature => Describe(true);

        public bool AddTag  (string tag) => Tags.Add(tag);
        public bool DropTag (string tag) => Tags.Remove(tag);
        public bool HasTag  (string tag) => Tags.Contains(tag);

        public override string ToString() => Summary;

        public readonly string DescriptorType;
        public readonly TypePriority TypePriority;

        private static readonly Dictionary<Type, Descriptor> InitCache = new Dictionary<Type, Descriptor>();

        public Descriptor()
        {
            var type = GetType();

            if (!InitCache.ContainsKey(type))
            {
                string descType = GetType().Name;

                if (descType != DescriptorType)
                {
                    DescriptorType = descType.Replace("Descriptor", "");
                    Enum.TryParse(DescriptorType, out TypePriority);
                }

                InitCache.Add(type, this);
            }
            else
            {
                var baseRef = InitCache[type];
                TypePriority = baseRef.TypePriority;
                DescriptorType = baseRef.DescriptorType;
            }
        }

        public class HtmlConfig
        {
            public int NumTabs = 0;

            public bool DiffMode = true; 
            public bool Detailed = false;

            public string TagType = "span";
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
            var tokens = new Dictionary<string, object>() { { "Name", Name } };
            string tags = Tags.ToString();

            if (detailed && tags.Length > 0)
                tokens.Add("Tags", tags);

            return tokens;
        }

        public string Describe(bool detailed = false)
        {
            int search = 0;
            
            var tokens = GetTokens(detailed);
            string desc = DescriptorType + ' ' + GetSchema(detailed);

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

            while (desc.Contains("  "))
                desc = desc.Replace("  ", " ");

            return desc.Trim();
        }
        
        public void WriteHtml(ReflectionDumper buffer, HtmlConfig config = null)
        {
            if (config == null)
                config = new HtmlConfig();

            int numTabs = config.NumTabs;
            string tagType = config.TagType;

            bool detailed = config.Detailed;
            bool diffMode = config.DiffMode;

            var tokens = GetTokens(detailed);
            tokens.Remove("DescriptorType");

            string schema = GetSchema(detailed);
            string tagClass = DescriptorType;

            if (!diffMode && Tags.Contains("Deprecated"))
                tagClass += " deprecated"; // The CSS will strike-through this.

            if (!diffMode && DescriptorType != "Class" && DescriptorType != "Enum")
                tagClass += " child";
            
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
                                var parameters = info.GetValue(this) as Parameters;
                                parameters.WriteHtml(buffer, numTabs + 1);
                                break;
                            }
                            else if (info.FieldType == typeof(LuaType) && token.EndsWith("Type"))
                            {
                                var luaType = info.GetValue(this) as LuaType;
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
                                token += " " + DescriptorType;

                            buffer.WriteElement(token, value, numTabs + 1);
                        }
                    }
                }

                search = closeToken + 1;
            }

            buffer.CloseClassTag(numTabs, tagType);
        }

        public void WriteHtml(ReflectionDumper buffer, int numTabs)
        {
            var config = new HtmlConfig() { NumTabs = numTabs };
            WriteHtml(buffer, config);
        }

        public void WriteHtml(ReflectionDumper buffer, int numTabs, bool detailed)
        {
            WriteHtml(buffer, new HtmlConfig()
            {
                NumTabs = numTabs,
                Detailed = detailed
            });
        }

        public virtual int CompareTo(object other)
        {
            if (other is Descriptor otherDesc)
            {
                int typeDiff = TypePriority - otherDesc.TypePriority;

                if (typeDiff != 0)
                    return typeDiff;

                return string.CompareOrdinal(Name, otherDesc.Name);
            }

            throw new NotSupportedException("Descriptor can only be compared with another Descriptor.");
        }
    }
}
