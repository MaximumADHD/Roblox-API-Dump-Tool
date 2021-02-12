using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Roblox.Reflection
{
    public enum MemberType
    {
        Property,
        Function,
        Event,
        Callback
    }

    public abstract class MemberDescriptor : Descriptor
    {
        public ClassDescriptor Class;
        public MemberType MemberType;
        public ThreadSafety ThreadSafety = ThreadSafetyType.Unknown;

        public override string GetSchema(bool detailed = true)
        {
            return "{ClassName}.{Name}";
        }

        public override Dictionary<string, object> GetTokens(bool detailed = false)
        {
            var tokens = base.GetTokens(detailed);
            tokens.Add("ClassName", Class.Name);
            tokens.Add("ThreadSafety", ThreadSafety.Describe(MemberType));

            Type type = GetType();

            foreach (FieldInfo field in type.GetFields())
            {
                if (field.DeclaringType == type)
                {
                    object value = field.GetValue(this);
                    tokens.Add(field.Name, value);
                }
            }

            return tokens;
        }

        public override int CompareTo(object other)
        {
            if (!(other is MemberDescriptor otherDesc))
                return base.CompareTo(other);

            if (Class != otherDesc.Class)
                return Class.CompareTo(otherDesc.Class);

            return base.CompareTo(other);
        }
    }
}