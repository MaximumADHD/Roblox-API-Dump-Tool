using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace RobloxApiDumpTool
{
    public sealed class ClassDescriptor : Descriptor
    {
        private int inheritLevel = -1;
        
        public string Superclass;
        public string MemoryCategory;
        public SecurityType Security = SecurityType.None;

        public ReflectionDatabase Database;
        
        [JsonIgnore]
        public List<MemberDescriptor> Members = new List<MemberDescriptor>();

        public int InheritanceLevel
        {
            get
            {
                if (inheritLevel < 0 && Database != null)
                {
                    if (Database.Classes.ContainsKey(Superclass))
                    {
                        ClassDescriptor parentClass = Database.Classes[Superclass];

                        if (parentClass.InheritanceLevel >= 0)
                        {
                            // Set the inheritance level to the parent's level + 1
                            inheritLevel = parentClass.InheritanceLevel + 1;
                        }
                    }
                    else if (Superclass == "<<<ROOT>>>")
                    {
                        // This is the top level class
                        inheritLevel = 0;
                    }
                }

                return inheritLevel;
            }
        }

        public bool IsAncestorOf(ClassDescriptor desc)
        {
            if (Database != desc.Database)
                return false;

            var classes = Database.Classes;

            while (desc.InheritanceLevel >= InheritanceLevel)
            {
                string superClass = desc.Superclass;

                if (!classes.ContainsKey(superClass))
                    break;

                if (Name == superClass)
                    return true;

                desc = classes[superClass];
            }

            return false;
        }

        public override string GetSchema(bool detailed = false)
        {
            string schema = base.GetSchema();

            if (detailed)
                schema += " : {Superclass} {Security} {Tags}";

            return schema;
        }

        public override Dictionary<string, object> GetTokens(bool detailed = false)
        {
            var tokens = base.GetTokens(detailed);

            if (detailed)
                tokens.Add("Superclass", Superclass);

            return tokens;
        }

        public int CompareTo(ClassDescriptor otherClass, bool compareInheritance = false)
        {
            if (compareInheritance && InheritanceLevel != otherClass.InheritanceLevel)
            {
                int diff = InheritanceLevel - otherClass.InheritanceLevel;
                return Math.Sign(diff);
            }
            
            return base.CompareTo(otherClass);
        }

        public override int CompareTo(object other)
        {
            int result;

            if (other is ClassDescriptor otherClass)
                result = CompareTo(otherClass, true);
            else
                result = base.CompareTo(other);

            return result;
        }
    }
}