using System;
using System.Collections.Generic;
using System.Text;

namespace Roblox.Reflection
{
    class ReflectionDumper
    {
        public bool HtmlDiffMode = false;
        public bool HtmlMarkDeprecated = true;
        public bool HtmlDumpUsingDetail = true;
        public string HtmlDescriptorTagType = "div";

        private ReflectionDatabase api;
        private StringBuilder buffer;

        public ReflectionDumper(ReflectionDatabase database = null)
        {
            api = database;
            buffer = new StringBuilder();
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

        public void OpenSpanTag(string spanClass, int numTabs = 0, string tagType = "span")
        {
            string attributes = "class=\"" + spanClass + '"';
            Tab(numTabs);
            OpenHtmlTag(tagType, attributes);
        }

        public void WriteTypeElement(ReflectionType type, int numTabs = 0)
        {
            string typeVal = type.ToString();

            if (typeVal.Contains("<") && typeVal.EndsWith(">"))
            {
                string category = Program.GetEnumName(type.Category);
                OpenSpanTag("Type", numTabs);
                Write(category);

                CloseHtmlTag("span");
                NextLine();

                OpenSpanTag("InnerType", numTabs);
                Write(type.Name);

                CloseHtmlTag("span");
                NextLine();
            }
            else
            {
                OpenSpanTag("Type", numTabs);
                Write(type);
                CloseHtmlTag("span");
            }
        }

        public void WriteParametersElement(Parameters parameters, int numTabs = 0, bool change = false)
        {
            string paramsTag = "Parameters";
            if (change)
                paramsTag += " change";

            OpenSpanTag(paramsTag, numTabs);

            if (parameters.Count > 0)
                NextLine();

            for (int i = 0; i < parameters.Count; i++)
            {
                Parameter param = parameters[i];
                OpenSpanTag("Parameter", numTabs + 1);
                NextLine();

                // Write Type
                WriteTypeElement(param.Type, numTabs + 2);
                NextLine();

                // Write Name
                string nameLbl = "ParamName";

                if (param.Default != null)
                    nameLbl += " default";

                OpenSpanTag(nameLbl, numTabs + 2);
                Write(param.Name);

                CloseHtmlTag("span");
                NextLine();

                // Write Default
                if (param.Default != null)
                {
                    OpenSpanTag("ParamDefault " + param.Type.Name, numTabs + 2);
                    Write(param.Default);

                    CloseHtmlTag("span");
                    NextLine();
                }

                CloseHtmlTag("span", numTabs + 1);
                NextLine();
            }

            CloseHtmlTag("span", parameters.Count > 0 ? numTabs : 0);
            NextLine();
        }

        public void WriteTagElements(Tags tags, int numTabs = 0)
        {
            foreach (string tag in tags)
            {
                OpenSpanTag("Tag", numTabs);
                Write('[' + tag + ']');
                CloseHtmlTag("span");
                NextLine();
            }
        }

        public static void DumpUsingTxt(ReflectionDumper dumper, Descriptor desc, int numTabs = 0)
        {
            dumper.Tab(numTabs);
            dumper.Write(desc.Signature);
        }

        public static void DumpUsingHtml(ReflectionDumper buffer, Descriptor desc, int numTabs = 0)
        {
            var tokens = desc.GetTokens(buffer.HtmlDumpUsingDetail);
            tokens.Remove("DescriptorType");

            string schema = desc.GetSchema(buffer.HtmlDumpUsingDetail);
            string descType = desc.GetDescriptorType();

            string descTag = descType;

            if (desc.Tags.Contains("Deprecated") && buffer.HtmlMarkDeprecated)
                descTag += " deprecated"; // The CSS will strike-through this.

            if (!buffer.HtmlDiffMode && descType != "Class" && descType != "Enum")
                descTag += " child";
            
            buffer.OpenSpanTag(descTag, numTabs, buffer.HtmlDescriptorTagType);
            buffer.NextLine();

            int search = 0;
            bool keepGoing = true;

            while (keepGoing)
            {
                int openToken = schema.IndexOf('{', search);
                keepGoing = false;

                if (openToken >= 0)
                {
                    int closeToken = schema.IndexOf('}', openToken);

                    if (closeToken >= 0)
                    {
                        // Check if any text came before this.
                        string token = schema.Substring(openToken + 1, closeToken - openToken - 1);

                        if (tokens.ContainsKey(token))
                        {
                            if (token == "Parameters" || token.EndsWith("Type"))
                            {
                                MemberDescriptor memberDesc = desc as MemberDescriptor;

                                if (token == "Parameters")
                                {
                                    Parameters parameters = memberDesc.GetParameters();
                                    buffer.WriteParametersElement(parameters, numTabs + 1);
                                }
                                else
                                {
                                    ReflectionType typeDesc = memberDesc.GetResultType();
                                    buffer.WriteTypeElement(typeDesc, numTabs + 1);
                                    buffer.NextLine();
                                }
                            }
                            else if (token == "Tags")
                            {
                                Tags tags = desc.Tags;
                                buffer.WriteTagElements(tags, numTabs + 1);
                            }
                            else
                            {
                                string value = tokens[token]
                                    .ToString()
                                    .Replace("<", "&lt;")
                                    .Replace(">", "&gt;")
                                    .Trim();

                                if (value.Length > 0)
                                {
                                    if (token == "ClassName")
                                        token += " " + descType;

                                    buffer.OpenSpanTag(token, numTabs + 1);
                                    buffer.Write(value);
                                    buffer.CloseHtmlTag("span");
                                    buffer.NextLine();
                                }
                            }
                        }

                        search = closeToken + 1;
                        keepGoing = true;
                    }
                }
            }

            buffer.CloseHtmlTag(buffer.HtmlDescriptorTagType, numTabs);
            buffer.NextLine();
        }

        public string DumpApi(Action<ReflectionDumper, Descriptor, int> WriteSignature, Func<string, string> postProcess = null)
        {
            if (api == null)
                throw new Exception("Cannot Dump API without a database defined.");

            buffer.Clear();

            foreach (ClassDescriptor classDesc in api.Classes)
            {
                WriteSignature(this, classDesc, 0);
                NextLine();

                foreach (MemberDescriptor memberDesc in sorted(classDesc.Members))
                {
                    WriteSignature(this, memberDesc, 1);
                    NextLine();
                }
            }

            foreach (EnumDescriptor enumDesc in api.Enums)
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