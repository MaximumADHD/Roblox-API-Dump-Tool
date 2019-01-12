namespace Roblox.Reflection
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
        public override string ToString() => GetSignature();

        public string GetSignature()
        {
            string result;

            if (Name == "Instance" || Category != TypeCategory.Class && Category != TypeCategory.Enum)
                result = Name;
            else
                result = Program.GetEnumName(Category) + '<' + Name + '>';

            return result;
        }

        public void WriteHtml(ReflectionDumper buffer, int numTabs = 0)
        {
            string typeVal = GetSignature();
            buffer.OpenClassTag("Type", numTabs);

            if (typeVal.Contains("<") && typeVal.EndsWith(">"))
            {
                string category = Program.GetEnumName(Category);
                buffer.Write(category);
                buffer.CloseClassTag();

                buffer.OpenClassTag("InnerType", numTabs);
                buffer.Write(Name);
            }
            else
            {
                buffer.Write(typeVal);
            }

            buffer.CloseClassTag();
        }
    }
}
