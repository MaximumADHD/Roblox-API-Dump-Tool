using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Roblox.Reflection
{
    public class Descriptor : IComparable
    {
        public string Name;

        public string Summary => Describe(false);
        public string Signature => Describe(true);

        public override string ToString() => Summary;
        public List<string> Tags = new List<string>();

        /// <summary>
        ///     If called from a derived class, this function returns 
        ///     what kind of Descriptor this object is, as a string.
        ///     (Example: ClassDescriptor => "Class", EnumDescriptor => "Enum", etc.)
        /// </summary>
        public string GetDescriptorType()
        {
            string descType = GetType().Name;

            if (descType != "Descriptor")
                descType = descType.Replace("Descriptor", "");

            return descType;
        }

        /// <summary>
        ///     Returns a string describing how the tokens of this 
        ///     descriptor should be formatted. Tokens are provided
        ///     in the GetTokens function, defined below.
        /// </summary>
        /// <param name="detailed">
        ///     Indicates if this should be a summary, or a full signature.
        /// </param>
        public virtual string GetSchema(bool detailed = false)
        {
            string schema = "{DescriptorType} {Name}";

            if (detailed)
                schema += " {Tags}";

            return schema;
        }

        /// <summary>
        ///     Returns a dictionary mapping tokens to the values that they should have.
        ///     The tokens provided are mapped into the schema provided in GetSchema.
        /// </summary>
        /// <param name="detailed">
        ///     Indicates if this should be a summary, or a full signature.
        /// </param>
        public virtual Dictionary<string, string> GetTokens(bool detailed = false)
        {
            var tokens = new Dictionary<string, string>();
            tokens.Add("Name", Name);

            string descType = GetDescriptorType();
            tokens.Add("DescriptorType", descType);

            string tags = Util.DescribeTags(Tags);
            if (detailed && tags.Length > 0)
                tokens.Add("Tags", tags);

            return tokens;
        }

        /// <summary>
        ///     Takes the schema provided from GetSchema, the tokens
        ///     from GetTokens, and returns a formatted string that
        ///     describes the signature of this descriptor.
        /// </summary>
        /// <param name="detailed">
        ///     Indicates if this should be a summary, or a full signature.
        /// </param>
        public string Describe(bool detailed = false)
        {
            int search = 0;

            var tokens = GetTokens(detailed);
            string desc = GetSchema(detailed);
            
            while (search < desc.Length)
            {
                int openToken = desc.IndexOf('{', search);
                if (openToken >= 0)
                {
                    int closeToken = desc.IndexOf('}', openToken);
                    if (closeToken >= 0)
                    {
                        string token = desc.Substring(openToken + 1, closeToken - openToken - 1);
                        string value = "";

                        if (tokens.ContainsKey(token))
                            value = tokens[token];

                        desc = desc.Replace('{' + token + '}', value);
                        search = openToken + value.Length;
                    }
                }
            }

            desc = desc.Replace("  ", " ");
            desc = desc.Trim();

            return desc;
        }

        protected static string ExtendDescription(params object[] targets)
        {
            string[] filtered = targets
                .Select(target => target.ToString())
                .Where(target => target.Length > 0)
                .ToArray();

            return string.Join(" ", filtered);
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
    }

    [JsonConverter( typeof(ReflectionConverter) )]
    public sealed class ClassDescriptor : Descriptor
    {
        public string Superclass;
        public ReflectionDatabase Database;
        public DeveloperMemoryTag MemoryCategory;

        public List<MemberDescriptor>   Members;
        public List<PropertyDescriptor> Properties;
        public List<FunctionDescriptor> Functions;
        public List<CallbackDescriptor> Callbacks;
        public List<EventDescriptor>    Events;

        private int inheritLevel = -1;

        public ClassDescriptor()
        {
            Members    = new List<MemberDescriptor>();
            Properties = new List<PropertyDescriptor>();
            Functions  = new List<FunctionDescriptor>();
            Callbacks  = new List<CallbackDescriptor>();
            Events     = new List<EventDescriptor>();
        }

        public int InheritanceLevel
        {
            get
            {
                if (inheritLevel < 0 && Database != null)
                {
                    if (Database.ClassLookup.ContainsKey(Superclass))
                    {
                        ClassDescriptor parentClass = Database.ClassLookup[Superclass];
                        if (parentClass.InheritanceLevel >= 0)
                        {
                            // Set the inheritance level to the parent's level + 1
                            inheritLevel = parentClass.InheritanceLevel + 1;
                        }
                    }
                    else if (Superclass == "<<<ROOT>>>")
                    {
                        // This is the top level class
                        inheritLevel = 0;
                    }
                }

                return inheritLevel;
            }
        }

        public override string GetSchema(bool detailed = false)
        {
            string schema = base.GetSchema();

            if (detailed)
                schema += " : {Superclass} {Tags}";

            return schema;
        }

        public override Dictionary<string, string> GetTokens(bool detailed = false)
        {
            var tokens = base.GetTokens(detailed);

            if (detailed)
                tokens.Add("Superclass", Superclass);

            return tokens;
        }

        public override int CompareTo(object other)
        {
            if (other is ClassDescriptor)
            {
                var otherClass = other as ClassDescriptor;
                if (InheritanceLevel != otherClass.InheritanceLevel)
                {
                    int diff = InheritanceLevel - otherClass.InheritanceLevel;
                    return Math.Sign(diff);
                }
            }

            return base.CompareTo(other);
        }
    }

    public abstract class MemberDescriptor : Descriptor
    {
        public ClassDescriptor Class;
        public MemberType MemberType;

        public override string GetSchema(bool detailed = true)
        {
            string schema = "{DescriptorType} ";

            if (detailed && MemberType != MemberType.Event)
            {
                string typeName;

                if (MemberType == MemberType.Property)
                    typeName = "{ValueType}";
                else
                    typeName = "{ReturnType}";

                schema += typeName + ' ';
            }

            schema += "{ClassName}";

            if (MemberType == MemberType.Function)
                schema += ':';
            else
                schema += '.';

            schema += "{Name}";

            if (detailed)
            {
                if (MemberType != MemberType.Property)
                    schema += "{Parameters}";

                schema += " {Security} {Tags}";
            }

            return schema;
        }

        public override Dictionary<string, string> GetTokens(bool detailed = false)
        {
            var tokens = base.GetTokens(detailed);
            tokens.Add("ClassName", Class.Name);

            return tokens;
        }

        public override int CompareTo(object other)
        {
            if (other is MemberDescriptor)
            {
                var otherDesc = other as MemberDescriptor;

                if (Class != otherDesc.Class)
                {
                    return Class.CompareTo(otherDesc.Class);
                }

                if (MemberType != otherDesc.MemberType)
                {
                    var priority = Util.TypePriority;

                    string thisMT = Util.GetEnumName(MemberType);
                    string otherMT = Util.GetEnumName(otherDesc.MemberType);
                    
                    if (priority.Contains(thisMT) && priority.Contains(otherMT))
                    {
                        return priority.IndexOf(thisMT) - priority.IndexOf(otherMT);
                    }
                }
            }

            return base.CompareTo(other);
        }

        // Returns a list of parameters if the descriptor calling this function has a
        // member named Parameters whose type is List<Parameters>. This is a hack to
        // make it easier to handle parameters in html.
        public List<Parameter> GetParameters()
        {
            try
            {
                Type type = GetType();
                FieldInfo info = type.GetField("Parameters");
                return info.GetValue(this) as List<Parameter>;
            }
            catch
            {
                return null;
            }
        }

        public virtual TypeDescriptor GetResultType()
        {
            throw new NotImplementedException();
        }
    }

    public sealed class PropertyDescriptor : MemberDescriptor
    {
        public string Category;
        public TypeDescriptor ValueType;
        public ReadWriteSecurity Security;
        public Serialization Serialization;

        public override TypeDescriptor GetResultType()
        {
            return ValueType;
        }

        public override Dictionary<string, string> GetTokens(bool detailed = false)
        {
            var tokens = base.GetTokens(detailed);

            if (detailed)
            {
                string valueType = ValueType.ToString();
                tokens.Add("ValueType", valueType);

                string security = Util.DescribeSecurity(Security);
                tokens.Add("Security", security);
            }

            return tokens;
        }
        
        public override int CompareTo(object other)
        {
            if (other is PropertyDescriptor)
            {
                var otherDesc = other as PropertyDescriptor;

                bool thisIsCamel = char.IsLower(Name[0]);
                bool otherIsCamel = char.IsLower(otherDesc.Name[0]);

                // Upcast the comparison if this is a camelCase condition.
                // camelCase members should always appear last in the member type listing.
                if (thisIsCamel != otherIsCamel)
                    return base.CompareTo(other);

                // Compare by categories.
                if (Category != otherDesc.Category)
                {
                    return Category.CompareTo(otherDesc.Category);
                }
            }

            return base.CompareTo(other);
        }
    }

    public sealed class FunctionDescriptor : MemberDescriptor
    {
        public List<Parameter> Parameters;
        public TypeDescriptor ReturnType;
        public SecurityType Security;

        public override TypeDescriptor GetResultType()
        {
            return ReturnType;
        }

        public override Dictionary<string, string> GetTokens(bool detailed = false)
        {
            var tokens = base.GetTokens(detailed);

            if (detailed)
            {
                string returnType = ReturnType.ToString();
                tokens.Add("ReturnType", returnType);

                string parameters = Util.DescribeParameters(Parameters);
                tokens.Add("Parameters", parameters);

                string security = Util.DescribeSecurity(Security);
                tokens.Add("Security", security);
            }

            return tokens;
        }
    }

    public sealed class EventDescriptor : MemberDescriptor
    {
        public List<Parameter> Parameters;
        public SecurityType Security;

        public override Dictionary<string, string> GetTokens(bool detailed = false)
        {
            var tokens = base.GetTokens(detailed);

            if (detailed)
            {
                string parameters = Util.DescribeParameters(Parameters);
                tokens.Add("Parameters", parameters);

                string security = Util.DescribeSecurity(Security);
                tokens.Add("Security", security);
            }

            return tokens;
        }
    }

    public sealed class CallbackDescriptor : MemberDescriptor
    {
        public List<Parameter> Parameters;
        public TypeDescriptor ReturnType;
        public SecurityType Security;

        public override TypeDescriptor GetResultType()
        {
            return ReturnType;
        }

        public override Dictionary<string, string> GetTokens(bool detailed = false)
        {
            var tokens = base.GetTokens(detailed);

            if (detailed)
            {
                string returnType = ReturnType.ToString();
                tokens.Add("ReturnType", returnType);

                string parameters = Util.DescribeParameters(Parameters);
                tokens.Add("Parameters", parameters);

                string security = Util.DescribeSecurity(Security);
                tokens.Add("Security", security);
            }

            return tokens;
        }
    }

    [JsonConverter( typeof(ReflectionConverter) )]
    public sealed class EnumDescriptor : Descriptor
    {
        public List<EnumItemDescriptor> Items;

        public EnumDescriptor()
        {
            Items = new List<EnumItemDescriptor>();
        }
    }

    public sealed class EnumItemDescriptor : Descriptor
    {
        public EnumDescriptor Enum;
        public int Value;

        public override string GetSchema(bool detailed = false)
        {
            string schema = "{DescriptorType} {EnumName}.{Name}";

            if (detailed)
                schema += " : {Value} {Tags}";

            return schema;
        }

        public override Dictionary<string, string> GetTokens(bool detailed = false)
        {
            var tokens = base.GetTokens(detailed);

            string enumName = Enum.Name;
            tokens.Add("EnumName", enumName);

            if (detailed)
            {
                string value = Value.ToString();
                tokens.Add("Value", value);
            }

            return tokens;
        }

        public override int CompareTo(object other)
        {
            if (other is EnumItemDescriptor)
            {
                var otherDesc = other as EnumItemDescriptor;

                if (Enum != otherDesc.Enum)
                    return Enum.CompareTo(otherDesc.Enum);

                return Value - otherDesc.Value;
            }

            return base.CompareTo(other);
        }
    }

    public class ReflectionDatabase
    {
        public int Version;

        public List<ClassDescriptor> Classes;
        public List<EnumDescriptor> Enums;

        public Dictionary<string, ClassDescriptor> ClassLookup;

        public static ReflectionDatabase Load(string jsonApiDump)
        {
            var result = JsonConvert.DeserializeObject<ReflectionDatabase>(jsonApiDump);
            result.ClassLookup = new Dictionary<string, ClassDescriptor>();

            foreach (ClassDescriptor classDesc in result.Classes)
            {
                result.ClassLookup.Add(classDesc.Name, classDesc);
                classDesc.Database = result;
            }

            return result;
        }
    }
}