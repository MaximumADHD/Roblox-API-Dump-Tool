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

            "<💾|📁> Saves & Loads"
        };

        public override string ToString()
        {
            // sure would be nice if booleans could be 
            // casted as ints in C# like in C++ lol

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
            Write = new Security(write, "✏️");
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

        public void WriteHtml(ReflectionDumper buffer, int numTabs = 0)
        {
            string typeVal = GetSignature();
            buffer.OpenClassTag("Type", numTabs);

            if (typeVal.Contains("<") && typeVal.EndsWith(">"))
            {
                string category = Program.GetEnumName(Category);
                buffer.Write(category);
                buffer.CloseClassTag();

                buffer.OpenClassTag("InnerType", numTabs);
                buffer.Write(Name);
            }
            else
            {
                buffer.Write(typeVal);
            }

            buffer.CloseClassTag();
        }
    }

    public struct Parameter
    {
        private const string quote = "\"";

        public ReflectionType Type;
        public string Name;
        public string Default;

        public override string ToString()
        {
            string result = Type.ToString() + " " + Name;
            string category = Program.GetEnumName(Type.Category);

            if ((Type.Name == "string" || category == "Enum") && Default != null)
                if (!Default.StartsWith(quote) && !Default.EndsWith(quote))
                    Default = quote + Default + quote;

            if (Default != null && Default.Length > 0)
                result += " = " + Default;

            return result;
        }

        public void WriteHtml(ReflectionDumper buffer, int numTabs = 0)
        {
            buffer.OpenClassTag("Parameter", numTabs);
            buffer.NextLine();

            // Write Type
            Type.WriteHtml(buffer, numTabs + 1);

            // Write Name
            string nameLbl = "ParamName";
            if (Default != null)
                nameLbl += " default";

            buffer.OpenClassTag(nameLbl, numTabs + 1);
            buffer.Write(Name);
            buffer.CloseClassTag();

            // Write Default
            if (Default != null)
            {
                string typeLbl = Type.GetSignature();
                string typeName;

                if (typeLbl.Contains("<") && typeLbl.EndsWith(">"))
                    typeName = Program.GetEnumName(Type.Category);
                else
                    typeName = Type.Name;

                if (typeName == "Enum")
                    typeName = "String";

                buffer.OpenClassTag("ParamDefault " + typeName, numTabs + 1);
                buffer.Write(Default);
                buffer.CloseClassTag();
            }

            buffer.CloseClassTag(numTabs);
        }
    }

    public class Parameters : List<Parameter>
    {
        public override string ToString()
        {
            string[] parameters = this.Select(param => param.ToString()).ToArray();
            return '(' + string.Join(", ", parameters) + ')';
        }

        public void WriteHtml(ReflectionDumper buffer, int numTabs = 0, bool diffMode = false)
        {
            string paramsTag = "Parameters";
            if (diffMode)
                paramsTag += " change";

            buffer.OpenClassTag(paramsTag, numTabs);

            if (Count > 0)
            {
                buffer.NextLine();

                foreach (Parameter parameter in this)
                    parameter.WriteHtml(buffer, numTabs + 1);

                buffer.CloseClassTag(numTabs);
            }
            else
            {
                buffer.CloseClassTag();
            }
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

        public void WriteHtml(ReflectionDumper buffer, int numTabs = 0)
        {
            foreach (string tag in this)
            {
                buffer.OpenClassTag("Tag", numTabs);
                buffer.Write('[' + tag + ']');
                buffer.CloseClassTag();
            }
        }
    }
}