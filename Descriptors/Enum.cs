using System.Collections.Generic;
using Newtonsoft.Json;

namespace Roblox.Reflection
{
    public sealed class EnumDescriptor : Descriptor
    {
        [JsonIgnore]
        public List<EnumItemDescriptor> Items = new List<EnumItemDescriptor>();
    }
}