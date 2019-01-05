using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

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

    public struct Serialization
    {
        public bool CanSave;
        public bool CanLoad;

        private static string[] flagLabels = new string[4]
        {
            "<🕒> Runtime-only",

            "<💾> Save-only", "<📁> Load-only",

            "<💾-📁> Saves & Loads"
        };

        public override string ToString()
        {
            int saves = (CanSave ? 1 : 0);
            int loads = (CanLoad ? 1 : 0);

            return '[' + flagLabels[saves << 1 | loads] + ']';
        }
    }

    [JsonConverter( typeof(ReflectionDeserializer) )]
    public class Security
    {
        public SecurityType Type;
        public string Prefix;

        public Security(string name, string prefix = "")
        {
            Enum.TryParse(name, out Type);
            Prefix = prefix;
        }

        public string Describe(bool displayNone)
        {
            string result = "";

            if (displayNone || Type != SecurityType.None)
                result += '{' + Prefix + Program.GetEnumName(Type) + '}';

            return result;
        }

        public override string ToString()
        {
            return Describe(false);
        }
    }

    [JsonConverter( typeof(ReflectionDeserializer) )]
    public class ReadWriteSecurity
    {
        public Security Read;
        public Security Write;

        public bool Merged => (Read.Type == Write.Type);

        public ReadWriteSecurity(string read, string write)
        {
            Read = new Security(read);
            Write = new Security(write, "✎");
        }

        public string Describe(bool displayNone)
        {
            string result = "";

            string read = Read.Describe(displayNone);
            string write = Write.Describe(displayNone);

            if (read.Length > 0)
                result += read;

            if (Read.Type != Write.Type && write.Length > 0)
                result += ' ' + write;

            return result.Trim();
        }

        public override string ToString()
        {
            return Describe(false);
        }
    }

    public class ReflectionType
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
                result = Program.GetEnumName(Category) + '<' + Name + '>';

            return result;
        }
    }

    public struct Parameter
    {
        public ReflectionType Type;
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

    public class Tags : HashSet<string>
    {
        public Tags(IEnumerable<string> tags = null)
        {
            tags?.ToList().ForEach(tag => Add(tag));
        }

        public override string ToString()
        {
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