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
        public TypeCategory Category;

        public bool IsReturnType = false;
        public override string ToString() => GetSignature();

        public string GetSignature(bool ignoreReturnType = false)
        {
            string result;

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
