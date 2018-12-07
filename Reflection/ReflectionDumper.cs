using System;
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
        
        private void write(object text)
        {
            buffer.Append(text);
        }

        private void nextLine()
        {
            write(Util.NewLine);
        }

        private void tab(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                write('\t');
            }
        }

        private void openHtmlTag(string tagName, string attributes = "", int numTabs = 0)
        {
            tab(numTabs);
            write('<' + tagName);

            if (attributes.Length > 0)
                write(" " + attributes);

            write('>');
        }

        private void closeHtmlTag(string tagName, int numTabs = 0)
        {
            tab(numTabs);
            write("</" + tagName + ">");
        }

        private void openSpanTag(string spanClass, int numTabs = 0, string tagType = "span")
        {
            string attributes = "class=\"" + spanClass + '"';
            tab(numTabs);
            openHtmlTag(tagType, attributes);
        }

        private void writeVoidTag(string tagName, int numTabs = 0)
        {
            tab(numTabs);
            write('<' + tagName + "/>");
        }

        private void writeTypeElement(TypeDescriptor type, int numTabs = 0)
        {
            string typeVal = type.ToString();

            if (typeVal.Contains("<") && typeVal.EndsWith(">"))
            {
                string category = Util.GetEnumName(type.Category);
                openSpanTag("Type", numTabs);
                write(category);

                closeHtmlTag("span");
                nextLine();

                openSpanTag("InnerType", numTabs);
                write(type.Name);

                closeHtmlTag("span");
                nextLine();
            }
            else
            {
                openSpanTag("Type", numTabs);
                write(type);
                closeHtmlTag("span");
            }
        }

        private void writeParamsElement(List<Parameter> parameters)
        {
            openSpanTag("Parameters", 1);

            if (parameters.Count > 0)
                nextLine();

            for (int i = 0; i < parameters.Count; i++)
            {
                Parameter param = parameters[i];

                string paramLbl = "Parameter";

                if (i == 0)
                    paramLbl += " first";

                if (i == parameters.Count - 1)
                    paramLbl += " last";

                openSpanTag(paramLbl, 2);
                nextLine();

                // Write Type
                writeTypeElement(param.Type, 3);
                nextLine();

                // Write Name
                openSpanTag("ParamName", 3);
                write(param.Name);

                closeHtmlTag("span");
                nextLine();

                // Write Default
                if (param.Default != null)
                {
                    openSpanTag("ParamDefault " + param.Type.Name, 3);
                    write(param.Default);

                    closeHtmlTag("span");
                    nextLine();
                }

                closeHtmlTag("span", 2);
                nextLine();
            }

            closeHtmlTag("span", parameters.Count > 0 ? 1 : 0);
            nextLine();
        }

        public static void DumpUsingTxt(ReflectionDumper dumper, Descriptor desc, int numTabs = 0)
        {
            dumper.tab(numTabs);
            dumper.write(desc.Signature);
        }

        public static void DumpUsingHtml(ReflectionDumper buffer, Descriptor desc, int numTabs = 0)
        {
            var tokens = desc.GetTokens(true);
            tokens.Remove("DescriptorType");

            string schema = desc.GetSchema(true);
            string descType = desc.GetDescriptorType();

            if (desc.Tags.Contains("Deprecated"))
                descType += " deprecated"; // The CSS will strike-through this.

            buffer.openSpanTag(descType, 0, "div");
            buffer.nextLine();

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
                                    List<Parameter> parameters = memberDesc.GetParameters();
                                    buffer.writeParamsElement(parameters);
                                }
                                else
                                {
                                    TypeDescriptor typeDesc = memberDesc.GetResultType();
                                    buffer.writeTypeElement(typeDesc, 1);
                                    buffer.nextLine();
                                }
                            }
                            else
                            {
                                string value = tokens[token]
                                    .Replace("<", "&lt;")
                                    .Replace(">", "&gt;")
                                    .Trim();

                                if (value.Length > 0)
                                {
                                    if (token == "ClassName")
                                        token += " " + descType;

                                    buffer.openSpanTag(token, 1);
                                    buffer.write(value);
                                    buffer.closeHtmlTag("span");
                                    buffer.nextLine();
                                }
                            }
                        }

                        search = closeToken + 1;
                        keepGoing = true;
                    }
                }
            }

            buffer.closeHtmlTag("div");
            buffer.nextLine();
        }

        public string DumpApi(Action<ReflectionDumper, Descriptor, int> writeSignature, Func<string, string> postProcess = null)
        {
            buffer.Clear();

            foreach (ClassDescriptor classDesc in api.Classes)
            {
                writeSignature(this, classDesc, 0);
                nextLine();

                foreach (MemberDescriptor memberDesc in sorted(classDesc.Members))
                {
                    writeSignature(this, memberDesc, 1);
                    nextLine();
                }
            }

            foreach (EnumDescriptor enumDesc in api.Enums)
            {
                writeSignature(this, enumDesc, 0);
                nextLine();

                foreach (EnumItemDescriptor itemDesc in sorted(enumDesc.Items))
                {
                    writeSignature(this, itemDesc, 1);
                    nextLine();
                }
            }

            string result = buffer.ToString();

            string post = postProcess?.Invoke(result);
            if (post != null)
                result = post;
 
            return result;
        }
    }
}