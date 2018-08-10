using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Roblox.Reflection
{
    public class ReflectionClassReader : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ClassDescriptor);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            JToken[] members = obj.GetValue("Members").ToArray();
            JToken superToken = obj.GetValue("Superclass");
            JToken memoryTag = obj.GetValue("MemoryCategory");

            Descriptor desc = obj.ToObject<Descriptor>();

            ClassDescriptor classDesc = new ClassDescriptor();
            classDesc.Name = desc.Name;
            classDesc.Tags = desc.Tags;
            classDesc.Superclass = superToken.ToString();

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
                member.ParentClass = classDesc;

            return classDesc;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
