using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Roblox.Reflection
{
    public static class Util
    {
        public const string NewLine = "\r\n";

        public static ReadOnlyCollection<string> TypePriority = new ReadOnlyCollection<string>(new string[]
        {
            "Class",
            "Property",
            "Function",
            "Event",
            "Callback",
            "Enum",
            "EnumItem"
        });


        public static string GetEnumName<T>(T item)
        {
            return Enum.GetName(typeof(T), item);
        }

        public static string GetParamSignature(List<Parameter> parameters)
        {
            return '(' + string.Join(", ", parameters.Select(param => param.ToString()).ToArray()) + ')';
        }

        public static string GetTagSignature(List<string> tags)
        {
            return string.Join(" ", tags.Select(tag => tag = '[' + tag + ']').ToArray());
        }

        public static string GetSecuritySignature(SecurityType security, string prefix = "")
        {
            if (security != SecurityType.None)
                return '{' + prefix + GetEnumName(security) + '}';
            else
                return "";
        }

        public static string GetSecuritySignature(ReadWriteSecurity security)
        {
            string read = GetSecuritySignature(security.Read);
            string write = GetSecuritySignature(security.Write, "✎");

            string result = "";

            if (read.Length > 0)
                result += read;

            if (write.Length > 0 && security.Read != security.Write)
                result += ' ' + write;

            return result.Trim();
        }
    }
}
