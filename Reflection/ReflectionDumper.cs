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

        private void write(params object[] list)
        {
            string result = string.Join(" ", list);
            write(result);
        }

        private void write(object text) => buffer.Append(text);
        private void space() => write(' ');
        private void nextLine() => write(Util.NewLine);
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
                space();
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
                space();
                tag(enumDesc);
                nextLine();

                foreach (EnumItemDescriptor itemDesc in enumDesc.Items)
                {
                    tab();
                    write("EnumItem", enumDesc.Name + '.' + itemDesc.ToString(), ':', itemDesc.Value);
                    space();
                    tag(itemDesc);
                    nextLine();
                }
            }

            return buffer.ToString();
        }
    }
}
