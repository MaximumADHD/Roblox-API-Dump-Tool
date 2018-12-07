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
            public Descriptor Target;

            public string From;
            public string To;

            private int stack;
            private List<Diff> children;

            public bool HasParent => (stack > 0);
            public bool Detailed = false;
            public bool Merged = false;

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

                string what = Target.Describe(Detailed);
                
                if (Type != DiffType.Change)
                    what = (what.StartsWith(Field) ? "" : Field + ' ') + what;
                else
                    what = "the " + Field + " of " + what;

                switch (Type)
                {
                    case DiffType.Add:
                        result += "Added " + what;
                        break;
                    case DiffType.Change:
                        result += "Changed " + what;

                        string merged = "from " + From + " to " + To;
                        if (merged.Length < 24)
                            result += " " + merged;
                        else
                            result += ' ' + Util.NewLine +
                                "\tfrom: " + From + Util.NewLine + 
                                "\t  to: " + To   + Util.NewLine;

                        break;
                    case DiffType.Remove:
                        result += "Removed " + what;
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

                Diff diff = obj as Diff;

                // Try sorting by the type of diff.
                int sortByType = Type - diff.Type;
                if (sortByType != 0)
                    return sortByType;

                // Try sorting by the field priority.
                ReadOnlyCollection<string> priority = Util.TypePriority;
                int sortByField = 0;
                
                if (priority.Contains(Field) && priority.Contains(diff.Field))
                    sortByField = priority.IndexOf(Field) - priority.IndexOf(diff.Field);
                else
                    sortByField = Field.CompareTo(diff.Field);
                
                if (sortByField != 0)
                    return sortByField;

                // Try sorting by the targets.
                int sortByTarget = Target.CompareTo(diff.Target);
                if (sortByTarget != 0)
                    return sortByTarget;

                // These are identical?
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

        private void flagEntireClass(ClassDescriptor classDesc, Func<string, Descriptor, bool, Diff, Diff> record, bool detailed)
        {
            Diff classDiff = record("Class", classDesc, detailed, null);

            foreach (MemberDescriptor memberDesc in classDesc.Members)
            {
                string memberType = Util.GetEnumName(memberDesc.MemberType);
                record(memberType, memberDesc, detailed, classDiff);
            }
        }

        private void flagEntireEnum(EnumDescriptor enumDesc, Func<string, Descriptor, bool, Diff, Diff> record, bool detailed)
        {
            Diff enumDiff = record("Enum", enumDesc, detailed, null);

            foreach (EnumItemDescriptor itemDesc in enumDesc.Items)
            {
                record("EnumItem", itemDesc, detailed, enumDiff);
            }
        }

        private Diff Added(string field, Descriptor target, bool detailed = true, Diff parent = null)
        {
            Diff added = new Diff();
            added.Type = DiffType.Add;
            added.Detailed = detailed;

            added.Field = field;
            added.Target = target;
            
            if (parent != null)
                parent.AddChild(added);
            else
                results.Add(added);

            return added;
        }

        private Diff Removed(string field, Descriptor target, bool detailed = false, Diff parent = null)
        {
            Diff removed = new Diff();
            removed.Type = DiffType.Remove;
            removed.Detailed = detailed;

            removed.Field = field;
            removed.Target = target;

            if (parent != null)
                parent.AddChild(removed);
            else
                results.Add(removed);

            return removed;
        }

        private Diff Changed(string field, Descriptor target, string from, string to)
        {
            Diff changed = new Diff();
            changed.Type = DiffType.Change;
            changed.Detailed = false;

            changed.Field = field;
            changed.Target = target;

            changed.From = from;
            changed.To = to;

            results.Add(changed);
            return changed;
        }

        private Dictionary<string, Diff> DiffTags(Descriptor target, List<string> oldTags, List<string> newTags)
        {
            var tagChanges = new Dictionary<string, Diff>();

            // Record tags that were added.
            List<string> addTags = newTags.Except(oldTags).ToList();

            if (addTags.Count > 0)
            {
                string signature = Util.DescribeTags(addTags, true);
                Diff diffAdd = Added(signature + " to", target, false);
                tagChanges.Add('+' + signature, diffAdd);
            }

            // Record tags that were removed.
            List<string> removeTags = oldTags.Except(newTags).ToList();

            if (removeTags.Count > 0)
            {
                string signature = Util.DescribeTags(removeTags, true);
                Diff diffRemove = Removed(signature + " from", target);
                tagChanges.Add('-' + signature, diffRemove);
            }

            return tagChanges;
        }

        private void DiffGeneric(Descriptor target, string context, object oldVal, object newVal)
        {
            if (oldVal.ToString() != newVal.ToString())
            {
                Changed(context, target, oldVal.ToString(), newVal.ToString());
            }
        }

        private void DiffNativeEnum<T>(Descriptor target, string context, T oldEnum, T newEnum)
        {
            string oldLbl = '{' + Util.GetEnumName(oldEnum) + '}';
            string newLbl = '{' + Util.GetEnumName(newEnum) + '}';
            DiffGeneric(target, context, oldLbl, newLbl);
        }

        private void DiffParameters(Descriptor target, List<Parameter> oldParams, List<Parameter> newParams)
        {
            string oldParamSig = Util.DescribeParameters(oldParams);
            string newParamSig = Util.DescribeParameters(newParams); 
            DiffGeneric(target, "parameters", oldParamSig, newParamSig);
        }

        public string CompareDatabases(ReflectionDatabase oldApi, ReflectionDatabase newApi)
        {
            results.Clear();

            // Diff Classes
            var oldClasses = createLookupTable(oldApi.Classes);
            var newClasses = createLookupTable(newApi.Classes);

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
                    var classTagDiffs = DiffTags(oldClass, oldClass.Tags, newClass.Tags);

                    DiffGeneric(oldClass, "superclass", oldClass.Superclass, newClass.Superclass);
                    DiffNativeEnum(oldClass, "memory category", oldClass.MemoryCategory, newClass.MemoryCategory);

                    // Diff the members
                    var oldMembers = createLookupTable(oldClass.Members);
                    var newMembers = createLookupTable(newClass.Members);

                    foreach (string memberName in newMembers.Keys)
                    {
                        if (!oldMembers.ContainsKey(memberName))
                        {
                            // Add New Member
                            MemberDescriptor newMember = newMembers[memberName];
                            string memberType = Util.GetEnumName(newMember.MemberType);
                            Added(memberType, newMember);
                        }
                    }

                    foreach (string memberName in oldMembers.Keys)
                    {
                        MemberDescriptor oldMember = oldMembers[memberName];
                        string memberType = Util.GetEnumName(oldMember.MemberType);

                        if (newMembers.ContainsKey(memberName))
                        {
                            MemberDescriptor newMember = newMembers[memberName];

                            // Diff Tags
                            var memberTagDiffs = DiffTags(newMember, oldMember.Tags, newMember.Tags);

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
                                var oldProp = oldMember as PropertyDescriptor;
                                var newProp = newMember as PropertyDescriptor;

                                // If the read and write permissions are both changed to the same value, try to group them.
                                if (oldProp.Security.ToString() != newProp.Security.ToString())
                                {
                                    if (oldProp.Security.ShouldMergeWith(newProp.Security))
                                    {
                                        // Doesn't matter if we read from 'Read' or 'Write' in this case.
                                        // ... so why not both! This outta balance out the pain.

                                        SecurityType oldSecurity = oldProp.Security.Read;
                                        SecurityType newSecurity = newProp.Security.Write;

                                        DiffNativeEnum(newMember, "security", oldSecurity, newSecurity);
                                    }
                                    else
                                    {
                                        DiffNativeEnum(newMember, "read permissions", oldProp.Security.Read, newProp.Security.Read);
                                        DiffNativeEnum(newMember, "write permissions", oldProp.Security.Write, newProp.Security.Write);
                                    }
                                }

                                DiffGeneric(newMember, "serialization", oldProp.Serialization.ToString(), newProp.Serialization.ToString());
                                DiffGeneric(newMember, "value type", oldProp.ValueType.Name, newProp.ValueType.Name);
                            }
                            else if (newMember is FunctionDescriptor)
                            {
                                var oldFunc = oldMember as FunctionDescriptor;
                                var newFunc = newMember as FunctionDescriptor;

                                DiffNativeEnum(newMember, "security", oldFunc.Security, newFunc.Security);
                                DiffGeneric(newMember, "return type", oldFunc.ReturnType.Name, newFunc.ReturnType.Name);
                                DiffParameters(newMember, oldFunc.Parameters, newFunc.Parameters);
                            }
                            else if (newMember is CallbackDescriptor)
                            {
                                var oldCall = oldMember as CallbackDescriptor;
                                var newCall = newMember as CallbackDescriptor;

                                DiffNativeEnum(newMember, "security", oldCall.Security, newCall.Security);
                                DiffGeneric(newMember, "expected return type", oldCall.ReturnType.Name, newCall.ReturnType.Name);
                                DiffParameters(newMember, oldCall.Parameters, newCall.Parameters);
                            }
                            else if (newMember is EventDescriptor)
                            {
                                var oldEvent = oldMember as EventDescriptor;
                                var newEvent = newMember as EventDescriptor;

                                DiffNativeEnum(newMember, "security", oldEvent.Security, newEvent.Security);
                                DiffParameters(newMember, oldEvent.Parameters, newEvent.Parameters);
                            }
                        }
                        else
                        {
                            // Remove Old Member
                            Removed(memberType, oldMember);
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
            var oldEnums = createLookupTable(oldApi.Enums);
            var newEnums = createLookupTable(newApi.Enums);

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

                if (newEnums.ContainsKey(enumName))
                {
                    EnumDescriptor newEnum = newEnums[enumName];

                    // Diff Tags
                    DiffTags(newEnum, oldEnum.Tags, newEnum.Tags);

                    // Diff Items
                    var oldItems = createLookupTable(oldEnum.Items);
                    var newItems = createLookupTable(newEnum.Items);

                    foreach (var itemName in newItems.Keys)
                    {
                        if (!oldItems.ContainsKey(itemName))
                        {
                            // Add New EnumItem
                            EnumItemDescriptor item = newItems[itemName];
                            Added("EnumItem", item);
                        }
                    }

                    foreach (var itemName in oldItems.Keys)
                    {
                        EnumItemDescriptor oldItem = oldItems[itemName];
                        string itemLbl = oldItem.Summary;

                        if (newItems.ContainsKey(itemName))
                        {
                            EnumItemDescriptor newItem = newItems[itemName];
                            DiffTags(newItem, oldItem.Tags, newItem.Tags);
                            DiffGeneric(newItem, "value", oldItem.Value, newItem.Value);
                        }
                        else
                        {
                            // Remove Old EnumItem
                            Removed("EnumItem", oldItem);
                        }
                    }
                }
                else
                {
                    // Remove Old Enum
                    flagEntireEnum(oldEnum, Removed, false);
                }
            }

            // Sort the results
            results.Sort();

            // Select diffs that are not parented to other diffs.
            List<Diff> diffs = results
                .Where(diff => !diff.HasParent)
                .ToList();

            // Merge similar changes
            foreach (Diff diff in diffs)
            {
                if (diff.Type == DiffType.Change && !diff.Merged)
                {
                    List<Diff> similarDiffs = diffs
                        .Where(similar => diff != similar)
                        .Where(similar => diff.Target == similar.Target)
                        .ToList();

                    if (similarDiffs.Count > 0)
                    {
                        foreach (Diff similar in similarDiffs)
                        {
                            if (diff.Field.Contains(" and "))
                                diff.Field = diff.Field.Replace(" and ", ", ");

                            diff.Field += " and " + similar.Field;
                            diff.From += " " + similar.From;
                            diff.To += " " + similar.To;

                            similar.Merged = true;
                        }
                    }
                }
            }

            // Generate the diff logs.
            List<string> final = new List<string>();
            string prevLead = "";
            string lastLine = "";

            List<string> lines = diffs
                .Where(diff => !diff.Merged)
                .Select(diff => diff.ToString())
                .ToList();

            foreach (string line in lines)
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

                    // Add a break between this line and the previous.
                    // This will make things easier to read.
                    if (addBreak)
                        final.Add("");

                    final.Add(line);
                    lastLine = line;
                }
            }

            return string.Join(Util.NewLine, final.ToArray()).Trim();
        }
    }
}