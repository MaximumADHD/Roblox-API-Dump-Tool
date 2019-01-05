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
        public Tags Tags = new Tags();

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
                if (openToken >= 0)
                {
                    int closeToken = desc.IndexOf('}', openToken);
                    if (closeToken >= 0)
                    {
                        string token = desc.Substring(openToken + 1, closeToken - openToken - 1);
                        string value = "";

                        if (tokens.ContainsKey(token))
                            value = tokens[token].ToString();

                        desc = desc.Replace('{' + token + '}', value);
                        search = openToken + value.Length;
                    }
                }
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
    }

    [JsonConverter( typeof(ReflectionDeserializer) )]
    public sealed class ClassDescriptor : Descriptor
    {
        public string Superclass;
        public MemoryTag MemoryCategory;
        public ReflectionDatabase Database;
        
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

        public override Dictionary<string, object> GetTokens(bool detailed = false)
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

        public override Dictionary<string, object> GetTokens(bool detailed = false)
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
                    var priority = ReflectionDiffer.TypePriority;

                    string thisMT = Program.GetEnumName(MemberType);
                    string otherMT = Program.GetEnumName(otherDesc.MemberType);
                    
                    if (priority.Contains(thisMT) && priority.Contains(otherMT))
                    {
                        return priority.IndexOf(thisMT) - priority.IndexOf(otherMT);
                    }
                }
            }

            return base.CompareTo(other);
        }

        // Returns a Parameters object if the descriptor calling this function has a
        // member named Parameters whose type is Parameters. This is a hack to
        // make it easier to handle parameters in html.
        public Parameters GetParameters()
        {
            try
            {
                Type type = GetType();
                FieldInfo info = type.GetField("Parameters");
                return info.GetValue(this) as Parameters;
            }
            catch
            {
                return null;
            }
        }

        // Returns a ReflectionType object if the descriptor calling 
        // this function has a field whose type is ReflectionType.
        // This is a hack to make type handling easier in html.
        public ReflectionType GetResultType()
        {
            Type type = GetType();

            foreach (FieldInfo info in type.GetFields())
            {
                if (info.FieldType == typeof(ReflectionType))
                {
                    var result = info.GetValue(this);
                    return result as ReflectionType;
                }
            }

            return null;
        }
    }

    public sealed class PropertyDescriptor : MemberDescriptor
    {
        public string Category;
        public ReflectionType ValueType;
        public ReadWriteSecurity Security;
        public Serialization Serialization;
        
        public override Dictionary<string, object> GetTokens(bool detailed = false)
        {
            var tokens = base.GetTokens(detailed);

            if (detailed)
            {
                tokens.Add("ValueType", ValueType);
                tokens.Add("Security", Security);
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
        public Security Security;
        public Parameters Parameters;
        public ReflectionType ReturnType;
        
        public override Dictionary<string, object> GetTokens(bool detailed = false)
        {
            var tokens = base.GetTokens(detailed);

            if (detailed)
            {
                tokens.Add("Security", Security);
                tokens.Add("Parameters", Parameters);
                tokens.Add("ReturnType", ReturnType);
            }

            return tokens;
        }
    }

    public sealed class EventDescriptor : MemberDescriptor
    {
        public Security Security;
        public Parameters Parameters;
        
        public override Dictionary<string, object> GetTokens(bool detailed = false)
        {
            var tokens = base.GetTokens(detailed);

            if (detailed)
            {
                tokens.Add("Security", Security);
                tokens.Add("Parameters", Parameters);
            }

            return tokens;
        }
    }

    public sealed class CallbackDescriptor : MemberDescriptor
    {
        public Security Security;
        public Parameters Parameters;
        public ReflectionType ReturnType;
        
        public override Dictionary<string, object> GetTokens(bool detailed = false)
        {
            var tokens = base.GetTokens(detailed);

            if (detailed)
            {
                tokens.Add("Security", Security);
                tokens.Add("Parameters", Parameters);
                tokens.Add("ReturnType", ReturnType);
            }

            return tokens;
        }
    }

    [JsonConverter( typeof(ReflectionDeserializer) )]
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

        public override Dictionary<string, object> GetTokens(bool detailed = false)
        {
            var tokens = base.GetTokens(detailed);
            tokens.Add("EnumName", Enum.Name);

            if (detailed)
                tokens.Add("Value", Value);

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