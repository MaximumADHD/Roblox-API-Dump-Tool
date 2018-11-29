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

    public enum DeveloperMemoryTag
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
        public TypeCategory Category;
        public string Name;
        public override string ToString() => GetSignature();

        public string GetSignature()
        {
            if (Name == "Instance" || Category != TypeCategory.Class && Category != TypeCategory.Enum)
            {
                return Name;
            }
            else
            {
                return Util.GetEnumName(Category) + '<' + Name + '>';
            }
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
}
