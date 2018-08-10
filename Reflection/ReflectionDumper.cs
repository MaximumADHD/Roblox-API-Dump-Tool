using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private void write(object text)
        {
            buffer.Append(text);
        }

        private void write(params object[] list)
        {
            string result = string.Join(" ", list);
            write(result);
        }

        private void space() => write(' ');
        private void nextLine() => write("\r\n");
        private void tab() => write('\t');

        private void tag(Descriptor desc)
        {
            string tags = Util.GetTagSignature(desc.Tags);
            write(tags);
        }

        public string Run()
        {
            buffer.Clear();

            foreach (ClassDescriptor classDesc in api.Classes)
            {
                write(classDesc, ':', classDesc.Superclass);
                tag(classDesc);
                nextLine();

                foreach (MemberDescriptor memberDesc in classDesc.Members)
                {
                    string memberType = Util.GetEnumName(memberDesc.MemberType);
                    tab();
                    write(memberType);
                    space();
                    write(memberDesc.Describe(true));
                    nextLine();
                }
            }

            foreach (EnumDescriptor enumDesc in api.Enums)
            {
                write(enumDesc);
                tag(enumDesc);
                nextLine();

                foreach (EnumItemDescriptor itemDesc in enumDesc.Items)
                {
                    tab();
                    write(itemDesc.ToString(), ':', itemDesc.Value);
                    nextLine();
                }
            }

            return buffer.ToString();
        }
    }
}
