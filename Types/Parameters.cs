using System.Collections.Generic;
using System.Linq;

namespace Roblox.Reflection
{
    public struct Parameter
    {
        private const string quote = "\"";

        public LuaType Type;
        public string Name;
        public string Default;

        public override string ToString()
        {
            string result = $"{Type} {Name}";
            string category = $"{Type.Category}";

            if (Default != null)
            {
                if ((Type.Name == "string" || category == "Enum"))
                    if (!Default.StartsWith(quote) && !Default.EndsWith(quote))
                        Default = quote + Default + quote;

                if (Type.Category == TypeCategory.DataType && Type.Name != "Function")
                    Default = $"{Type.Name}.new()";

                if (Default.Length == 0)
                    return result;

                result += " = " + Default;
            }
            
            return result;
        }

        public void WriteHtml(ReflectionDumper buffer, int numTabs = 0)
        {
            buffer.OpenClassTag("Parameter", numTabs);
            buffer.NextLine();

            // Write Type
            Type.WriteHtml(buffer, numTabs + 1);

            // Write Name
            string nameLbl = "ParamName";
            if (Default != null)
                nameLbl += " default";

            buffer.WriteElement(nameLbl, Name, numTabs + 1);

            // Write Default
            if (Default != null)
            {
                string typeLbl = Type.GetSignature();
                string typeName;

                if (typeLbl.Contains("<") && typeLbl.EndsWith(">"))
                    typeName = $"{Type.Category}";
                else
                    typeName = Type.Name;

                if (Type.Category == TypeCategory.DataType && typeName != "Function")
                {
                    buffer.WriteElement("ClassName Type", typeName, numTabs + 1);
                    buffer.WriteElement("Name", "new", numTabs + 1);
                    buffer.WriteElement("Parameters", null, numTabs + 1);
                }
                else
                {
                    if (Type.Category == TypeCategory.Enum)
                        typeName = "String";

                    buffer.WriteElement("ParamDefault " + typeName, Default, numTabs + 1);
                }
            }

            buffer.CloseClassTag(numTabs);
        }
    }

    public class Parameters : List<Parameter>
    {
        public override string ToString()
        {
            string[] parameters = this.Select(param => param.ToString()).ToArray();
            return '(' + string.Join(", ", parameters) + ')';
        }

        public void WriteHtml(ReflectionDumper buffer, int numTabs = 0, bool diffMode = false)
        {
            string paramsTag = "Parameters";

            if (diffMode)
                paramsTag += " change";

            int closingTabs = 0;
            buffer.OpenClassTag(paramsTag, numTabs);

            if (Count > 0)
            {
                buffer.NextLine();

                foreach (Parameter parameter in this)
                    parameter.WriteHtml(buffer, numTabs + 1);

                closingTabs = numTabs;
            }

            buffer.CloseClassTag(closingTabs);
        }
    }
}