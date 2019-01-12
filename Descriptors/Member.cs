using System;
using System.Collections.Generic;
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
            if (other is MemberDescriptor)
            {
                var otherDesc = other as MemberDescriptor;

                if (Class != otherDesc.Class)
                    return Class.CompareTo(otherDesc.Class);

                if (MemberType != otherDesc.MemberType)
                {
                    var priority = ReflectionDatabase.TypePriority;

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
    }
}