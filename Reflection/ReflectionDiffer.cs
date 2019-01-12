using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Roblox.Reflection
{
    public class ReflectionDiffer
    {
        private const string NL = "\r\n";
        private const string HTML_BREAK = NL + "<br/>" + NL;

        private List<Diff> results = new List<Diff>();

        private enum DiffType
        {
            Add,
            Change,
            Remove,
        }

        public static ReadOnlyCollection<string> TypePriority = new ReadOnlyCollection<string>(new string[]
        {
            "Class",
            "Property",
            "Function",
            "Event",
            "Callback",
            "Enum",
            "EnumItem"
        });
        
        private delegate Diff DiffRecorder(Descriptor target, bool detailed = true, Diff parent = null);
        private delegate void DiffResultLineAdder(string line, bool addBreak);
        private delegate string DiffResultFinalizer();
        
        private class ChangeList : List<object>
        {
            public string Name { get; private set; }

            public ChangeList(string name)
            {
                Name = name;
            }

            public override string ToString()
            {
                string[] elements = this.Select(elem => elem.ToString()).ToArray();
                return string.Join(" ", elements);
            }

            public void WriteHtml(ReflectionDumper buffer, bool multiline = false)
            {
                int numTabs;

                if (multiline)
                {
                    buffer.OpenClassTag(Name, 1, "div");
                    buffer.NextLine();

                    buffer.OpenClassTag("ChangeList", 2);
                    numTabs = 3;
                }
                else
                {
                    buffer.OpenClassTag(Name, 1);
                    numTabs = 2;
                }

                buffer.NextLine();

                foreach (object change in this)
                {
                    if (change is Parameters)
                    {
                        var parameters = change as Parameters;
                        parameters.WriteHtml(buffer, numTabs, true);
                    }
                    else if (change is ReflectionType)
                    {
                        var type = change as ReflectionType;
                        type.WriteHtml(buffer, numTabs);
                    }
                    else
                    {
                        string value;

                        if (change is Security)
                        {
                            var security = change as Security;
                            value = security.Describe(true);
                        }
                        else
                        {
                            value = change.ToString();
                        }

                        string tagClass;

                        if (value.StartsWith("{") && value.EndsWith("}"))
                            tagClass = "Security";
                        else if (value.StartsWith("[") && value.EndsWith("]"))
                            tagClass = "Serialization";
                        else if (value.StartsWith("\"") && value.EndsWith("\""))
                            tagClass = "String";
                        else
                            tagClass = change.GetType().Name;

                        buffer.OpenClassTag(tagClass, numTabs);
                        buffer.Write(value);
                        buffer.CloseClassTag();
                    }
                }

                buffer.CloseClassTag(numTabs - 1);
                
                if (multiline)
                {
                    buffer.CloseClassTag(1, "div");
                }
            }
        }

        private class Diff : IComparable
        {
            public DiffType Type;

            public string Field;
            public object Context;

            public Descriptor Target;

            public ChangeList From = new ChangeList("ChangeFrom");
            public ChangeList To = new ChangeList("ChangeTo");

            public bool HasParent => (stack > 0);

            public bool Detailed;
            public bool Processed;

            private int stack;
            private List<Diff> children = new List<Diff>();

            public void AddChild(Diff child)
            {
                if (!children.Contains(child))
                {
                    child.stack++;
                    children.Add(child);
                }
            }

            public string WriteDiffTxt(bool detailed = false)
            {
                string result = "";
                for (int i = 0; i < stack; i++)
                    result += '\t';

                string what = Target.Describe(detailed);

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

                        string from = From.ToString();
                        string to = To.ToString();

                        string merged = "from " + from + " to " + to;
                        if (merged.Length < 24)
                            result += " " + merged;
                        else
                            result += ' ' + NL +
                                "\tfrom: " + from + NL +
                                "\t  to: " + to + NL;

                        break;
                    case DiffType.Remove:
                        result += "Removed " + what;
                        break;
                }

                if (children.Count > 0)
                {
                    children.Sort();

                    foreach (Diff child in children)
                    {
                        result += NL;
                        result += child.WriteDiffTxt(detailed);
                    };

                    result += NL;
                }

                return result;
            }

            public override string ToString()
            {
                return WriteDiffTxt(Detailed);
            }
            
            public void WriteDiffHtml(ReflectionDumper buffer)
            {
                string diffType = Program.GetEnumName(Type);

                if (Type == DiffType.Add)
                    diffType += "e";
                
                diffType += "d";

                if (HasParent)
                    diffType += " child";

                buffer.OpenClassTag(diffType, stack, "div");
                buffer.NextLine();

                if (Type == DiffType.Change)
                {
                    // Check if we should keep this on one line, based on the text version.
                    string textSignature = WriteDiffTxt();
                    bool multiline = textSignature.Contains(NL);

                    // Write what we changed.
                    buffer.OpenClassTag("WhatChanged", stack + 1);
                    buffer.Write(Field);
                    buffer.CloseClassTag();

                    // Write what was changed.
                    Target.WriteHtml(buffer, stack + 1, false, true);

                    // Changed From, Changed To.
                    From.WriteHtml(buffer, multiline);
                    To.WriteHtml(buffer, multiline);
                }
                else
                {
                    string descType = Target.GetDescriptorType();
                    bool detailed = (Type == DiffType.Add);

                    if (Field != descType)
                    {
                        if (Context != null && Context is Tags)
                        {
                            Tags tags = Context as Tags;
                            string tagClass = "TagChange";

                            if (tags.Count == 1)
                                tagClass += " singular";

                            if (Type == DiffType.Add)
                                tagClass += " to";
                            else
                                tagClass += " from";

                            buffer.OpenClassTag(tagClass, stack + 1);
                            buffer.NextLine();

                            tags.WriteHtml(buffer, stack + 2);
                            buffer.CloseClassTag(stack + 1);

                            detailed = false;
                        }
                        else
                        {
                            buffer.OpenClassTag("Field", stack + 1);
                            buffer.Write(Field);
                            buffer.CloseClassTag();
                        }
                    }

                    buffer.OpenClassTag("Target", stack + 1);
                    buffer.NextLine();

                    Target.WriteHtml(buffer, stack + 2, detailed, true);
                    buffer.CloseClassTag(stack + 1);
                }
                
                if (children.Count > 0)
                {
                    children.Sort();
                    children.ForEach(child => child.WriteDiffHtml(buffer));
                }

                buffer.CloseClassTag(stack, "div");
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
                int sortByField = 0;
                
                if (TypePriority.Contains(Field) && TypePriority.Contains(diff.Field))
                    sortByField = TypePriority.IndexOf(Field) - TypePriority.IndexOf(diff.Field);
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


        private static Dictionary<string, T> createLookupTable<T>(List<T> entries) where T : Descriptor
        {
            return entries.ToDictionary(entry => entry.Name);
        }

        private void flagEntireClass(ClassDescriptor classDesc, DiffRecorder record, bool detailed)
        {
            Diff classDiff = record(classDesc, detailed);
            classDesc.Members.ForEach(memberDesc => record(memberDesc, detailed, classDiff));
        }

        private void flagEntireEnum(EnumDescriptor enumDesc, DiffRecorder record, bool detailed)
        {
            Diff enumDiff = record(enumDesc, detailed);
            enumDesc.Items.ForEach(itemDesc => record(itemDesc, detailed, enumDiff));
        }

        private Diff Added(string field, Descriptor target, bool detailed = true, Diff parent = null)
        {
            Diff added = new Diff()
            {
                Type = DiffType.Add,
                Detailed = detailed,

                Field = field,
                Target = target
            };

            if (parent != null)
                parent.AddChild(added);
            else
                results.Add(added);

            return added;
        }

        private Diff Added(Descriptor target, bool detailed = true, Diff parent = null)
        {
            string descType = target.GetDescriptorType();
            return Added(descType, target, detailed, parent);
        }

        private Diff Removed(string field, Descriptor target, bool detailed = false, Diff parent = null)
        {
            Diff removed = new Diff()
            {
                Type = DiffType.Remove,
                Detailed = detailed,

                Field = field,
                Target = target,
            };

            if (parent != null)
                parent.AddChild(removed);
            else
                results.Add(removed);

            return removed;
        }

        private Diff Removed(Descriptor target, bool detailed = true, Diff parent = null)
        {
            string descType = target.GetDescriptorType();
            return Removed(descType, target, detailed, parent);
        }

        private Diff Changed(string field, Descriptor target, object from, object to)
        {
            Diff changed = new Diff()
            {
                Type = DiffType.Change,
                Detailed = false,

                Field = field,
                Target = target,

                From = { from },
                To = { to }
            };

            results.Add(changed);
            return changed;
        }

        private Dictionary<string, Diff> DiffTags(Descriptor target, Tags oldTags, Tags newTags)
        {
            var tagChanges = new Dictionary<string, Diff>();

            // Record tags that were added.
            Tags addTags = new Tags(newTags.Except(oldTags));

            if (addTags.Count > 0)
            {
                string signature = addTags.Signature;

                Diff diffAdd = Added(signature + " to", target, false);
                diffAdd.Context = addTags;

                tagChanges.Add('+' + signature, diffAdd);
            }

            // Record tags that were removed.
            Tags removeTags = new Tags(oldTags.Except(newTags));

            if (removeTags.Count > 0)
            {
                string signature = removeTags.Signature;

                Diff diffRemove = Removed(signature + " from", target);
                diffRemove.Context = removeTags;

                tagChanges.Add('-' + signature, diffRemove);
            }

            return tagChanges;
        }

        private void DiffGeneric(Descriptor target, string context, object oldVal, object newVal)
        {
            if (oldVal.ToString() != newVal.ToString())
            {
                Changed(context, target, oldVal, newVal);
            }
        }

        private void DiffNativeEnum<T>(Descriptor target, string context, T oldEnum, T newEnum)
        {
            string oldLbl = '{' + Program.GetEnumName(oldEnum) + '}';
            string newLbl = '{' + Program.GetEnumName(newEnum) + '}';
            DiffGeneric(target, context, oldLbl, newLbl);
        }
        
        public string CompareDatabases(ReflectionDatabase oldApi, ReflectionDatabase newApi, string format = "TXT")
        {
            results.Clear();

            // Diff Classes
            var oldClasses = createLookupTable(oldApi.Classes);
            var newClasses = createLookupTable(newApi.Classes);

            foreach (string className in newClasses.Keys)
            {
                if (!oldClasses.ContainsKey(className))
                {
                    // Add New Class
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
                            Added(newMember);
                        }
                    }

                    foreach (string memberName in oldMembers.Keys)
                    {
                        MemberDescriptor oldMember = oldMembers[memberName];
                        string memberType = Program.GetEnumName(oldMember.MemberType);

                        if (newMembers.ContainsKey(memberName))
                        {
                            MemberDescriptor newMember = newMembers[memberName];
                            var memberTagDiffs = DiffTags(newMember, oldMember.Tags, newMember.Tags);

                            // Check if any tags that were added to this member
                            // were also added to its parent class.
                            foreach (string classTag in classTagDiffs.Keys)
                            {
                                if (memberTagDiffs.ContainsKey(classTag))
                                {
                                    Diff classDiff = classTagDiffs[classTag];
                                    Diff memberDiff = memberTagDiffs[classTag];
                                    classDiff.AddChild(memberDiff);
                                }
                            }

                            // Diff specific member types
                            if (newMember is PropertyDescriptor)
                            {
                                var oldProp = oldMember as PropertyDescriptor;
                                var newProp = newMember as PropertyDescriptor;

                                // If the read and write permissions are both changed to the same value, try to group them.
                                if (oldProp.Security.Merged && newProp.Security.Merged)
                                {
                                    string oldSecurity = oldProp.Security.Describe(true);
                                    string newSecurity = newProp.Security.Describe(true);
                                    DiffGeneric(newMember, "security", oldSecurity, newSecurity);
                                }
                                else
                                {
                                    ReadWriteSecurity oldSecurity = oldProp.Security;
                                    ReadWriteSecurity newSecurity = newProp.Security;

                                    string oldRead = oldSecurity.Read.Describe(true);
                                    string newRead = newSecurity.Read.Describe(true);
                                    DiffGeneric(newMember, "read permissions", oldRead, newRead);

                                    string oldWrite = oldSecurity.Write.Describe(true);
                                    string newWrite = newSecurity.Write.Describe(true);
                                    DiffGeneric(newMember, "write permissions", oldWrite, newWrite);
                                }

                                DiffGeneric(newMember, "serialization", oldProp.Serialization, newProp.Serialization);
                                DiffGeneric(newMember, "value-type", oldProp.ValueType, newProp.ValueType);
                            }
                            else if (newMember is FunctionDescriptor)
                            {
                                var oldFunc = oldMember as FunctionDescriptor;
                                var newFunc = newMember as FunctionDescriptor;

                                DiffGeneric(newMember, "security", oldFunc.Security, newFunc.Security);
                                DiffGeneric(newMember, "return-type", oldFunc.ReturnType, newFunc.ReturnType);
                                DiffGeneric(newMember, "parameters",  oldFunc.Parameters, newFunc.Parameters);
                            }
                            else if (newMember is CallbackDescriptor)
                            {
                                var oldCall = oldMember as CallbackDescriptor;
                                var newCall = newMember as CallbackDescriptor;

                                DiffGeneric(newMember, "security", oldCall.Security, newCall.Security);
                                DiffGeneric(newMember, "expected return-type", oldCall.ReturnType, newCall.ReturnType);
                                DiffGeneric(newMember, "parameters", oldCall.Parameters, newCall.Parameters);
                            }
                            else if (newMember is EventDescriptor)
                            {
                                var oldEvent = oldMember as EventDescriptor;
                                var newEvent = newMember as EventDescriptor;

                                DiffGeneric(newMember, "security", oldEvent.Security, newEvent.Security);
                                DiffGeneric(newMember, "parameters", oldEvent.Parameters, newEvent.Parameters);
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
                            Added(item);
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
                            Removed(oldItem, false);
                        }
                    }
                }
                else
                {
                    // Remove Old Enum
                    flagEntireEnum(oldEnum, Removed, false);
                }
            }

            // Select diffs that are not parented to other diffs.
            List<Diff> diffs = results
                .Where(diff => !diff.HasParent)
                .ToList();

            // Merge similar changes
            List<Diff> changeDiffs = diffs
                .Where(diff => diff.Type == DiffType.Change)
                .ToList();

            foreach (Diff diff in changeDiffs)
            {
                if (!diff.Processed)
                {
                    List<Diff> similarDiffs = changeDiffs
                        .Where(similar => diff != similar)
                        .Where(similar => diff.Target == similar.Target)
                        .ToList();

                    if (similarDiffs.Count > 0)
                    {
                        foreach (Diff similar in similarDiffs)
                        {
                            diff.Field = diff.Field.Replace(" and ", ", ");
                            diff.Field += " and " + similar.Field;

                            similar.From.ForEach(elem => diff.From.Add(elem));
                            similar.To.ForEach(elem => diff.To.Add(elem));

                            similar.Processed = true;
                        }
                    }
                }
            }

            // Detect changed class names
            List<Diff> memberedClassDiffs = diffs
                .Where(diff => diff.Target is ClassDescriptor)
                .Where(diff => (diff.Target as ClassDescriptor).Members.Count > 0)
                .ToList();

            List<Diff> newClassDiffs = memberedClassDiffs
                .Where(diff => diff.Type == DiffType.Add)
                .ToList();

            List<Diff> oldClassDiffs = memberedClassDiffs
                .Where(diff => diff.Type == DiffType.Remove)
                .ToList();

            if (oldClassDiffs.Count > 0 && newClassDiffs.Count > 0)
            {
                foreach (Diff newClassDiff in newClassDiffs)
                {
                    // Ignore processed diffs.
                    if (newClassDiff.Processed)
                        continue;

                    // Grab the summary version of the new diff.
                    ClassDescriptor newClass = newClassDiff.Target as ClassDescriptor;
                    string newDiff = newClassDiff.WriteDiffTxt(false);

                    foreach (Diff oldClassDiff in oldClassDiffs)
                    {
                        // Ignore processed diffs.
                        if (oldClassDiff.Processed)
                            continue;

                        Descriptor oldClass = oldClassDiff.Target;
                        string oldDiff = oldClassDiff.WriteDiffTxt(false);

                        // Try to convert the old diff into the new diff generated above.
                        string nameChange = oldDiff
                            .Replace(oldClass.Name, newClass.Name)
                            .Replace("Removed", "Added");

                        // If the signatures match, then this is likely a renamed class.
                        if (newDiff == nameChange)
                        {
                            // Create a change diff describing the classname change.
                            Diff nameChangeDiff = new Diff()
                            {
                                Type = DiffType.Change,

                                Field = "ClassName",
                                Target = oldClass,

                                From = { '"' + oldClass.Name + '"' },
                                To = { '"' + newClass.Name + '"' }
                            };

                            // Add this change to the diffs.
                            diffs.Add(nameChangeDiff);

                            // Mark the original class diffs as processed.
                            oldClassDiff.Processed = true;
                            newClassDiff.Processed = true;
                        }
                    }
                }
            }

            // Remove processed diffs, and sort the results.
            diffs = diffs.Where(diff => !diff.Processed).ToList();
            diffs.Sort();

            // Setup actions for generating the final result, based on the requested format.
            DiffResultLineAdder addLineToResults;
            DiffResultFinalizer finalizeResults;

            List<string> lines = diffs.Select(diff => diff.ToString()).ToList();

            if (format == "HTML")
            {
                ReflectionDumper htmlDumper = new ReflectionDumper();
                Dictionary<string, Diff> diffLookup = diffs.ToDictionary(diff => diff.ToString());

                finalizeResults = new DiffResultFinalizer(() =>
                {
                    string result = htmlDumper.GetBuffer();
                    return Main.PostProcessHtml(result);
                });

                addLineToResults = new DiffResultLineAdder((line, addBreak) =>
                {
                    if (addBreak)
                    {
                        htmlDumper.Write(HTML_BREAK);
                        htmlDumper.NextLine();
                    }

                    Diff diff = diffLookup[line];
                    diff.WriteDiffHtml(htmlDumper);

                    if (line.EndsWith(NL))
                    {
                        htmlDumper.Write(HTML_BREAK);
                        htmlDumper.NextLine();
                    }
                });
            }
            else
            {
                List<string> final = new List<string>();

                addLineToResults = new DiffResultLineAdder((line, addBreak) =>
                {
                    if (addBreak)
                        final.Add("");

                    final.Add(line);
                });

                finalizeResults = new DiffResultFinalizer(() =>
                {
                    string[] finalLines = final.ToArray();
                    return string.Join(NL, finalLines).Trim();
                });
            }

            // Generate the final diff results
            string prevLead = "";
            string lastLine = NL;

            foreach (string line in lines)
            {
                string[] words = line.Split(' ');

                if (words.Length >= 2)
                {
                    // Capture the first two words in this line.
                    string first = words[0];
                    string second = words[1];

                    if (second.ToLower() == "the" && words.Length > 2)
                        second = words[2];

                    string lead = (first + ' ' + second).Trim();

                    bool addBreak = false;
                    bool lastLineNoBreak = !lastLine.EndsWith(NL);

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
                    if (!addBreak && lastLineNoBreak && line.EndsWith(NL))
                        addBreak = true;

                    // Handle writing this line depending on the format we're using.
                    addLineToResults(line, addBreak);
                    lastLine = line;
                }
            }

            return finalizeResults();
        }
    }
}