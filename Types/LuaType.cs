using System.Collections.Generic;

namespace RobloxApiDumpTool
{
    public enum TypeCategory
    {
        Primitive,
        Class,
        Enum,
        Group,
        DataType
    }

    public class LuaType
    {
        public string Name;
        public string SourceName;
        public TypeCategory Category;

        public bool IsReturnType = false;
        public override string ToString() => GetSignature();

        private static IReadOnlyDictionary<string, string> LuauTypes = new Dictionary<string, string>()
        {
            { "Dictionary", "{ [string]: any }" },
            { "Map", "{ [any]: any }" },
            { "Array", "{ any }" },
            { "Variant", "any" },

            { "Objects", "{ Instance }" },
            { "Tuple", "...any" },
            { "Function", "((...any) -> ...any)" },

            { "null", "()" },
            { "void", "()" },

            { "int", "number" },
            { "int64", "number" },
            { "float", "number" },
            { "double", "number" },

            { "bool", "boolean" },
        };

        public string GetSignature(bool ignoreReturnType = false)
        {
            string result;
            SourceName = Name;

            bool optional = false;

            if (Name.EndsWith("?"))
            {
                optional = true;
                Name = Name.Replace("?", "");
            }

            if (LuauTypes.ContainsKey(Name))
                Name = LuauTypes[Name];

            if (optional)
                Name += '?';

            if (Category != TypeCategory.Enum)
                result = Name;
            else
                result = $"{Category}.{Name}";

            if (IsReturnType && !ignoreReturnType)
                result = "-> " + result;

            return result;
        }

        public void WriteHtml(ReflectionDumper buffer, int numTabs = 0)
        {
            string typeVal = GetSignature(true);

            if (typeVal.StartsWith("Enum."))
            {
                buffer.OpenClassTag("EnumName Type", numTabs);
                buffer.Write("Enum");
                buffer.CloseClassTag();

                typeVal = typeVal.Substring(5);
            }

            string typeTag = "Type";

            if (IsReturnType)
                typeTag += " WithReturn";

            buffer.OpenClassTag(typeTag, numTabs);
            buffer.Write(typeVal);
            buffer.CloseClassTag();
        }
    }
}
