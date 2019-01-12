using System;
using System.Collections.Generic;
using System.Text;

namespace Roblox.Reflection
{
    public class ReflectionDumper
    {
        public ReflectionDatabase Database { get; private set; }
        private StringBuilder buffer = new StringBuilder();

        public delegate void SignatureWriter(ReflectionDumper buffer, Descriptor desc, int numTabs = 0);
        public delegate string DumpPostProcesser(string result);

        public ReflectionDumper(ReflectionDatabase database = null)
        {
            Database = database;
        }

        private static List<T> sorted<T>(List<T> list)
        {
            list.Sort();
            return list;
        }
        
        public void Write(object text)
        {
            buffer.Append(text);
        }

        public void NextLine()
        {
            Write("\r\n");
        }

        public void Tab(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Write('\t');
            }
        }

        public void OpenHtmlTag(string tagName, string attributes = "", int numTabs = 0)
        {
            Tab(numTabs);
            Write('<' + tagName);

            if (attributes.Length > 0)
                Write(" " + attributes);

            Write('>');
        }

        public void CloseHtmlTag(string tagName, int numTabs = 0)
        {
            Tab(numTabs);
            Write("</" + tagName + ">");
        }

        public void OpenClassTag(string spanClass, int numTabs = 0, string tagType = "span")
        {
            string attributes = "class=\"" + spanClass + '"';
            Tab(numTabs);
            OpenHtmlTag(tagType, attributes);
        }

        public void CloseClassTag(int numTabs = 0, string tagType = "span")
        {
            CloseHtmlTag(tagType, numTabs);
            NextLine();
        }

        public static SignatureWriter DumpUsingTxt = (buffer, desc, numTabs) =>
        {
            buffer.Tab(numTabs);
            buffer.Write(desc.Signature);
        };

        public static SignatureWriter DumpUsingHtml = (buffer, desc, numTabs) =>
        {
            desc.WriteHtml(buffer, 0);
        };

        public string DumpApi(SignatureWriter WriteSignature, DumpPostProcesser postProcess = null)
        {
            if (Database == null)
                throw new Exception("Cannot Dump API without a ReflectionDatabase provided.");

            buffer.Clear();

            foreach (ClassDescriptor classDesc in Database.Classes.Values)
            {
                WriteSignature(this, classDesc, 0);
                NextLine();

                foreach (MemberDescriptor memberDesc in sorted(classDesc.Members))
                {
                    WriteSignature(this, memberDesc, 1);
                    NextLine();
                }
            }

            foreach (EnumDescriptor enumDesc in Database.Enums.Values)
            {
                WriteSignature(this, enumDesc, 0);
                NextLine();

                foreach (EnumItemDescriptor itemDesc in sorted(enumDesc.Items))
                {
                    WriteSignature(this, itemDesc, 1);
                    NextLine();
                }
            }

            string result = buffer.ToString();

            string post = postProcess?.Invoke(result);
            if (post != null)
                result = post;
 
            return result;
        }

        public string GetBuffer()
        {
            return buffer.ToString();
        }
    }
}