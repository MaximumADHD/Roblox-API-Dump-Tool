using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Roblox.Reflection
{
    public class Descriptor
    {
        public string Name;
        public List<string> Tags;
        public override string ToString()
        {
            return GetType().Name.Replace("Descriptor", "") + " " + Name;
        }

        public Descriptor()
        {
            Tags = new List<string>();
        }
    }

    [ JsonConverter( typeof(ReflectionClassReader) ) ]
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
    }

    public abstract class MemberDescriptor : Descriptor
    {
        public MemberType MemberType;
        public ClassDescriptor ParentClass;

        protected static string ExtendDescription(params string[] targets)
        {
            List<string> join = new List<string>();
            foreach (string target in targets)
            {
                if (target.Length > 0)
                {
                    join.Add(target);
                }
            }
            return string.Join(" ", join.ToArray());
        }

        public virtual string Describe(bool detailed = false)
        {
            string result = Name;

            if (ParentClass != null)
            {
                char divider = (MemberType == MemberType.Function ? ':' : '.');
                result = ParentClass.Name + divider + Name;
            }

            return result;
        }

        public override string ToString()
        {
            return Describe(false);
        }
    }

    public class PropertyDescriptor : MemberDescriptor
    {
        public string Category;
        public RobloxType ValueType;
        public ReadWriteSecurity Security;
        public Serialization Serialization;

        public override string Describe(bool detailed = false)
        {
            string desc = base.Describe(detailed);

            if (detailed)
            {
                string valueType = ValueType.ToString();
                string security = Util.GetSecuritySignature(Security);
                string tags = Util.GetTagSignature(Tags);
                desc = ExtendDescription(valueType, desc, security, tags);
            }

            return desc;
        }
    }

    public class FunctionDescriptor : MemberDescriptor
    {
        public List<Parameter> Parameters;
        public RobloxType ReturnType;
        public SecurityType Security;

        public override string Describe(bool detailed = false)
        {
            string desc = base.Describe(detailed);

            if (detailed)
            {
                string returnType = ReturnType.ToString();
                string paramz = Util.GetParamSignature(Parameters);
                string security = Util.GetSecuritySignature(Security);
                string tags = Util.GetTagSignature(Tags);
                desc = ExtendDescription(returnType, desc + paramz, security, tags);
            }

            return desc;
        }
    }

    public class EventDescriptor : MemberDescriptor
    {
        public List<Parameter> Parameters;
        public SecurityType Security;

        public override string Describe(bool detailed = false)
        {
            string desc = base.Describe(detailed);

            if (detailed)
            {
                string paramz = Util.GetParamSignature(Parameters);
                string security = Util.GetSecuritySignature(Security);
                string tags = Util.GetTagSignature(Tags);
                desc = ExtendDescription(desc + paramz, security, tags);
            }

            return desc;
        }
    }

    public class CallbackDescriptor : MemberDescriptor
    {
        public List<Parameter> Parameters;
        public RobloxType ReturnType;
        public SecurityType Security;
        public override string Describe(bool detailed = false)
        {
            string desc = base.Describe(detailed);

            if (detailed)
            {
                string returnType = ReturnType.ToString();
                string paramz = Util.GetParamSignature(Parameters);
                string security = Util.GetSecuritySignature(Security);
                string tags = Util.GetTagSignature(Tags);
                desc = ExtendDescription(returnType, desc + paramz, security, tags);
            }

            return desc;
        }
    }

    public class EnumDescriptor : Descriptor
    {
        public List<EnumItemDescriptor> Items;
    }

    public class EnumItemDescriptor : Descriptor
    {
        public int Value;
        public EnumDescriptor Enum;
        public override string ToString() => Name;
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
