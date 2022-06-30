using System.Collections.Generic;

namespace RobloxApiDumpTool
{
    public sealed class EnumItemDescriptor : Descriptor
    {
        public List<string> LegacyNames = new List<string>();
        public EnumDescriptor Enum;
        public int Value;

        public override string GetSchema(bool detailed = false)
        {
            string schema = "{EnumName}.{Name}";

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
}
