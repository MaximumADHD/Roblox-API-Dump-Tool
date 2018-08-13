using System;
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
    }

    public struct Serialization
    {
        public bool CanSave;
        public bool CanLoad;

        public override string ToString()
        {
            if (CanSave && CanLoad)
                return "`Saves & Loads`";
            else if (CanSave)
                return "`Save only`";
            else if (CanLoad)
                return "`Load only`";
            else
                return "`None`";
        }
    }

    public struct TypeDescriptor
    {
        public TypeCategory Category;
        public string Name;
        public override string ToString() => GetSignature();

        public string GetSignature()
        {
            if (Category != TypeCategory.Group && Category != TypeCategory.Primitive)
            {
                string category = Util.GetEnumName(Category);
                string result = category + '<' + Name + '>';

                return result;
            }
            else
            {
                return Name;
            }
        }
    }

    public struct Parameter
    {
        public TypeDescriptor Type;
        public string Name;
        public string Default;

        public override string ToString()
        {
            string result = Type.ToString() + " " + Name;

            if (Default != null && Default.Length > 0)
            {
                result += " = ";
                if (Type.Category == TypeCategory.Enum)
                    result += "Enum." + Type.Name + "." + Default;
                else if (Type.Name == "string")
                    result += '"' + Default + '"';
                else
                    result += Default;
            }

            return result;
        }
    }
}
