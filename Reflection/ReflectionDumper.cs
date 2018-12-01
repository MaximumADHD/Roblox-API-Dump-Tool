using System.Collections.Generic;
using System.Text;

namespace Roblox.Reflection
{
    class ReflectionDumper
    {
        private ReflectionDatabase api;
        private StringBuilder buffer;

        public ReflectionDumper(ReflectionDatabase database)
        {
            api = database;
            buffer = new StringBuilder();
        }

        private static List<T> sorted<T>(List<T> list)
        {
            list.Sort();
            return list;
        }

        private void write(params object[] parts)
        {
            string text = string.Join(" ", parts);
            buffer.Append(text);
        }

        private void nextLine()
        {
            write(Util.NewLine);
        }

        private void tab()
        {
            write('\t');
        }

        private void openHtmlTag(int stack, string tagName, string attributes = "")
        {
            for (int i = 0; i < stack; i++)
                tab();

            write('<' + tagName);

            if (attributes.Length > 0)
                write(" " + attributes);

            write('>');
        }

        private void closeHtmlTag(int stack, string tagName)
        {
            for (int i = 0; i < stack; i++)
                tab();

            write("</" + tagName + ">");
        }

        public string DumpTxt()
        {
            buffer.Clear();

            foreach (ClassDescriptor classDesc in api.Classes)
            {
                write(classDesc.Signature);
                nextLine();

                foreach (MemberDescriptor memberDesc in sorted(classDesc.Members))
                {
                    tab();
                    write(memberDesc.Signature);
                    nextLine();
                }
            }

            foreach (EnumDescriptor enumDesc in api.Enums)
            {
                write(enumDesc.Signature);
                nextLine();

                foreach (EnumItemDescriptor itemDesc in sorted(enumDesc.Items))
                {
                    tab();
                    write(itemDesc.Signature);
                    nextLine();
                }
            }

            return buffer.ToString();
        }

        public string DumpHtml()
        {
            // TO-DO
            return "";
        }
    }
}