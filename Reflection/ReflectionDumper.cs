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

        private void write(params object[] list)
        {
            string result = string.Join(" ", list);
            write(result);
        }

        private void write(object text) => buffer.Append(text);
        private void nextLine() => write(Util.NewLine);
        private void tab() => write('\t');

        public string Run()
        {
            buffer.Clear();

            foreach (ClassDescriptor classDesc in api.Classes)
            {
                write(classDesc.Signature);
                nextLine();

                foreach (MemberDescriptor memberDesc in classDesc.Members)
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

                foreach (EnumItemDescriptor itemDesc in enumDesc.Items)
                {
                    tab();
                    write(itemDesc.Signature);
                    nextLine();
                }
            }

            return buffer.ToString();
        }
    }
}
