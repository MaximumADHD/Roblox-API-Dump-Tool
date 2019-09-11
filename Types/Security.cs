using System;
using Newtonsoft.Json;

namespace Roblox.Reflection
{
    public enum SecurityType
    {
        None,
        PluginSecurity,
        LocalUserSecurity,
        RobloxScriptSecurity,
        RobloxSecurity,
        NotAccessibleSecurity
    }

    public class Security
    {
        public SecurityType Type;

        public string Prefix;
        public string Value => Describe(true);

        public Security(SecurityType security, string prefix = "")
        {
            Type = security;
            Prefix = prefix;
        }

        public Security(string security, string prefix = "")
        {
            Enum.TryParse(security, out Type);
            Prefix = prefix;
        }

        public static implicit operator Security(string security)
        {
            return new Security(security);
        }

        public static implicit operator Security(SecurityType security)
        {
            return new Security(security);
        }

        public string Describe(bool displayNone)
        {
            string result = "";

            if (displayNone || Type != SecurityType.None)
                result += $"{{{Prefix}{Program.GetEnumName(Type)}}}";

            return result;
        }

        public override string ToString()
        {
            return Describe(false);
        }
    }

    public class ReadWriteSecurity
    {
        public Security Read;
        public Security Write;

        public bool Merged => (Read.Type == Write.Type);
        public string Value => Describe(true);

        [JsonConstructor]
        public ReadWriteSecurity(string read, string write)
        {
            Read = new Security(read);
            Write = new Security(write, "✏️");
        }

        public ReadWriteSecurity(string security) : this(security, security)
        {
        }

        public ReadWriteSecurity(SecurityType read, SecurityType? write = null)
        {
            Read = new Security(read);
            Write = new Security(write ?? read, "✏️");
        }

        public static implicit operator ReadWriteSecurity(string security)
        {
            return new ReadWriteSecurity(security);
        }

        public static implicit operator ReadWriteSecurity(SecurityType security)
        {
            return new ReadWriteSecurity(security);
        }

        public string Describe(bool displayNone)
        {
            string result = "";

            string read = Read.Describe(displayNone);
            string write = Write.Describe(displayNone);

            if (read.Length > 0)
                result += read;

            if (Read.Type != Write.Type && write.Length > 0)
                result += ' ' + write;

            return result.Trim();
        }

        public override string ToString()
        {
            return Describe(false);
        }
    }
}