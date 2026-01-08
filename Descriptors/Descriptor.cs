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

    [Flags]
    public enum WriteHtmlFlags
    {
        UseSpan = 0x1,
        Detailed = 0x2,
        DiffMode = 0x4,
        KeepDim = 0x8,
    }

    public class Descriptor : IComparable
    {
        public string Name;
        public string Default = "";
        public Dictionary<string, string> Metadata = new Dictionary<string, string>();


        [JsonProperty("Tags")]
        private JArray _
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

        public void AddTag(string tag) => Tags.Add(tag);
        public bool HasTag(string tag) => Tags.Contains(tag);

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

        public virtual string GetSchema(bool detailed = false)
        {
            string schema = "{DescriptorType} {Name}";

            if (detailed)
                schema += " {Tags}";

            return schema;
        }

        public virtual Dictionary<string, object> GetTokens(bool detailed = false)
        {
            var tokens = new Dictionary<string, object>() 
            {
                { "DescriptorType", DescriptorType },
                { "Name", Name }
            };

            string tags = Tags.ToString();

            if (detailed && tags.Length > 0)
                tokens.Add("Tags", tags);

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
                {
                    var obj = tokens[token];
                    value = obj.ToString();
                }
                
                desc = desc.Replace($"{{{token}}}", value);
                search = openToken + value.Length;
            }

            while (desc.Contains("  "))
                desc = desc.Replace("  ", " ");

            return desc.Trim();
        }

        public void WriteHtml(ReflectionHtml html, WriteHtmlFlags flags = WriteHtmlFlags.Detailed)
        {
            bool keepDim = flags.HasFlag(WriteHtmlFlags.KeepDim);
            bool diffMode = flags.HasFlag(WriteHtmlFlags.DiffMode);
            bool detailed = flags.HasFlag(WriteHtmlFlags.Detailed);
            string elemType = flags.HasFlag(WriteHtmlFlags.UseSpan) ? "span" : "div";

            var tokens = GetTokens(detailed);
            string schema = GetSchema(detailed);

            string elemClass = DescriptorType;
            var securityField = GetType().GetField("Security");

            if (!diffMode && Tags.Contains("Deprecated"))
                elemClass += " deprecated"; // The CSS will strike-through this.

            if (!diffMode && DescriptorType != "Class" && DescriptorType != "Enum")
                elemClass += " child";

            if (keepDim || !diffMode)
                elemClass += Hidden ? " dim" : "";

            html.OpenStack(elemType, elemClass, () =>
            {
                int search = 0;

                while (true)
                {
                    int openToken = schema.IndexOf('{', search);

                    if (openToken < 0)
                        break;

                    string symbols = schema.Substring(search, openToken - search);

                    if (symbols.Length > 0)
                        html.Symbol(symbols);

                    int closeToken = schema.IndexOf('}', openToken);

                    if (closeToken < 0)
                        break;

                    string token = schema.Substring(openToken + 1, closeToken - openToken - 1);

                    if (tokens.ContainsKey(token))
                    {
                        if (token == "Tags")
                        {
                            Tags.WriteHtml(html);
                        }
                        else if (token == "DescriptorType")
                        {
                            html.Span($"DescriptorType {DescriptorType}", DescriptorType);
                        }
                        else if (token == "Parameters" || token.EndsWith("Type") || token == "Capabilities")
                        {
                            Type type = GetType();

                            foreach (FieldInfo info in type.GetFields())
                            {
                                if (info.FieldType == typeof(Parameters) && token == "Parameters")
                                {
                                    var parameters = info.GetValue(this) as Parameters;
                                    parameters.WriteHtml(html);
                                    break;
                                }
                                else if (info.FieldType == typeof(LuaType) && token.EndsWith("Type"))
                                {
                                    var luaType = info.GetValue(this) as LuaType;
                                    luaType.WriteHtml(html);
                                    break;
                                }
                                else if (info.FieldType == typeof(Capabilities) && token == "Capabilities")
                                {
                                    var capabilities = info.GetValue(this) as Capabilities;

                                    if (!capabilities.IsEmpty())
                                        html.Span("Capabilities", capabilities.Value);

                                    break;
                                }
                            }
                        }
                        else
                        {
                            string value = tokens[token]
                                .ToString()
                                .Trim();

                            if (value.Length > 0)
                            {
                                html.Span(token, value);
                            }
                        }
                    }

                    search = closeToken + 1;
                }
            });
        }

        [JsonIgnore]
        public virtual bool Hidden
        {
            get
            {
                const int minHidelevel = (int)SecurityType.RobloxScriptSecurity;
                bool hidden = Tags.Contains("Hidden") || Tags.Contains("Deprecated");
                var securityField = GetType().GetField("Security");

                if (securityField != null && !hidden)
                {
                    object value = securityField.GetValue(this);

                    if (value is ReadWriteSecurity rw)
                    {
                        var read = rw.Read.Level;
                        var write = rw.Write.Level;
                        hidden = read >= minHidelevel && write >= minHidelevel;
                    }
                    else if (value is Security sec)
                    {
                        var level = sec.Level;
                        hidden = level >= minHidelevel;
                    }
                }

                return hidden;
            }
            
        }

        public virtual int CompareTo(object other)
        {
            if (other is Descriptor otherDesc)
            {
                int typeDiff = TypePriority - otherDesc.TypePriority;

                if (typeDiff != 0)
                    return typeDiff;

                if (Hidden != otherDesc.Hidden)
                    return Hidden ? 1 : -1;

                return string.CompareOrdinal(Name, otherDesc.Name);
            }

            throw new NotSupportedException("Descriptor can only be compared with another Descriptor.");
        }
    }
}
