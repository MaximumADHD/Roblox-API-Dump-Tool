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

        public ReflectionDatabase(string jsonApiDump)
        {
            using (StringReader jsonText = new StringReader(jsonApiDump))
            {
                JsonTextReader reader = new JsonTextReader(jsonText);
                JObject database = JObject.Load(reader);

                // Initialize classes.
                Classes = new Dictionary<string, ClassDescriptor>();

                foreach (JObject classObj in database.GetValue("Classes"))
                {
                    var classDesc = classObj.ToObject<ClassDescriptor>();
                    classDesc.Database = this;

                    // Initialize members.
                    foreach (JObject memberObj in classObj.GetValue("Members"))
                    {
                        MemberType memberType;

                        if (Enum.TryParse(memberObj.Value<string>("MemberType"), out memberType))
                        {
                            MemberDescriptor memberDesc = null;

                            if (memberType == MemberType.Property)
                                memberDesc = memberObj.ToObject<PropertyDescriptor>();
                            else if (memberType == MemberType.Function)
                                memberDesc = memberObj.ToObject<FunctionDescriptor>();
                            else if (memberType == MemberType.Event)
                                memberDesc = memberObj.ToObject<EventDescriptor>();
                            else if (memberType == MemberType.Callback)
                                memberDesc = memberObj.ToObject<CallbackDescriptor>();

                            memberDesc.Class = classDesc;
                            classDesc.Members.Add(memberDesc);
                        }
                    }

                    Classes.Add(classDesc.Name, classDesc);
                }

                // Initialize enums.
                Enums = new Dictionary<string, EnumDescriptor>();

                foreach (JObject enumObj in database.GetValue("Enums"))
                {
                    var enumDesc = enumObj.ToObject<EnumDescriptor>();
                    
                    // Initialize items.
                    foreach (JObject itemObj in enumObj.GetValue("Items"))
                    {
                        EnumItemDescriptor itemDesc = itemObj.ToObject<EnumItemDescriptor>();
                        itemDesc.Enum = enumDesc;
                        enumDesc.Items.Add(itemDesc);
                    }

                    Enums.Add(enumDesc.Name, enumDesc);
                }
            }
        }
    }
}