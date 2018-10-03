using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Roblox.Reflection
{
    public class Descriptor
    {
        public string Name;
        public List<string> Tags;
        public override string ToString() => Describe();

        protected static string ExtendDescription(params string[] targets)
        {
            string[] filtered = targets.Where(target => target.Length > 0).ToArray();
            return string.Join(" ", filtered);
        }

        public virtual string Describe(bool detailed = false)
        {
            return GetType().Name.Replace("Descriptor", "") + " " + Name;
        }

        public string Signature => Describe(true);
        public string Summary => Describe(false);

        public Descriptor()
        {
            Tags = new List<string>();
        }
    }

    [JsonConverter( typeof(ReflectionConverter) )]
    public class ClassDescriptor : Descriptor
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
                string tags = Util.GetTagSignature(Tags);
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

        protected string PrependMemberType(string desc)
        {
            return Util.GetEnumName(MemberType) + ' ' + desc;
        }
    }

    public class PropertyDescriptor : MemberDescriptor
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
                string valueType = ValueType.ToString();
                string security = Util.GetSecuritySignature(Security);
                string tags = Util.GetTagSignature(Tags);
                desc = ExtendDescription(valueType, desc, security, tags);
            }

            return PrependMemberType(desc);
        }
    }

    public class FunctionDescriptor : MemberDescriptor
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
                string parameters = Util.GetParamSignature(Parameters);
                string security = Util.GetSecuritySignature(Security);
                string tags = Util.GetTagSignature(Tags);
                desc = ExtendDescription(returnType, desc + parameters, security, tags);
            }

            return PrependMemberType(desc);
        }
    }

    public class EventDescriptor : MemberDescriptor
    {
        public List<Parameter> Parameters;
        public SecurityType Security;

        public override string Describe(bool detailed = false)
        {
            string desc = base.Describe();

            if (detailed)
            {
                string parameters = Util.GetParamSignature(Parameters);
                string security = Util.GetSecuritySignature(Security);
                string tags = Util.GetTagSignature(Tags);
                desc = ExtendDescription(desc + parameters, security, tags);
            }

            return PrependMemberType(desc);
        }
    }

    public class CallbackDescriptor : MemberDescriptor
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
                string parameters = Util.GetParamSignature(Parameters);
                string security = Util.GetSecuritySignature(Security);
                string tags = Util.GetTagSignature(Tags);
                desc = ExtendDescription(returnType, desc + parameters, security, tags);
            }

            return PrependMemberType(desc);
        }
    }

    [JsonConverter( typeof(ReflectionConverter) )]
    public class EnumDescriptor : Descriptor
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
                string tags = Util.GetTagSignature(Tags);
                result = ExtendDescription(result, tags);
            }

            return result;
        }
    }

    public class EnumItemDescriptor : Descriptor
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
                string tags = Util.GetTagSignature(Tags);
                result = ExtendDescription(result, ":", Value.ToString(), tags);
            }

            return "EnumItem " + result;
        }
    }

    public class ReflectionDatabase
    {
        public int Version;
        public List<ClassDescriptor> Classes;
        public List<EnumDescriptor> Enums;

        public static ReflectionDatabase Load(string jsonApiDump)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.All;

            ReflectionDatabase api = JsonConvert.DeserializeObject<ReflectionDatabase>(jsonApiDump);
            return api;
        }
    }
}
