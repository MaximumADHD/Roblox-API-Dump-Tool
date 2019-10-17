using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Roblox.Reflection
{
    public class ReflectionDatabase
    {
        public string Branch;
        public string Version;

        public Dictionary<string, ClassDescriptor> Classes;
        public Dictionary<string, EnumDescriptor> Enums;

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

        public override string ToString()
        {
            return $"{Branch} - {Version}";
        }

        public ReflectionDatabase(string filePath, string branch = "unknown", string version = "0.0.0.0")
        {
            string jsonApiDump = File.ReadAllText(filePath);

            Branch = branch;
            Version = version;

            using (StringReader jsonText = new StringReader(jsonApiDump))
            {
                Type MemberDescriptor = typeof(MemberDescriptor);

                JsonTextReader reader = new JsonTextReader(jsonText);
                JObject database = JObject.Load(reader);

                // Initialize classes.
                Classes = new Dictionary<string, ClassDescriptor>();

                foreach (JObject classObj in database.GetValue("Classes"))
                {
                    var classDesc = classObj.ToObject<ClassDescriptor>();
                    classDesc.Database = this;

                    bool classDeprecated = classDesc.HasTag("Deprecated");
                    int membersDeprecated = 0;

                    int minSecurity = (int)SecurityType.NotAccessibleSecurity;

                    // Initialize members.
                    foreach (JObject memberObj in classObj.GetValue("Members"))
                    {
                        MemberType memberType;

                        if (Enum.TryParse(memberObj.Value<string>("MemberType"), out memberType))
                        {
                            // Use some Reflection magic to resolve the descriptor object in use.
                            // This assumes that all MemberType values have a corresponding object with
                            // a "Descriptor" suffix (Example: MemberType.Property -> PropertyDescriptor)
                            // It will also assume that the descriptor object derives MemberDescriptor.

                            string typeName = Program.GetEnumName(memberType) + "Descriptor";
                            Type descType = Type.GetType(MemberDescriptor.Namespace + '.' + typeName);

                            if (!MemberDescriptor.IsAssignableFrom(descType))
                                throw new TypeLoadException(typeName + " does not derive from MemberDescriptor!");

                            var memberDesc = memberObj.ToObject(descType) as MemberDescriptor;
                            memberDesc.Class = classDesc;

                            if (classDeprecated)
                                memberDesc.AddTag("Deprecated");
                            else if (memberDesc.HasTag("Deprecated"))
                                membersDeprecated++;

                            int securityValue = minSecurity;

                            if (memberDesc is PropertyDescriptor)
                            {
                                var propDesc = memberDesc as PropertyDescriptor;
                                var security = propDesc.Security;

                                if (memberDesc.HasTag("NotScriptable"))
                                {
                                    propDesc.Security = SecurityType.NotAccessibleSecurity;
                                    security = propDesc.Security;
                                }
                                
                                int read = (int)security.Read.Type;
                                int write = (int)security.Write.Type;

                                securityValue = Math.Min(securityValue, Math.Min(read, write));
                            }
                            else
                            {
                                var type = memberDesc.GetType();
                                var securityField = type.GetField("Security");

                                Security security = (Security)securityField.GetValue(memberDesc);
                                securityValue = Math.Min(securityValue, (int)security.Type);
                            }

                            minSecurity = Math.Min(securityValue, minSecurity);
                            classDesc.Members.Add(memberDesc);
                        }
                    }

                    // Drop deprecated members that have a direct PascalCase variant.
                    var memberLookup = classDesc.Members.ToDictionary(member => member.Name);

                    foreach (string memberName in memberLookup.Keys)
                    {
                        char firstChar = memberName[0];

                        if (char.IsLower(firstChar))
                        {
                            string pascalCase = char.ToUpper(firstChar) + memberName.Substring(1);

                            if (memberLookup.ContainsKey(pascalCase))
                            {
                                MemberDescriptor oldMember = memberLookup[memberName];

                                if (oldMember.HasTag("Deprecated"))
                                    membersDeprecated--;

                                classDesc.Members.Remove(oldMember);
                            }
                        }
                    }

                    if (membersDeprecated == classDesc.Members.Count && membersDeprecated > 0)
                        classDesc.AddTag("Deprecated");

                    if (classDesc.Members.Count == 0)
                        minSecurity = 0;

                    var securityType = (SecurityType)minSecurity;
                    classDesc.Security = securityType;

                    if (securityType != SecurityType.None)
                    {
                        foreach (MemberDescriptor memberDesc in classDesc.Members)
                        {
                            if (memberDesc is PropertyDescriptor)
                            {
                                var propDesc = memberDesc as PropertyDescriptor;
                                var security = propDesc.Security;

                                var read = security.Read.Type;
                                security.Read = (read == securityType ? SecurityType.None : read);

                                var write = security.Write.Type;
                                security.Write = (write == securityType ? SecurityType.None : write);
                            }
                            else
                            {
                                var type = memberDesc.GetType();
                                var securityField = type.GetField("Security");

                                Security security = (Security)securityField.GetValue(memberDesc);

                                if (security.Type == securityType)
                                {
                                    Security none = SecurityType.None;
                                    securityField.SetValue(memberDesc, none);
                                }
                            }
                        }
                    }

                    Classes.Add(classDesc.Name, classDesc);
                }

                // Initialize enums.
                Enums = new Dictionary<string, EnumDescriptor>();

                foreach (JObject enumObj in database.GetValue("Enums"))
                {
                    var enumDesc = enumObj.ToObject<EnumDescriptor>();

                    bool enumDeprecated = enumDesc.HasTag("Deprecated");
                    int itemsDeprecated = 0;
                    
                    // Initialize items.
                    foreach (JObject itemObj in enumObj.GetValue("Items"))
                    {
                        EnumItemDescriptor itemDesc = itemObj.ToObject<EnumItemDescriptor>();
                        itemDesc.Enum = enumDesc;

                        if (enumDeprecated)
                            itemDesc.AddTag("Deprecated");
                        else if (itemDesc.HasTag("Deprecated"))
                            itemsDeprecated++;

                        enumDesc.Items.Add(itemDesc);
                    }

                    if (itemsDeprecated == enumDesc.Items.Count && itemsDeprecated > 0)
                        enumDesc.AddTag("Deprecated");

                    Enums.Add(enumDesc.Name, enumDesc);
                }
            }
        }
    }
}