using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Roblox.Reflection
{
    public class Descriptor : IComparable
    {
        public string Name;
        public List<string> Tags;

        public string Summary => Describe(false);
        public string Signature => Describe(true);
        public override string ToString() => Summary;

        public Descriptor()
        {
            Tags = new List<string>();
        }

        protected string PrependDescriptorType(string desc)
        {
            string descType = GetType().Name;

            if (descType != "Descriptor")
                descType = descType.Replace("Descriptor", "");

            return descType + ' ' + desc;
        }

        public virtual string Describe(bool detailed = false)
        {
            return PrependDescriptorType(Name);
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
        public DeveloperMemoryTag MemoryCategory;

        public List<MemberDescriptor>   Members;
        public List<PropertyDescriptor> Properties;
        public List<FunctionDescriptor> Functions;
        public List<CallbackDescriptor> Callbacks;
        public List<EventDescriptor>    Events;

        public ClassDescriptor()
        {
            Members    = new List<MemberDescriptor>();
            Properties = new List<PropertyDescriptor>();
            Functions  = new List<FunctionDescriptor>();
            Callbacks  = new List<CallbackDescriptor>();
            Events     = new List<EventDescriptor>();
        }

        public override string Describe(bool detailed = false)
        {
            string result = base.Describe();

            if (detailed)
            {
                string tags = Util.DescribeTags(Tags);
                result = ExtendDescription(result, ":", Superclass, tags);
            }

            return result;
        }
    }

    public abstract class MemberDescriptor : Descriptor
    {
        public ClassDescriptor Class;
        public MemberType MemberType;

        public override string Describe(bool detailed = false)
        {
            string memberType = Util.GetEnumName(MemberType);
            string result = Name;

            if (Class != null)
            {
                char divider = (MemberType == MemberType.Function ? ':' : '.');
                result = Class.Name + divider + Name;
            }

            return result;
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
    }

    public sealed class PropertyDescriptor : MemberDescriptor
    {
        public string Category;
        public TypeDescriptor ValueType;
        public ReadWriteSecurity Security;
        public Serialization Serialization;

        public override string Describe(bool detailed = false)
        {
            string desc = base.Describe();

            if (detailed)
            {
                string security = Util.DescribeSecurity(Security);
                string tags = Util.DescribeTags(Tags);
                desc = ExtendDescription(ValueType, desc, security, tags);
            }

            return PrependDescriptorType(desc);
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

        public override string Describe(bool detailed = false)
        {
            string desc = base.Describe();

            if (detailed)
            {
                string returnType = ReturnType.ToString();
                string parameters = Util.DescribeParameters(Parameters);
                string security = Util.DescribeSecurity(Security);
                string tags = Util.DescribeTags(Tags);

                desc = ExtendDescription(returnType, desc + parameters, security, tags);
            }

            return PrependDescriptorType(desc);
        }
    }

    public sealed class EventDescriptor : MemberDescriptor
    {
        public List<Parameter> Parameters;
        public SecurityType Security;

        public override string Describe(bool detailed = false)
        {
            string desc = base.Describe();

            if (detailed)
            {
                string parameters = Util.DescribeParameters(Parameters);
                string security = Util.DescribeSecurity(Security);
                string tags = Util.DescribeTags(Tags);

                desc = ExtendDescription(desc + parameters, security, tags);
            }

            return PrependDescriptorType(desc);
        }
    }

    public sealed class CallbackDescriptor : MemberDescriptor
    {
        public List<Parameter> Parameters;
        public TypeDescriptor ReturnType;
        public SecurityType Security;

        public override string Describe(bool detailed = false)
        {
            string desc = base.Describe();

            if (detailed)
            {
                string returnType = ReturnType.ToString();
                string parameters = Util.DescribeParameters(Parameters);
                string security = Util.DescribeSecurity(Security);
                string tags = Util.DescribeTags(Tags);

                desc = ExtendDescription(returnType, desc + parameters, security, tags);
            }

            return PrependDescriptorType(desc);
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

        public override string Describe(bool detailed = false)
        {
            string result = base.Describe();

            if (detailed)
            {
                string tags = Util.DescribeTags(Tags);
                result = ExtendDescription(result, tags);
            }

            return result;
        }
    }

    public sealed class EnumItemDescriptor : Descriptor
    {
        public EnumDescriptor Enum;
        public int Value;

        public override string Describe(bool detailed = false)
        {
            string result;
            if (Enum != null)
                result = Enum.Name + '.' + Name;
            else
                result = Name;

            if (detailed)
            {
                string tags = Util.DescribeTags(Tags);
                result = ExtendDescription(result, ":", Value.ToString(), tags);
            }

            return PrependDescriptorType(result);
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

        public static ReflectionDatabase Load(string jsonApiDump)
        {
            return JsonConvert.DeserializeObject<ReflectionDatabase>(jsonApiDump);
        }
    }
}