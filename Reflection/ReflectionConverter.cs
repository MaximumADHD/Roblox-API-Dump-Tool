using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Roblox.Reflection
{
    public class ReflectionConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ClassDescriptor)
                || objectType == typeof(EnumDescriptor);
        }

        private static ClassDescriptor ReadClassDescriptor(JObject obj, Descriptor desc)
        {
            JToken[] members = obj.GetValue("Members").ToArray();
            JToken superclass = obj.GetValue("Superclass");
            JToken memoryTag = obj.GetValue("MemoryCategory");

            ClassDescriptor classDesc = new ClassDescriptor();
            classDesc.Name = desc.Name;
            classDesc.Tags = desc.Tags;
            classDesc.Superclass = superclass.ToString();

            Enum.TryParse(memoryTag.ToString(), out classDesc.MemoryCategory);

            foreach (JToken member in members)
            {
                MemberType memberType;
                if (Enum.TryParse(member.Value<string>("MemberType"), out memberType))
                {
                    switch (memberType)
                    {
                        case MemberType.Property:
                            PropertyDescriptor prop = member.ToObject<PropertyDescriptor>();
                            classDesc.Properties.Add(prop);
                            classDesc.Members.Add(prop);
                            break;
                        case MemberType.Function:
                            FunctionDescriptor func = member.ToObject<FunctionDescriptor>();
                            classDesc.Functions.Add(func);
                            classDesc.Members.Add(func);
                            break;
                        case MemberType.Callback:
                            CallbackDescriptor call = member.ToObject<CallbackDescriptor>();
                            classDesc.Callbacks.Add(call);
                            classDesc.Members.Add(call);
                            break;
                        case MemberType.Event:
                            EventDescriptor evnt = member.ToObject<EventDescriptor>();
                            classDesc.Events.Add(evnt);
                            classDesc.Members.Add(evnt);
                            break;
                    }
                }
            }

            foreach (MemberDescriptor member in classDesc.Members)
                member.Class = classDesc;

            return classDesc;
        }

        private static EnumDescriptor ReadEnumDescriptor(JObject obj, Descriptor desc)
        {
            JToken[] items = obj.GetValue("Items").ToArray();

            EnumDescriptor enumDesc = new EnumDescriptor();
            enumDesc.Name = desc.Name;
            enumDesc.Tags = desc.Tags;

            foreach (JToken item in items)
            {
                EnumItemDescriptor itemDesc = item.ToObject<EnumItemDescriptor>();
                itemDesc.Enum = enumDesc;
                enumDesc.Items.Add(itemDesc);
            }

            return enumDesc;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            Descriptor desc = obj.ToObject<Descriptor>();

            if (objectType == typeof(ClassDescriptor))
                return ReadClassDescriptor(obj, desc);
            else if (objectType == typeof(EnumDescriptor))
                return ReadEnumDescriptor(obj, desc);
            else
                throw new NotImplementedException();

        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
