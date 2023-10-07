using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RobloxApiDumpTool
{
    public class ReflectionHtml
    {
        private XmlDocument Root;
        private List<XmlNode> Stack;

        private static readonly XmlWriterSettings writerSettings = new XmlWriterSettings()
        {
            Indent = true,
            IndentChars = "",
            NewLineChars = "",
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = true,
            CheckCharacters = false,
        };

        public XmlNode Current
        {
            get
            {
                XmlNode node = Stack.LastOrDefault();

                if (node == null)
                    return Root;

                return node;
            }
        }

        public ReflectionHtml()
        {
            Root = new XmlDocument();
            Stack = new List<XmlNode>();
        }

        public XmlNode CreateElement(string elemType, string className = null, string id = null)
        {
            var element = Root.CreateElement(elemType);

            if (className != null)
                element.SetAttribute("class", className);

            Current.AppendChild(element);
            return element; 
        }

        public XmlNode OpenStack(string elemType, string className, Action action)
        {
            var node = CreateElement(elemType, className);
            Stack.Add(node);

            action();
            CloseStack();

            return node;
        }

        private void CloseStack()
        {
            if (Stack.Count > 0)
            {
                var last = Stack.LastOrDefault();

                if (last == null)
                    return;

                Stack.Remove(last);
            }
        }

        public XmlNode Div(string className, string text)
        {
            var node = CreateElement("div", className);
            node.InnerText = text;

            return node;
        }

        public XmlNode Span(string className, string text = "")
        {
            var node = CreateElement("span", className);
            node.InnerText = text;

            return node;
        }

        public XmlNode Break()
        {
            return CreateElement("br");
        }

        public XmlNode OpenDiv(string className, Action action)
        {
            return OpenStack("div", className, action);
        }

        public XmlNode OpenSpan(string className, Action action)
        {
            return OpenStack("span", className, action);
        }

        public XmlNode Symbol(string str)
        {
            return Span("symbol", str);
        }

        public void WriteTo(StringBuilder builder)
        {
            var writer = XmlWriter.Create(builder, writerSettings);
            Root.WriteContentTo(writer);
            writer.Dispose();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            WriteTo(builder);

            return builder.ToString();
        }

        public void Clear()
        {
            Stack.Clear();
            Root = new XmlDocument();
        }
    }
}
