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
            return "[" + (CanSave ? "Can Save" : "Cannot Save") + " | " + (CanLoad ? "Can Load" : "Cannot Load") + "]";
        }
    }

    public struct RobloxType
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
        public RobloxType Type;
        public string Name;
        public string Default;

        public override string ToString()
        {
            string result = Type.ToString() + " " + Name;

            if (Default != null && Default.Length > 0)
                result += " = " + Default;

            return result;
        }
    }
}
