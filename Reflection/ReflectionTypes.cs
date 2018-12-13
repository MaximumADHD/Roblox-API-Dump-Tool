using System.Collections.Generic;
using System.Linq;

namespace Roblox.Reflection
{
    public enum MemberType
    {
        Property,
        Function,
        Event,
        Callback
    }

    public enum SecurityType
    {
        None,
        PluginSecurity,
        LocalUserSecurity,
        RobloxScriptSecurity,
        RobloxSecurity,
        NotAccessibleSecurity
    }

    public enum TypeCategory
    {
        Primitive,
        Class,
        Enum,
        Group,
        DataType
    }

    public enum MemoryTag
    {
        Internal,
        HttpCache,
        Instances,
        Signals,
        LuaHeap,
        Script,
        PhysicsCollision,
        PhysicsParts,
        GraphicsSolidModels,
        GraphicsMeshParts,
        GraphicsParticles,
        GraphicsParts,
        GraphicsSpatialHash,
        GraphicsTerrain,
        GraphicsTexture,
        GraphicsTextureCharacter,
        Sounds,
        StreamingSounds,
        TerrainVoxels,
        Gui,
        Animation,
        Navigation
    }

    public struct ReadWriteSecurity
    {
        public SecurityType Read;
        public SecurityType Write;

        public override string ToString()
        {
            return "Read: " + Util.GetEnumName(Read) + " | Write: " + Util.GetEnumName(Read);
        }

        public bool ShouldMergeWith(ReadWriteSecurity newSecurity)
        {
            return Read  != newSecurity.Read  && 
                   Write != newSecurity.Write && 
                   newSecurity.Read == newSecurity.Write;
        }
    }

    public struct Serialization
    {
        public bool CanSave;
        public bool CanLoad;

        private static string[] flagLabels = new string[4]
        {
            "None",
            "Load-only",
            "Save-only",
            "Saves & Loads"
        };

        public override string ToString()
        {
            int flags = (CanSave ? 1 : 0) << 1 | (CanLoad ? 1 : 0);
            return "'" + flagLabels[flags] + "'";
        }
    }

    public struct TypeDescriptor
    {
        public string Name;
        public TypeCategory Category;
        public override string ToString() => GetSignature();

        public string GetSignature()
        {
            string result;

            if (Name == "Instance" || Category != TypeCategory.Class && Category != TypeCategory.Enum)
                result = Name;
            else
                result = Util.GetEnumName(Category) + '<' + Name + '>';

            return result;
        }
    }

    public struct Parameter
    {
        public TypeDescriptor Type;
        public string Name;
        public string Default;

        private const string quote = "\"";

        public override string ToString()
        {
            string result = Type.ToString() + " " + Name;

            if (Type.Name == "string" && Default != null)
                if (!Default.StartsWith(quote) || !Default.EndsWith(quote))
                    Default = quote + Default + quote;

            if (Default != null && Default.Length > 0)
                result += " = " + Default;

            return result;
        }
    }

    public class Parameters : List<Parameter>
    {
        public override string ToString()
        {
            string[] parameters = this.Select(param => param.ToString()).ToArray();
            return '(' + string.Join(", ", parameters) + ')';
        }
    }

    public class Tags : List<string>
    {
        public Tags(IEnumerable<string> tags = null)
        {
            tags?.ToList().ForEach(Add);
        }

        public override string ToString()
        {
            // (Hopefully) temporary patch.
            if (Contains("ReadOnly"))
                Remove("NotReplicated");

            string[] tags = this.Select(tag => '[' + tag + ']').ToArray();
            return string.Join(" ", tags);
        }

        public string Signature
        {
            get
            {
                if (Count > 0)
                {
                    string label = "Tag";
                    if (Count > 1)
                        label += "s";

                    return label + ' ' + ToString();
                }

                return "";
            }
        }
    }
}