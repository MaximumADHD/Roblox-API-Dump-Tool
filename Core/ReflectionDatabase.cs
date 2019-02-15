using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public ReflectionDatabase(string filePath)
        {
            string jsonApiDump = File.ReadAllText(filePath);

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

                            classDesc.Members.Add(memberDesc);
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