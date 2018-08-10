using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Roblox.Reflection
{

    class ReflectionDiffer
    {
        private enum DiffType
        {
            Add,
            Change,
            Remove,
        }

        private struct Diff : IComparable
        {
            public DiffType Type;

            public string Field;
            public string Target;
            public string From;
            public string To;

            private int Stack;
            private List<Diff> Children;

            private static List<string> FieldSortPriority = new List<string>()
            {
                "Class",
                "Property",
                "Function",
                "Event",
                "Callback",
                "Enum",
                "EnumItem"
            };

            public void AddChild(Diff child)
            {
                if (Children == null)
                    Children = new List<Diff>();

                if (!Children.Contains(child))
                {
                    child.Stack++;
                    Children.Add(child);
                }
            }

            private string backtick(string word)
            {
                return '`' + word.Trim() + '`';
            }

            public override string ToString()
            {
                string result = "";
                for (int i = 0; i < Stack; i++)
                    result += '\t';

                switch (Type)
                {
                    case DiffType.Add:
                        result += "Added " + Field + ' ' + backtick(Target);
                        break;
                    case DiffType.Change:
                        string fromDiff = "from " + From;
                        string toDiff = "to " + To;
                        string merged = fromDiff + " " + toDiff;
                        result += "Changed the " + Field + " of " + backtick(Target);

                        if (merged.Length < 30)
                            result += " " + merged;
                        else
                            result += "\n\t" + fromDiff + "\n\t" + toDiff;

                        break;
                    case DiffType.Remove:
                        result += "Removed " + Field + ' ' + backtick(Target);
                        break;
                }

                if (Children != null)
                {
                    Children.Sort();
                    foreach (Diff child in Children)
                        result += "\r\n" + child.ToString();

                    result += "\r\n";
                }

                return result;
            }

            public int CompareTo(object obj)
            {
                if (obj.GetType() != GetType())
                    throw new NotImplementedException("Diff can only be compared with another Diff");

                Diff diff = (Diff)obj;

                int sortByType = Type - diff.Type;
                if (sortByType != 0)
                    return sortByType;

                if (FieldSortPriority.Contains(Field) && FieldSortPriority.Contains(diff.Field))
                {
                    int sortByField = FieldSortPriority.IndexOf(Field) - FieldSortPriority.IndexOf(diff.Field);
                    if (sortByField != 0)
                        return sortByField;
                }
                else
                {
                    int sortByField = Field.CompareTo(diff.Field);
                    if (sortByField != 0)
                        return sortByField;
                }

                int sortByTarget = Target.CompareTo(diff.Target);
                if (sortByTarget != 0)
                    return sortByTarget;

                return 0;
            }
        }

        private List<Diff> Results = new List<Diff>();

        private static Dictionary<string,T> createLookupTable<T>(List<T> entries) where T : Descriptor
        {
            Dictionary<string, T> lookup = new Dictionary<string, T>();

            foreach (T entry in entries)
                lookup.Add(entry.Name, entry);

            return lookup;
        }

        private Diff Added(string field, string target, bool add = true)
        {
            Diff added = new Diff();
            added.Type = DiffType.Add;
            added.Field = field;
            added.Target = target;

            if (add)
            {
                Results.Add(added);
            }

            return added;
        }

        private Diff Removed(string field, string target, bool add = true)
        {
            Diff removed = new Diff();
            removed.Type = DiffType.Remove;
            removed.Field = field;
            removed.Target = target;

            if (add)
            {
                Results.Add(removed);
            }

            return removed;
        }

        private Diff Changed(string field, string target, string from, string to, bool add = true)
        {
            Diff changed = new Diff();
            changed.Type = DiffType.Change;
            changed.Field = field;
            changed.Target = target;
            changed.From = from;
            changed.To = to;

            if (add)
            {
                Results.Add(changed);
            }

            return changed;
        }

        private void FlagEntireClass(ClassDescriptor classDesc, Func<string,string,bool,Diff> record, bool detailed)
        {
            Diff classDiff = record("Class", classDesc.Name, false);

            foreach (PropertyDescriptor propDesc in classDesc.Properties)
            {
                Diff propDiff = record("Property", propDesc.Describe(detailed), false);
                classDiff.AddChild(propDiff);
            }

            foreach (FunctionDescriptor funcDesc in classDesc.Functions)
            {
                Diff funcDiff = record("Function", funcDesc.Describe(detailed), false);
                classDiff.AddChild(funcDiff);
            }

            foreach (CallbackDescriptor callDesc in classDesc.Callbacks)
            {
                Diff callDiff = record("Callback", callDesc.Describe(detailed), false);
                classDiff.AddChild(callDiff);
            }

            foreach (EventDescriptor evntDesc in classDesc.Events)
            {
                Diff evntDiff = record("Event", evntDesc.Describe(detailed), false);
                classDiff.AddChild(evntDiff);
            }

            Results.Add(classDiff);
        }

        private void FlagEntireEnum(EnumDescriptor enumDesc, Func<string,string,bool,Diff> record)
        {
            string enumName = enumDesc.Name;
            Diff enumDiff = record("Enum", enumName, false);

            foreach (EnumItemDescriptor enumItem in enumDesc.Items)
            {
                Diff itemDiff = record("EnumItem", enumName + '.' + enumItem.Name, false);
                enumDiff.AddChild(itemDiff);
            }

            Results.Add(enumDiff);
        }

        private void DiffTags(string target, List<string> oldTags, List<string> newTags)
        {
            foreach (string newTag in newTags)
            {
                if (!oldTags.Contains(newTag))
                {
                    Added("Tag [" + newTag + "] to", target);
                }
            }
            foreach (string oldTag in oldTags)
            {
                if (!newTags.Contains(oldTag))
                {
                    Removed("Tag [" + oldTag + "] from", target);
                }
            }
        }

        private void DiffGeneric(string target, string context, object oldVal, object newVal)
        {
            if (oldVal.ToString() != newVal.ToString())
                Changed(context, target, oldVal.ToString(), newVal.ToString());
        }

        private void DiffNativeEnum<T>(string target, string context, T oldEnum, T newEnum)
        {
            string oldLbl = Util.GetEnumName(oldEnum);
            string newLbl = Util.GetEnumName(newEnum);
            DiffGeneric(target, context, oldLbl, newLbl);
        }

        private void DiffParameters(string target, List<Parameter> oldParams, List<Parameter> newParams)
        {
            string oldParamSig = Util.GetParamSignature(oldParams);
            string newParamSig = Util.GetParamSignature(newParams); 
            DiffGeneric(target, "parameters", oldParamSig, newParamSig);
        }

        public string CompareDatabases(ReflectionDatabase oldApi, ReflectionDatabase newApi)
        {
            Results.Clear();

            // Diff Classes
            Dictionary<string, ClassDescriptor> oldClasses = createLookupTable(oldApi.Classes);
            Dictionary<string, ClassDescriptor> newClasses = createLookupTable(newApi.Classes);

            foreach (string className in newClasses.Keys)
            {
                if (!oldClasses.ContainsKey(className))
                {
                    // Add this class.
                    ClassDescriptor classDesc = newClasses[className];
                    FlagEntireClass(classDesc, Added, true);
                }
            }

            foreach (string className in oldClasses.Keys)
            {
                ClassDescriptor oldClass = oldClasses[className];
                string classLbl = oldClass.ToString();

                if (newClasses.ContainsKey(className))
                {
                    ClassDescriptor newClass = newClasses[className];

                    DiffTags(classLbl, oldClass.Tags, newClass.Tags);
                    DiffGeneric(classLbl, "superclass", oldClass.Superclass, newClass.Superclass);
                    DiffNativeEnum(classLbl, "memory category", oldClass.MemoryCategory, newClass.MemoryCategory);

                    // Diff the members
                    Dictionary<string, MemberDescriptor> oldMembers = createLookupTable(oldClass.Members);
                    Dictionary<string, MemberDescriptor> newMembers = createLookupTable(newClass.Members);

                    foreach (string memberName in newMembers.Keys)
                    {
                        if (!oldMembers.ContainsKey(memberName))
                        {
                            // Add New Member
                            MemberDescriptor newMember = newMembers[memberName];
                            string memberType = Util.GetEnumName(newMember.MemberType);
                            Added(memberType, newMember.Describe(true));
                        }
                    }

                    foreach (string memberName in oldMembers.Keys)
                    {
                        MemberDescriptor oldMember = oldMembers[memberName];
                        string memberType = Util.GetEnumName(oldMember.MemberType);

                        if (newMembers.ContainsKey(memberName))
                        {
                            MemberDescriptor newMember = newMembers[memberName];
                            string memberLbl = memberType + ' ' + newMember.Describe();

                            // Diff Tags
                            DiffTags(memberLbl, oldMember.Tags, newMember.Tags);

                            // Diff Specific Member Types
                            if (newMember is PropertyDescriptor)
                            {
                                PropertyDescriptor oldProp = oldMember as PropertyDescriptor;
                                PropertyDescriptor newProp = newMember as PropertyDescriptor;

                                DiffGeneric(memberLbl, "category", oldProp.Category, newProp.Category);
                                DiffNativeEnum(memberLbl, "read permissions", oldProp.Security.Read, newProp.Security.Read);
                                DiffNativeEnum(memberLbl, "write permissions", oldProp.Security.Write, newProp.Security.Write);
                                DiffGeneric(memberLbl, "serialization", oldProp.Serialization.ToString(), newProp.Serialization.ToString());
                                DiffGeneric(memberLbl, "value type", oldProp.ValueType.Name, newProp.ValueType.Name);
                            }
                            else if (newMember is FunctionDescriptor)
                            {
                                FunctionDescriptor oldFunc = oldMember as FunctionDescriptor;
                                FunctionDescriptor newFunc = newMember as FunctionDescriptor;

                                DiffNativeEnum(memberLbl, "permissions", oldFunc.Security, newFunc.Security);
                                DiffGeneric(memberLbl, "return type", oldFunc.ReturnType.Name, newFunc.ReturnType.Name);
                                DiffParameters(memberLbl, oldFunc.Parameters, newFunc.Parameters);
                            }
                            else if (newMember is CallbackDescriptor)
                            {
                                CallbackDescriptor oldCall = oldMember as CallbackDescriptor;
                                CallbackDescriptor newCall = newMember as CallbackDescriptor;

                                DiffNativeEnum(memberLbl, "permissions", oldCall.Security, newCall.Security);
                                DiffGeneric(memberLbl, "expected return type", oldCall.ReturnType.Name, newCall.ReturnType.Name);
                                DiffParameters(memberLbl, oldCall.Parameters, newCall.Parameters);
                            }
                            else if (newMember is EventDescriptor)
                            {
                                EventDescriptor oldEvent = oldMember as EventDescriptor;
                                EventDescriptor newEvent = newMember as EventDescriptor;

                                DiffNativeEnum(memberLbl, "permissions", oldEvent.Security, newEvent.Security);
                                DiffParameters(memberLbl, oldEvent.Parameters, newEvent.Parameters);
                            }
                        }
                        else
                        {
                            // Remove Old Member
                            Removed(memberType, oldMember.Describe(false));
                        }
                    }
                }
                else
                {
                    // Remove Old Class
                    FlagEntireClass(oldClass, Removed, false);
                }
            }

            // Diff Enums
            Dictionary<string, EnumDescriptor> oldEnums = createLookupTable(oldApi.Enums);
            Dictionary<string, EnumDescriptor> newEnums = createLookupTable(newApi.Enums);

            foreach (string enumName in newEnums.Keys)
            {
                if (!oldEnums.ContainsKey(enumName))
                {
                    // Add New Enum
                    EnumDescriptor newEnum = newEnums[enumName];
                    FlagEntireEnum(newEnum, Added);
                }
            }

            foreach (string enumName in oldEnums.Keys)
            {
                EnumDescriptor oldEnum = oldEnums[enumName];
                string enumLbl = "Enum " + enumName;

                if (newEnums.ContainsKey(enumName))
                {
                    EnumDescriptor newEnum = newEnums[enumName];

                    // Diff Tags
                    DiffTags(enumLbl, oldEnum.Tags, newEnum.Tags);

                    // Diff Items
                    Dictionary<string, EnumItemDescriptor> oldItems = createLookupTable(oldEnum.Items);
                    Dictionary<string, EnumItemDescriptor> newItems = createLookupTable(newEnum.Items);

                    foreach (var itemName in newItems.Keys)
                    {
                        if (!oldItems.ContainsKey(itemName))
                        {
                            // Add New EnumItem
                            EnumItemDescriptor item = newItems[itemName];
                            Added("EnumItem", enumName + '.' + itemName);
                        }
                    }

                    foreach (var itemName in oldItems.Keys)
                    {
                        string itemLbl = enumName + '.' + itemName;
                        EnumItemDescriptor oldItem = oldItems[itemName];

                        if (newItems.ContainsKey(itemName))
                        {
                            EnumItemDescriptor newItem = newItems[itemName];
                            itemLbl = "EnumItem " + itemLbl;

                            // Diff Tags
                            DiffTags(itemLbl, oldItem.Tags, newItem.Tags);

                            // Diff Values
                            DiffGeneric(itemLbl, "value", oldItem.Value, newItem.Value);
                        }
                        else
                        {
                            // Remove Old EnumItem
                            Removed("EnumItem", itemLbl);
                        }
                    }
                }
                else
                {
                    // Remove Old Enum
                    FlagEntireEnum(oldEnum, Removed);
                }
            }

            // Finalize Diff
            Results.Sort();

            List<string> compiled = Results.Select(diff => diff.ToString()).ToList();
            List<string> final = new List<string>();
            string prevLead = "";
            string lastLine = "";

            foreach (string line in compiled)
            {
                string[] words = line.Split(' ');
                if (words.Length >= 2)
                {
                    string lead = (words[0] + ' ' + words[1]).Trim();

                    if (lead != prevLead)
                    {
                        if (prevLead != "" && !lastLine.EndsWith("\r\n"))
                        {
                            // Add a break between this line and the previous.
                            // This will make things easier to read.
                            final.Add("");
                        }
                        
                        prevLead = lead;
                    }

                    final.Add(line);
                    lastLine = line;
                }
            }

            return string.Join("\r\n", final.ToArray()).Trim();
        }
    }
}
