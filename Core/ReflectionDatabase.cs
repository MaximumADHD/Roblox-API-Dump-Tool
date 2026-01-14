using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace RobloxApiDumpTool
{
    public class ReflectionDatabase
    {
        public const string UNKNOWN = "unknown";

        public string Channel { get; set; } = UNKNOWN;
        public string Version { get; set; } = "0.0.0.0";

        public Dictionary<string, ClassDescriptor> Classes;
        public Dictionary<string, EnumDescriptor> Enums;

        public JObject Source { get; private set; }

        public override string ToString()
        {
            return $"{Channel} - {Version}";
        }

        private void setupV1(string filePath)
        {
            string jsonApiDump = File.ReadAllText(filePath);

            using (StringReader jsonText = new StringReader(jsonApiDump))
            using (JsonTextReader reader = new JsonTextReader(jsonText))
            {
                Type MemberDescriptor = typeof(MemberDescriptor);
                JObject database = JObject.Load(reader);

                // Initialize classes.
                Source = database;
                Classes = new Dictionary<string, ClassDescriptor>();

                foreach (JObject classObj in database.GetValue("Classes", StringComparison.InvariantCulture))
                {
                    var classDesc = classObj.ToObject<ClassDescriptor>();
                    classDesc.Database = this;

                    bool classDeprecated = classDesc.HasTag("Deprecated");
                    var membersInternal = new Dictionary<int, int>();
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
                            var securityField = descType.GetField("Security");

                            if (!MemberDescriptor.IsAssignableFrom(descType))
                                throw new TypeLoadException(typeName + " does not derive from MemberDescriptor!");

                            var memberDesc = memberObj.ToObject(descType) as MemberDescriptor;
                            memberDesc.Class = classDesc;

                            if (securityField != null)
                            {
                                var rawSec = securityField.GetValue(memberDesc);
                                var isInternal = false;
                                var level = 0;

                                if (rawSec is ReadWriteSecurity rw)
                                {
                                    isInternal = rw.Read.Internal && rw.Write.Internal;
                                    level = Math.Max(rw.Read.Level, rw.Write.Level);
                                }
                                else if (rawSec is Security sec)
                                {
                                    isInternal = sec.Internal;
                                    level = sec.Level;
                                }

                                if (isInternal)
                                {
                                    if (!membersInternal.ContainsKey(level))
                                        membersInternal.Add(level, 0);

                                    membersInternal[level]++;
                                }
                            }

                            if (classDeprecated)
                                memberDesc.AddTag("Deprecated");
                            else if (memberDesc.HasTag("Deprecated"))
                                membersDeprecated++;

                            if (memberDesc is PropertyDescriptor prop)
                                if (prop.ValueType.Category == TypeCategory.Class)
                                    prop.ValueType.Optional = true;

                            if (memberDesc.Capabilities == null)
                                memberDesc.Capabilities = new Capabilities();

                            if (memberDesc is FunctionDescriptor func)
                                if (func.ReturnType.Name == "Instance" || func.ReturnType.Name == "RaycastResult")
                                    func.ReturnType.Optional = true;

                            classDesc.Members.Add(memberDesc);
                        }
                    }

                    // Check if the class needs a security type
                    if (classDesc.Members.Any())
                    {
                        int numMembersInternal = membersInternal.Values.Sum();

                        if (classDesc.Members.Count == numMembersInternal)
                        {
                            var bestPair = membersInternal
                                .OrderBy(pair => pair.Value * 1000 + pair.Key)
                                .FirstOrDefault();

                            var secLevel = (SecurityType)bestPair.Key;
                            classDesc.Security = new Security(secLevel);
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

        private void setupV2(string filePath)
        {
            // The only information currently worth extracting from V2 is class security.
            // When it has correct information parity with V1, or maybe it gets support for Luau type annotations, update accordingly?
            string jsonApiDump = File.ReadAllText(filePath);

            using (StringReader jsonText = new StringReader(jsonApiDump))
            using (JsonTextReader reader = new JsonTextReader(jsonText))
            {
                JObject database = JObject.Load(reader);
                Classes = new Dictionary<string, ClassDescriptor>();
                Source = database;

                foreach (JObject classObj in database.GetValue("classes", StringComparison.InvariantCulture))
                {
                    var name = classObj.Value<string>("name");
                    var security = classObj.Value<string>("security");
                    var superclass = classObj.Value<string>("baseClass");

                    var classDesc = new ClassDescriptor()
                    {
                        Name = name,
                        Database = this,
                        Superclass = superclass,
                    };

                    if (Enum.TryParse(security, out SecurityTypeV2 securityV2))
                    {
                        switch (securityV2)
                        {
                            case SecurityTypeV2.None:
                            {
                                classDesc.Security = SecurityType.None;
                                break;
                            }
                            case SecurityTypeV2.Plugin:
                            {
                                classDesc.Security = SecurityType.PluginSecurity;
                                break;
                            }
                            case SecurityTypeV2.WritePlayer:
                            {
                                classDesc.Security = SecurityType.WritePlayerSecurity;
                                break;
                            }
                            case SecurityTypeV2.LocalUser:
                            {
                                classDesc.Security = SecurityType.LocalUserSecurity;
                                break;
                            }
                            case SecurityTypeV2.RobloxScript:
                            {
                                classDesc.Security = SecurityType.RobloxScriptSecurity;
                                break;
                            }
                            case SecurityTypeV2.RobloxEngine:
                            {
                                classDesc.Security = SecurityType.RobloxSecurity;
                                break;
                            }
                            case SecurityTypeV2.NotAccessible:
                            {
                                classDesc.Security = SecurityType.NotAccessibleSecurity;
                                break;
                            }
                            default:
                            {
                                throw new InvalidDataException("Unknown security type!");
                            }
                        }

                        Classes.Add(name, classDesc);
                    }
                }
            }
        }

        public ReflectionDatabase(string filePath, ApiDumpSchema format)
        {
            switch (format)
            {
                case ApiDumpSchema.V1_Full:
                case ApiDumpSchema.V1_Partial:
                {
                    setupV1(filePath);
                    break;
                }
                case ApiDumpSchema.V2:
                {
                    setupV2(filePath);
                    break;
                }
                default:
                {
                    throw new IndexOutOfRangeException($"Unknown format: {format}");
                }
            } 
        }

        public void MungeV2(string filePathV2)
        {
            var api2 = new ReflectionDatabase(filePathV2, ApiDumpSchema.V2);

            foreach (var className in api2.Classes.Keys)
            {
                var class2 = api2.Classes[className];

                if (!Classes.TryGetValue(className, out var class1))
                    continue;

                class1.Security = class2.Security;
            }
        }
    }
}