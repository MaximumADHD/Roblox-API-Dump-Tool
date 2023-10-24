using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace RobloxApiDumpTool
{
    public class ReflectionHtml
    {
        private XmlNode Root;
        private List<XmlNode> Stack;
        private XmlDocument Document;

        public bool HasElements { get; private set; }


        private static readonly XmlWriterSettings writerSettings = new XmlWriterSettings()
        {
            Indent = true,
            IndentChars = "",
            NewLineChars = "",
            CheckCharacters = false,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = true,
            NewLineHandling = NewLineHandling.Entitize,
            ConformanceLevel = ConformanceLevel.Fragment,
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
            Document = new XmlDocument();
            Root = Document.CreateElement("ROOT");

            Stack = new List<XmlNode>();
            Stack.Add(Root);
        }

        public XmlNode CreateElement(string elemType, string className = null)
        {
            var element = Document.CreateElement(elemType);

            if (className != null)
                element.SetAttribute("class", className);

            Current.AppendChild(element);
            HasElements = true;

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

                if (last == null || last == Root)
                    return;

                Stack.Remove(last);
            }
        }

        public XmlNode Span(string className, string text = "")
        {
            var node = CreateElement("span", className);
            node.InnerText = text;

            return node;
        }

        public XmlNode String(string text)
        {
            if (!text.StartsWith("\"") && !text.EndsWith("\""))
                text = '"' + text + '"';

            return Span("String", text);
        }

        public XmlNode OpenDiv(string className, Action action)
        {
            return OpenStack("div", className, action);
        }

        public XmlNode OpenSpan(string className, Action action)
        {
            return OpenStack("span", className, action);
        }

        public XmlNode Break()
        {
            return CreateElement("br");
        }

        public XmlNode Symbol(string str)
        {
            return Span("symbol", str);
        }

        public XmlText Text(string str)
        {
            var text = Document.CreateTextNode(str);
            Current.AppendChild(text);

            return text;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            using (var writer = XmlWriter.Create(builder, writerSettings))
                Root.WriteContentTo(writer);

            // FIXME: This sucks, but I need a balance of
            //        somewhat readable HTML and avoiding
            //        whitespace padding between spans.

            string result = builder.ToString()
                .Replace("<div", "\n<div")
                .Replace("</h2>", "</h2>\n")
                .Replace("<br />", "\n<br/>\n");

            return result;
        }

        public void WriteTo(StringBuilder builder)
        {
            var html = ToString();
            builder.Append(html);
        }

        public void Clear()
        {
            Stack.Clear();
            Document = new XmlDocument();

            Root = Document.CreateElement("ROOT");
            Document.AppendChild(Root);

            Stack.Add(Root);
            HasElements = false;
        }
    }
}
