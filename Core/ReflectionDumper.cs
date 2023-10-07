using System;
using System.Collections.Generic;
using System.Text;

namespace RobloxApiDumpTool
{
    public class ReflectionDumper
    {
        public ReflectionDatabase Database { get; private set; }
        
        public delegate void SignatureWriter(ReflectionDumper buffer, Descriptor desc, int numTabs = 0);
        public delegate string DumpPostProcesser(string result, string workDir = "");

        public readonly StringBuilder Builder = new StringBuilder();

        private static readonly Descriptor.HtmlConfig htmlConfig = new Descriptor.HtmlConfig
        {
            TagType = "div",
            Detailed = true,
            DiffMode = false,
        };

        public ReflectionDumper(ReflectionDatabase database = null)
        {
            Database = database;
        }

        public string ExportResults(DumpPostProcesser postProcess = null)
        {
            string result = Builder.ToString();
            string post = postProcess?.Invoke(result);

            if (post != null)
                result = post;

            return result;
        }

        private static List<T> Sorted<T>(List<T> list)
        {
            list.Sort();
            return list;
        }
        
        public void Write(object text)
        {
            Builder.Append(text);
        }

        public void NextLine(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Write("\r\n");
            }
        }

        public void Tab(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Write('\t');
            }
        }

        public static SignatureWriter DumpUsingTxt = (buffer, desc, numTabs) =>
        {
            buffer.Tab(numTabs);
            buffer.Write(desc.Signature);
        };

        public static SignatureWriter DumpUsingHtml = (buffer, desc, numTabs) =>
        {
            var html = new ReflectionHtml();
            desc.WriteHtml(html);
            html.WriteTo(buffer.Builder);
        };

        public string DumpApi(SignatureWriter WriteSignature, DumpPostProcesser postProcess = null)
        {
            if (Database == null)
                throw new Exception("Cannot Dump API without a ReflectionDatabase provided.");

            Builder.Clear();

            foreach (ClassDescriptor classDesc in Database.Classes.Values)
            {
                WriteSignature(this, classDesc, 0);
                NextLine();

                foreach (MemberDescriptor memberDesc in Sorted(classDesc.Members))
                {
                    WriteSignature(this, memberDesc, 1);
                    NextLine();
                }
            }

            foreach (EnumDescriptor enumDesc in Database.Enums.Values)
            {
                WriteSignature(this, enumDesc, 0);
                NextLine();

                foreach (EnumItemDescriptor itemDesc in Sorted(enumDesc.Items))
                {
                    WriteSignature(this, itemDesc, 1);
                    NextLine();
                }
            }

            return ExportResults(postProcess);
        }
    }
}