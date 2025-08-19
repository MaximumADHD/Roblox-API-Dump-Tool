using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace RobloxApiDumpTool
{
    public sealed class FunctionDescriptor : MemberDescriptor
    {
        [JsonProperty("ReturnType")]
        internal JToken WriteReturnTypeJson
        {
            get => null;

            set
            {
                if (value.Type == JTokenType.Array)
                {
                    ReturnType = new LuaType()
                    {
                        SubTypes = value.ToObject<LuaType[]>(),
                        Category = TypeCategory.Group,
                        Name = "Tuple",
                    };

                    return;
                }

                ReturnType = value.ToObject<LuaType>();
            }
        }

        [JsonIgnore]
        public LuaType ReturnType;
        public Parameters Parameters;
        public Security Security;

        public override string GetSchema(bool detailed = true)
        {
            string schema = base.GetSchema(detailed)
                .Replace(".", ":");

            if (detailed)
                schema += "{Parameters} -> {ReturnType} {Capabilities} {Security} {Tags} {ThreadSafety}";

            return schema;
        }
    }
}