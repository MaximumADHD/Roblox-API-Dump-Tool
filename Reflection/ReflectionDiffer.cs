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

        private class Diff : IComparable
        {
            public DiffType Type;

            public string Field;
            public string Target;
            public string From;
            public string To;

            private int stack;
            private List<Diff> children;

            public bool HasParent => (stack > 0);

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
                        if (merged.Length < 36)
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
                int sortByField = 0;
                
                if (priority.Contains(Field) && priority.Contains(diff.Field))
                    sortByField = priority.IndexOf(Field) - priority.IndexOf(diff.Field);
                else
                    sortByField = Field.CompareTo(diff.Field);
                
                if (sortByField != 0)
                    return sortByField;
                
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

        private void flagEntireClass(ClassDescriptor classDesc, Func<string, string, Diff, Diff> record, bool detailed)
        {
            Diff classDiff = record("Class", classDesc.Describe(detailed), null);

            foreach (PropertyDescriptor propDesc in classDesc.Properties)
                record("Property", propDesc.Describe(detailed), classDiff);

            foreach (FunctionDescriptor funcDesc in classDesc.Functions)
                record("Function", funcDesc.Describe(detailed), classDiff);

            foreach (CallbackDescriptor callDesc in classDesc.Callbacks)
                record("Callback", callDesc.Describe(detailed), classDiff);

            foreach (EventDescriptor evntDesc in classDesc.Events)
                record("Event", evntDesc.Describe(detailed), classDiff);

        }

        private void flagEntireEnum(EnumDescriptor enumDesc, Func<string, string, Diff, Diff> record, bool detailed)
        {
            Diff enumDiff = record("Enum", enumDesc.Describe(detailed), null);

            foreach (EnumItemDescriptor itemDesc in enumDesc.Items)
                record("EnumItem", itemDesc.Describe(detailed), enumDiff);
        }

        private Diff Added(string field, string target, Diff parent = null)
        {
            Diff added = new Diff();
            added.Type = DiffType.Add;
            added.Field = field;
            added.Target = target;

            if (parent != null)
                parent.AddChild(added);
            else
                results.Add(added);

            return added;
        }

        private Diff Removed(string field, string target, Diff parent = null)
        {
            Diff removed = new Diff();
            removed.Type = DiffType.Remove;
            removed.Field = field;
            removed.Target = target;

            if (parent != null)
                parent.AddChild(removed);
            else
                results.Add(removed);

            return removed;
        }

        private Diff Changed(string field, string target, string from, string to)
        {
            Diff changed = new Diff();
            changed.Type = DiffType.Change;
            changed.Field = field;
            changed.Target = target;
            changed.From = from;
            changed.To = to;

            results.Add(changed);
            return changed;
        }

        private Dictionary<string, Diff> DiffTags(string target, List<string> oldTags, List<string> newTags)
        {
            var tagChanges = new Dictionary<string, Diff>();

            // Record tags that were added.
            var addTags = newTags.Except(oldTags).ToList();
            if (addTags.Count > 0)
            {
                string signature = Util.GetTagSignature(addTags, true);
                Diff diffAdd = Added(signature + " to", target);
                tagChanges.Add('+' + signature, diffAdd);
            }

            // Record tags that were removed.
            var removeTags = oldTags.Except(newTags).ToList();
            if (removeTags.Count > 0)
            {
                string signature = Util.GetTagSignature(removeTags, true);
                Diff diffRemove = Removed(signature + " from", target);
                tagChanges.Add('-' + signature, diffRemove);
            }

            return tagChanges;
        }

        private void DiffGeneric(string target, string context, object oldVal, object newVal)
        {
            if (oldVal.ToString() != newVal.ToString())
                Changed(context, target, oldVal.ToString(), newVal.ToString());
        }

        private void DiffNativeEnum<T>(string target, string context, T oldEnum, T newEnum)
        {
            string oldLbl = '{' + Util.GetEnumName(oldEnum) + '}';
            string newLbl = '{' + Util.GetEnumName(newEnum) + '}';
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
                    var classTagDiffs = DiffTags(classLbl, oldClass.Tags, newClass.Tags);

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
                            var memberTagDiffs = DiffTags(memberLbl, oldMember.Tags, newMember.Tags);

                            // Check if any tags that were added to this member were
                            // also added to its parent class.
                            foreach (string classTag in classTagDiffs.Keys)
                            {
                                if (memberTagDiffs.ContainsKey(classTag))
                                {
                                    Diff classDiff = classTagDiffs[classTag];
                                    Diff memberDiff = memberTagDiffs[classTag];
                                    classDiff.AddChild(memberDiff);
                                }
                            }

                            // Diff Specific Member Types
                            if (newMember is PropertyDescriptor)
                            {
                                PropertyDescriptor oldProp = oldMember as PropertyDescriptor;
                                PropertyDescriptor newProp = newMember as PropertyDescriptor;

                                // If the read and write permissions are both changed to the same value, try to group them.
                                if (oldProp.Security.ToString() != newProp.Security.ToString())
                                {
                                    if (oldProp.Security.ShouldMergeWith(newProp.Security))
                                    {
                                        // Doesn't matter if we read from 'Read' or 'Write' in this case.
                                        SecurityType oldSecurity = oldProp.Security.Read;
                                        SecurityType newSecurity = newProp.Security.Read;

                                        DiffNativeEnum(memberLbl, "security", oldSecurity, newSecurity);
                                    }
                                    else
                                    {
                                        DiffNativeEnum(memberLbl, "read permissions", oldProp.Security.Read, newProp.Security.Read);
                                        DiffNativeEnum(memberLbl, "write permissions", oldProp.Security.Write, newProp.Security.Write);
                                    }
                                }


                                DiffGeneric(memberLbl, "serialization", oldProp.Serialization.ToString(), newProp.Serialization.ToString());
                                DiffGeneric(memberLbl, "value type", oldProp.ValueType.Name, newProp.ValueType.Name);
                            }
                            else if (newMember is FunctionDescriptor)
                            {
                                FunctionDescriptor oldFunc = oldMember as FunctionDescriptor;
                                FunctionDescriptor newFunc = newMember as FunctionDescriptor;

                                DiffNativeEnum(memberLbl, "security", oldFunc.Security, newFunc.Security);
                                DiffGeneric(memberLbl, "return type", oldFunc.ReturnType.Name, newFunc.ReturnType.Name);
                                DiffParameters(memberLbl, oldFunc.Parameters, newFunc.Parameters);
                            }
                            else if (newMember is CallbackDescriptor)
                            {
                                CallbackDescriptor oldCall = oldMember as CallbackDescriptor;
                                CallbackDescriptor newCall = newMember as CallbackDescriptor;

                                DiffNativeEnum(memberLbl, "security", oldCall.Security, newCall.Security);
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

            List<string> diffs = results
                .Where(diff => !diff.HasParent)
                .Select(diff => diff.ToString())
                .ToList();

            List<string> final = new List<string>();
            string prevLead = "";
            string lastLine = "";

            foreach (string line in diffs)
            {
                string[] words = line.Split(' ');
                if (words.Length >= 2)
                {
                    // Capture the first two words in this line.
                    string first = words[0];
                    string second = words[1];

                    if (second.ToLower() == "the" && words.Length > 3)
                        second = words[2];

                    string lead = (first + ' ' + second).Trim();

                    bool addBreak = false;
                    bool lastLineNoBreak = !lastLine.EndsWith(Util.NewLine);

                    // If the first two words of this line aren't the same as the last...
                    if (lead != prevLead)
                    {
                        // Add a break if the last line doesn't have a break.
                        // (and if there actually were two previous words)
                        if (prevLead != "" && lastLineNoBreak)
                            addBreak = true;

                        prevLead = lead;
                    }

                    // If we didn't add a break, but this line has a break and the
                    // previous line doesn't, then we will add a break.
                    if (!addBreak && lastLineNoBreak && line.EndsWith(Util.NewLine))
                        addBreak = true;

                    if (addBreak)
                    {
                        // Add a break between this line and the previous.
                        // This will make things easier to read.
                        final.Add("");
                    }

                    final.Add(line);
                    lastLine = line;
                }
            }

            return string.Join(Util.NewLine, final.ToArray()).Trim();
        }
    }
}
