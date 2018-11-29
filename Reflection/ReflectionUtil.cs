using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

        public static string DescribeParameters(List<Parameter> parameters)
        {
            return '(' + string.Join(", ", parameters.Select(param => param.ToString()).ToArray()) + ')';
        }

        public static string DescribeTags(List<string> tags, bool prefixed = false)
        {
            // (Hopefully) temporary patch.
            if (tags.Contains("ReadOnly"))
                tags.Remove("NotReplicated");

            string result = string.Join(" ", tags.Select(tag => tag = '[' + tag + ']').ToArray());

            if (prefixed && tags.Count > 0)
                result = (tags.Count > 1 ? "Tags " : "Tag ") + result;

            return result;
        }

        public static string DescribeSecurity(SecurityType security, string prefix = "")
        {
            if (security != SecurityType.None)
                return '{' + prefix + GetEnumName(security) + '}';

            return "";
        }

        public static string DescribeSecurity(ReadWriteSecurity security)
        {
            string read = DescribeSecurity(security.Read);
            string write = DescribeSecurity(security.Write, "✎");

            string result = "";

            if (read.Length > 0)
                result += read;

            if (write.Length > 0 && security.Read != security.Write)
                result += ' ' + write;

            return result.Trim();
        }
    }
}
