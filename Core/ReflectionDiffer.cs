using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Roblox.Reflection
{
    public static class ReflectionDiffer
    {
        private const string NL = "\r\n";
        private const string HTML_BREAK = NL + "<br/>" + NL;

        private static List<Diff> results = new List<Diff>();
        private static string currentFormat;

        private static List<IDiffModifier> preModifiers = new List<IDiffModifier>();
        private static List<IDiffModifier> postModifiers = new List<IDiffModifier>(); 

        private delegate Diff DiffRecorder(Descriptor target, bool detailed = true, Diff parent = null);
        private delegate void DiffResultLineAdder(string line, bool addBreak);
        private delegate string DiffResultFinalizer();

        static ReflectionDiffer()
        {
            // Initialize the IDiffModifier singletons
            Type IDiffModifier = typeof(IDiffModifier);

            var taskTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type != IDiffModifier)
                .Where(type => IDiffModifier.IsAssignableFrom(type));

            foreach (Type taskType in taskTypes)
            {
                var modifier = Activator.CreateInstance(taskType) as IDiffModifier;
                
                switch (modifier.Order)
                {
                    case ModifierOrder.PreMemberDiff:
                    {
                        preModifiers.Add(modifier);
                        break;
                    }
                    case ModifierOrder.PostMemberDiff:
                    {
                        postModifiers.Add(modifier);
                        break;
                    }
                }
            }
        }

        private static Dictionary<string, T> createLookupTable<T>(List<T> entries) where T : Descriptor
        {
            return entries.ToDictionary(entry => entry.Name);
        }

        private static void flagEntireClass(ClassDescriptor classDesc, DiffRecorder record, bool detailed)
        {
            Diff classDiff = record(classDesc, detailed);
            classDesc.Members.ForEach(memberDesc => record(memberDesc, detailed, classDiff));
        }

        private static void flagEntireEnum(EnumDescriptor enumDesc, DiffRecorder record, bool detailed)
        {
            Diff enumDiff = record(enumDesc, detailed);
            enumDesc.Items.ForEach(itemDesc => record(itemDesc, detailed, enumDiff));
        }

        private static Diff Added(string field, Descriptor target, bool detailed = true, Diff parent = null)
        {
            var tags = target.Tags;
            tags.SwitchToPreliminary();

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

        private static Diff Added(Descriptor target, bool detailed = true, Diff parent = null)
        {
            string descType = target.DescriptorType;
            return Added(descType, target, detailed, parent);
        }

        private static Diff Removed(string field, Descriptor target, bool detailed = false, Diff parent = null)
        {
            var tags = target.Tags;
            tags.SwitchToPreliminary();
            
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

        private static Diff Removed(Descriptor target, bool detailed = true, Diff parent = null)
        {
            string descType = target.DescriptorType;
            return Removed(descType, target, detailed, parent);
        }

        private static Diff Changed(string field, Descriptor target, object from, object to)
        {
            Diff changed = new Diff()
            {
                Type = DiffType.Change,
                Detailed = false,

                Field = field,
                Target = target,

                From = { from },
                To   = {  to  }
            };

            results.Add(changed);
            return changed;
        }

        private static void Compare(Descriptor target, string context, object oldVal, object newVal)
        {
            if (oldVal.ToString() == newVal.ToString())
                return;
            
            Changed(context, target, oldVal, newVal);
        }

        private static void Compare(Descriptor target, string context, string oldVal, string newVal, bool inQuotes = false)
        {
            if (oldVal != newVal)
            {
                if (inQuotes && currentFormat != "html")
                {
                    oldVal = '"' + oldVal + '"';
                    newVal = '"' + newVal + '"';
                }

                Changed(context, target, oldVal, newVal);
            }
        }

        private static Dictionary<string, Diff> CompareTags(Descriptor target, Tags oldTags, Tags newTags)
        {
            var tagChanges = new Dictionary<string, Diff>();
            oldTags.ClearBadData();
            newTags.ClearBadData();

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

        private static void MergeTagDiffs(Dictionary<string, Diff> parentTags, Dictionary<string, Diff> childTags)
        {
            foreach (string tag in parentTags.Keys)
            {
                if (childTags.ContainsKey(tag))
                {
                    Diff parentDiff = parentTags[tag];
                    Diff childDiff = childTags[tag];
                    parentDiff.AddChild(childDiff);
                }
            }
        }

        public static async Task<string> CompareDatabases(ReflectionDatabase oldApi, ReflectionDatabase newApi, string format = "TXT", bool postProcess = true)
        {
            currentFormat = format.ToLower();

            // For the purposes of the differ, treat png like html.
            // Its assumed that the result will be processed afterwards.

            if (currentFormat == "png")
                currentFormat = "html";

            // Clean up old results.
            if (results.Count > 0)
                results.Clear();

            // Grab the class lists.
            var oldClasses = oldApi.Classes;
            var newClasses = newApi.Classes;

            // Record classes that were added.
            foreach (string className in newClasses.Keys)
            {
                if (!oldClasses.ContainsKey(className))
                {
                    ClassDescriptor classDesc = newClasses[className];
                    flagEntireClass(classDesc, Added, true);
                }
            }

            // Record classes that were removed.
            foreach (string className in oldClasses.Keys)
            {
                if (!newClasses.ContainsKey(className))
                {
                    ClassDescriptor classDesc = oldClasses[className];
                    flagEntireClass(classDesc, Removed, false);
                }
            }

            // Run pre-member-diff modifier tasks.
            foreach (IDiffModifier preModifier in preModifiers)
                preModifier.RunModifier(ref results);

            // Compare class changes.
            foreach (string className in oldClasses.Keys)
            {
                ClassDescriptor oldClass = oldClasses[className];

                if (newClasses.ContainsKey(className))
                {
                    ClassDescriptor newClass = newClasses[className];
                    
                    // Capture the members of these classes.
                    var oldMembers = createLookupTable(oldClass.Members);
                    var newMembers = createLookupTable(newClass.Members);

                    // Compare the classes directly.
                    var classTagDiffs = CompareTags(oldClass, oldClass.Tags, newClass.Tags);
                    Compare(oldClass, "superclass", oldClass.Superclass, newClass.Superclass, true);
                    Compare(oldClass, "memory category", oldClass.MemoryCategory, newClass.MemoryCategory, true);

                    // Record members that were added.
                    foreach (string memberName in newMembers.Keys)
                    {
                        if (!oldMembers.ContainsKey(memberName))
                        {
                            // Add New Member
                            MemberDescriptor newMember = newMembers[memberName];
                            Added(newMember);
                        }
                    }

                    // Record members that were changed or removed.
                    foreach (string memberName in oldMembers.Keys)
                    {
                        MemberDescriptor oldMember = oldMembers[memberName];

                        if (newMembers.ContainsKey(memberName))
                        {
                            MemberDescriptor newMember = newMembers[memberName];

                            if (oldMember.ThreadSafety.Type != ThreadSafetyType.Unknown)
                                Compare(newMember, "thread safety", oldMember.ThreadSafety, newMember.ThreadSafety);

                            // Check if any tags added to this member were also added to its parent class.
                            var memberTagDiffs = CompareTags(newMember, oldMember.Tags, newMember.Tags);
                            MergeTagDiffs(classTagDiffs, memberTagDiffs);

                            // Compare the fields specific to these member types
                            // TODO: I'd like to move these routines into their respective 
                            //       members, but I'm not sure how to do so in a clean manner.

                            if (newMember is PropertyDescriptor)
                            {
                                var oldProp = oldMember as PropertyDescriptor;
                                var newProp = newMember as PropertyDescriptor;

                                var oldMerged = oldProp.Security.Merged;
                                var newMerged = newProp.Security.Merged;

                                if (oldMerged && newMerged)
                                {
                                    // Just compare them as a security change alone.
                                    var oldSecurity = oldProp.Security.Value;
                                    var newSecurity = newProp.Security.Value;

                                    Compare(newMember, "security", oldSecurity, newSecurity);
                                }
                                else
                                {
                                    // Compare the read/write permissions individually.
                                    var oldSecurity = oldProp.Security;
                                    var newSecurity = newProp.Security;

                                    string oldRead = oldSecurity.Read.Value,
                                           newRead = newSecurity.Read.Value;

                                    string oldWrite = oldSecurity.Write.Value,
                                           newWrite = newSecurity.Write.Value;

                                    Compare(newMember, "read permissions", oldRead, newRead);
                                    Compare(newMember, "write permissions", oldWrite, newWrite);
                                }

                                var oldSerial = oldProp.Serialization.Describe(true);
                                var newSerial = newProp.Serialization.Describe(true);

                                Compare(newMember, "serialization", oldSerial, newSerial);
                                Compare(newMember, "value-type", oldProp.ValueType, newProp.ValueType);
                                Compare(newMember, "category", oldProp.Category, newProp.Category, true);
                            }
                            else if (newMember is FunctionDescriptor)
                            {
                                var oldFunc = oldMember as FunctionDescriptor;
                                var newFunc = newMember as FunctionDescriptor;

                                Compare(newMember, "security", oldFunc.Security, newFunc.Security);
                                Compare(newMember, "parameters", oldFunc.Parameters, newFunc.Parameters);
                                Compare(newMember, "return-type", oldFunc.ReturnType, newFunc.ReturnType);
                            }
                            else if (newMember is CallbackDescriptor)
                            {
                                var oldCall = oldMember as CallbackDescriptor;
                                var newCall = newMember as CallbackDescriptor;

                                Compare(newMember, "security", oldCall.Security, newCall.Security);
                                Compare(newMember, "parameters", oldCall.Parameters, newCall.Parameters);
                                Compare(newMember, "expected return-type", oldCall.ReturnType, newCall.ReturnType);
                            }
                            else if (newMember is EventDescriptor)
                            {
                                var oldEvent = oldMember as EventDescriptor;
                                var newEvent = newMember as EventDescriptor;

                                Compare(newMember, "security", oldEvent.Security, newEvent.Security);
                                Compare(newMember, "parameters", oldEvent.Parameters, newEvent.Parameters);
                            }
                        }
                        else
                        {
                            // Remove old member.
                            Removed(oldMember, false);
                        }
                    }
                }
            }

            // Grab the enum lists.
            var oldEnums = oldApi.Enums;
            var newEnums = newApi.Enums;

            // Record enums that were added.
            foreach (string enumName in newEnums.Keys)
            {
                if (!oldEnums.ContainsKey(enumName))
                {
                    EnumDescriptor newEnum = newEnums[enumName];
                    flagEntireEnum(newEnum, Added, true);
                }
            }

            // Record enums that were changed or removed.
            foreach (string enumName in oldEnums.Keys)
            {
                EnumDescriptor oldEnum = oldEnums[enumName];

                if (newEnums.ContainsKey(enumName))
                {
                    EnumDescriptor newEnum = newEnums[enumName];
                    var enumTagDiffs = CompareTags(newEnum, oldEnum.Tags, newEnum.Tags);

                    // Grab the enum-item lists.
                    var oldItems = createLookupTable(oldEnum.Items);
                    var newItems = createLookupTable(newEnum.Items);

                    // Record enum-items that were added.
                    foreach (var itemName in newItems.Keys)
                    {
                        if (!oldItems.ContainsKey(itemName))
                        {
                            EnumItemDescriptor item = newItems[itemName];
                            Added(item);
                        }
                    }

                    foreach (var itemName in oldItems.Keys)
                    {
                        EnumItemDescriptor oldItem = oldItems[itemName];

                        if (newItems.ContainsKey(itemName))
                        {
                            EnumItemDescriptor newItem = newItems[itemName];
                            Compare(newItem, "value", oldItem.Value, newItem.Value);
                            
                            // Check if any tags that were added to this item were also added to its parent enum.
                            var itemTagDiffs = CompareTags(newItem, oldItem.Tags, newItem.Tags);
                            MergeTagDiffs(enumTagDiffs, itemTagDiffs);
                        }
                        else
                        {
                            // Remove old enum-item.
                            Removed(oldItem, false);
                        }
                    }
                }
                else
                {
                    // Remove old enum.
                    flagEntireEnum(oldEnum, Removed, false);
                }
            }

            // Exit early if no diffs were recorded.
            if (results.Count == 0)
                return "";

            // Select diffs that are not parented to other diffs.
            List<Diff> diffs = results
                .Where(diff => !diff.HasParent)
                .ToList();

            // Run post-member-diff modifier tasks.
            foreach (IDiffModifier postModifier in postModifiers)
                postModifier.RunModifier(ref diffs);

            // Remove diffs that were disposed during the modifier tasks,
            diffs = diffs
                .Where(diff => !diff.Disposed)
                .OrderBy(diff => diff)
                .ToList();

            // Setup actions for generating the final result, based on the requested format.
            DiffResultLineAdder addLineToResults;
            DiffResultFinalizer finalizeResults;

            List<string> lines = diffs
                .Select(diff => diff.ToString())
                .ToList();

            if (currentFormat == "html")
            {
                var htmlDumper = new ReflectionDumper();
                var diffLookup = diffs.ToDictionary(diff => diff.ToString());

                addLineToResults = new DiffResultLineAdder((line, addBreak) =>
                {
                    if (addBreak)
                    {
                        htmlDumper.Write(HTML_BREAK);
                        htmlDumper.NextLine();
                    }

                    if (diffLookup.ContainsKey(line))
                    {
                        Diff diff = diffLookup[line];
                        diff.WriteDiffHtml(htmlDumper);
                    }
                    
                    if (line.EndsWith(NL))
                    {
                        htmlDumper.Write(HTML_BREAK);
                        htmlDumper.NextLine();
                    }
                });

                finalizeResults = new DiffResultFinalizer(() =>
                {
                    if (newApi.Branch == "roblox")
                        htmlDumper.NextLine();

                    string result = htmlDumper.ExportResults();

                    if (postProcess)
                        result = ApiDumpTool.PostProcessHtml(result);

                    return result;
                });

                if (newApi.Branch == "roblox")
                {
                    var deployLog = await ApiDumpTool.GetLastDeployLog("roblox");
                    htmlDumper.OpenHtmlTag("h2");
                    htmlDumper.Write("Version " + deployLog.ToString());

                    htmlDumper.CloseHtmlTag("h2");
                    htmlDumper.NextLine(2);
                }
            }
            else
            {
                var final = new List<string>();

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
