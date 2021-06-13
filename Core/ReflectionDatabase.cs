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
        public string Branch { get; set; }
        public string Version { get; set; }

        public Dictionary<string, ClassDescriptor> Classes;
        public Dictionary<string, EnumDescriptor> Enums;

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
            using (JsonTextReader reader = new JsonTextReader(jsonText))
            {
                Type MemberDescriptor = typeof(MemberDescriptor);
                JObject database = JObject.Load(reader);

                // Initialize classes.
                Classes = new Dictionary<string, ClassDescriptor>();

                foreach (JObject classObj in database.GetValue("Classes", StringComparison.InvariantCulture))
                {
                    var classDesc = classObj.ToObject<ClassDescriptor>();
                    classDesc.Database = this;

                    bool classDeprecated = classDesc.HasTag("Deprecated");
                    int membersDeprecated = 0;

                    // Initialize members.
                    foreach (JObject memberObj in classObj.GetValue("Members", StringComparison.InvariantCulture))
                    {
                        if (Enum.TryParse(memberObj.Value<string>("MemberType"), out MemberType memberType))
                        {
                            // Use some Reflection magic to resolve the descriptor object in use.
                            // This assumes that all MemberType values have a corresponding object with
                            // a "Descriptor" suffix (Example: MemberType.Property -> PropertyDescriptor)
                            // It will also assume that the descriptor object derives MemberDescriptor.

                            string typeName = $"{memberType}Descriptor";
                            Type descType = Type.GetType($"{MemberDescriptor.Namespace}.{typeName}");

                            if (!MemberDescriptor.IsAssignableFrom(descType))
                                throw new TypeLoadException(typeName + " does not derive from MemberDescriptor!");

                            var memberDesc = memberObj.ToObject(descType) as MemberDescriptor;
                            memberDesc.Class = classDesc;

                            if (classDeprecated)
                                memberDesc.AddTag("Deprecated");
                            else if (memberDesc.HasTag("Deprecated"))
                                membersDeprecated++;

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