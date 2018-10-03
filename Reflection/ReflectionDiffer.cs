using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

            private int stack;
            private List<Diff> children;

            public void AddChild(Diff child)
            {
                if (children == null)
                    children = new List<Diff>();

                if (!children.Contains(child))
                {
                    child.stack++;
                    children.Add(child);
                }
            }

            public override string ToString()
            {
                string result = "";
                for (int i = 0; i < stack; i++)
                    result += '\t';

                string desc;
                if (Type != DiffType.Change)
                {
                    desc = "";
                    if (!Target.StartsWith(Field))
                        desc += Field + ' ';

                    desc += Target;
                }
                else
                {
                    desc = "the " + Field + " of " + Target;
                }

                switch (Type)
                {
                    case DiffType.Add:
                        result += "Added " + desc;
                        break;
                    case DiffType.Change:
                        result += "Changed " + desc;

                        string merged = "from " + From + " to " + To;
                        if (merged.Length < 30)
                            result += " " + merged;
                        else
                            result += Util.NewLine +
                                "\tfrom: " + From + Util.NewLine + 
                                "\t  to: " + To   + Util.NewLine;

                        break;
                    case DiffType.Remove:
                        result += "Removed " + desc;
                        break;
                }

                if (children != null)
                {
                    children.Sort();
                    foreach (Diff child in children)
                        result += Util.NewLine + child.ToString();

                    result += Util.NewLine;
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

                ReadOnlyCollection<string> priority = Util.TypePriority;
                if (priority.Contains(Field) && priority.Contains(diff.Field))
                {
                    int sortByField = Util.TypePriority.IndexOf(Field) - Util.TypePriority.IndexOf(diff.Field);
                    if (sortByField != 0)
                        return sortByField;

                }
                else
                {
                    int sortByField = Field.CompareTo(diff.Field);
                    if (sortByField != 0)
                        return sortByField;

                }

                // Sort by the last word in the target (so that it is sorted by class->member instead of by type)
                string myTarget = (Target.Split(' ').Last());
                string diffTarget = (diff.Target.Split(' ').Last());

                int sortByTarget = myTarget.CompareTo(diffTarget);
                if (sortByTarget != 0)
                    return sortByTarget;

                return 0;
            }
        }

        private List<Diff> results = new List<Diff>();

        private static Dictionary<string,T> createLookupTable<T>(List<T> entries) where T : Descriptor
        {
            Dictionary<string, T> lookup = new Dictionary<string, T>();

            foreach (T entry in entries)
                lookup.Add(entry.Name, entry);

            return lookup;
        }

        private void flagEntireClass(ClassDescriptor classDesc, Func<string, string, bool, Diff> record, bool detailed)
        {
            Diff classDiff = record("Class", classDesc.Describe(detailed), false);

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

            results.Add(classDiff);
        }

        private void flagEntireEnum(EnumDescriptor enumDesc, Func<string, string, bool, Diff> record, bool detailed)
        {
            Diff enumDiff = record("Enum", enumDesc.Describe(detailed), false);

            foreach (EnumItemDescriptor itemDesc in enumDesc.Items)
            {
                Diff itemDiff = record("EnumItem", itemDesc.Describe(detailed), false);
                enumDiff.AddChild(itemDiff);
            }

            results.Add(enumDiff);
        }

        private Diff Added(string field, string target, bool add = true)
        {
            Diff added = new Diff();
            added.Type = DiffType.Add;
            added.Field = field;
            added.Target = target;

            if (add)
            {
                results.Add(added);
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
                results.Add(removed);
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
                results.Add(changed);
            }

            return changed;
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
            results.Clear();

            // Diff Classes
            Dictionary<string, ClassDescriptor> oldClasses = createLookupTable(oldApi.Classes);
            Dictionary<string, ClassDescriptor> newClasses = createLookupTable(newApi.Classes);

            foreach (string className in newClasses.Keys)
            {
                if (!oldClasses.ContainsKey(className))
                {
                    // Add this class.
                    ClassDescriptor classDesc = newClasses[className];
                    flagEntireClass(classDesc, Added, true);
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
                            Added(memberType, newMember.Signature);
                        }
                    }

                    foreach (string memberName in oldMembers.Keys)
                    {
                        MemberDescriptor oldMember = oldMembers[memberName];
                        string memberType = Util.GetEnumName(oldMember.MemberType);

                        if (newMembers.ContainsKey(memberName))
                        {
                            MemberDescriptor newMember = newMembers[memberName];
                            string memberLbl = newMember.Summary;

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
                            Removed(memberType, oldMember.Summary);
                        }
                    }
                }
                else
                {
                    // Remove Old Class
                    flagEntireClass(oldClass, Removed, false);
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
                    flagEntireEnum(newEnum, Added, true);
                }
            }

            foreach (string enumName in oldEnums.Keys)
            {
                EnumDescriptor oldEnum = oldEnums[enumName];
                string enumLbl = oldEnum.Summary;

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
                            Added("EnumItem", item.Signature);
                        }
                    }

                    foreach (var itemName in oldItems.Keys)
                    {
                        EnumItemDescriptor oldItem = oldItems[itemName];
                        string itemLbl = oldItem.Summary;

                        if (newItems.ContainsKey(itemName))
                        {
                            EnumItemDescriptor newItem = newItems[itemName];
                            DiffTags(itemLbl, oldItem.Tags, newItem.Tags);
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
                    flagEntireEnum(oldEnum, Removed, false);
                }
            }

            // Finalize Diff
            results.Sort();

            List<string> compiled = results.Select(diff => diff.ToString()).ToList();
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
                        if (prevLead != "" && !lastLine.EndsWith(Util.NewLine))
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

            return string.Join(Util.NewLine, final.ToArray()).Trim();
        }
    }
}
