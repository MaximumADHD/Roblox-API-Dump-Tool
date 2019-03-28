using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roblox.Reflection
{
    public class ReflectionDiffer
    {
        public bool PostProcessHtml = true;

        private const string NL = "\r\n";
        private const string HTML_BREAK = NL + "<br/>" + NL;

        private List<Diff> results = new List<Diff>();
        private string currentFormat;

        private static List<IDiffMerger> preMergers = new List<IDiffMerger>();
        private static List<IDiffMerger> postMergers = new List<IDiffMerger>(); 

        private delegate Diff DiffRecorder(Descriptor target, bool detailed = true, Diff parent = null);
        private delegate void DiffResultLineAdder(string line, bool addBreak);
        private delegate string DiffResultFinalizer();

        static ReflectionDiffer()
        {
            // Initialize the IDiffMerger singletons
            Type IDiffMerger = typeof(IDiffMerger);

            var taskTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type != IDiffMerger)
                .Where(type => IDiffMerger.IsAssignableFrom(type));

            foreach (Type taskType in taskTypes)
            {
                IDiffMerger merger = Activator.CreateInstance(taskType) as IDiffMerger;
                List<IDiffMerger> orderList = null;

                if (merger.Order == IDiffMergerOrder.PreMemberDiff)
                    orderList = preMergers;
                else if (merger.Order == IDiffMergerOrder.PostMemberDiff)
                    orderList = postMergers;

                orderList?.Add(merger);
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

        private void Compare(Descriptor target, string context, object oldVal, object newVal)
        {
            if (oldVal.ToString() != newVal.ToString())
            {
                Changed(context, target, oldVal, newVal);
            }
        }

        private void Compare(Descriptor target, string context, string oldVal, string newVal, bool inQuotes = false)
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

        private Dictionary<string, Diff> CompareTags(Descriptor target, Tags oldTags, Tags newTags)
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

        private void MergeTagDiffs(Dictionary<string, Diff> parentTags, Dictionary<string, Diff> childTags)
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

        public async Task<string> CompareDatabases(ReflectionDatabase oldApi, ReflectionDatabase newApi, string format = "TXT")
        {
            results.Clear();
            currentFormat = format.ToLower();

            // Diff Classes
            var oldClasses = oldApi.Classes;
            var newClasses = newApi.Classes;

            // Record added classes
            foreach (string className in newClasses.Keys)
            {
                if (!oldClasses.ContainsKey(className))
                {
                    // Add New Class
                    ClassDescriptor classDesc = newClasses[className];
                    flagEntireClass(classDesc, Added, true);
                }
            }

            // Record removed classes
            foreach (string className in oldClasses.Keys)
            {
                if (!newClasses.ContainsKey(className))
                {
                    ClassDescriptor classDesc = oldClasses[className];
                    flagEntireClass(classDesc, Removed, false);
                }
            }

            // Run pre-merger tasks.
            foreach (IDiffMerger preMerge in preMergers)
                preMerge.RunMergeTask(ref results);

            // Compare class changes.
            foreach (string className in oldClasses.Keys)
            {
                ClassDescriptor oldClass = oldClasses[className];

                if (newClasses.ContainsKey(className))
                {
                    ClassDescriptor newClass = newClasses[className];
                    var classTagDiffs = CompareTags(oldClass, oldClass.Tags, newClass.Tags);

                    Compare(oldClass, "superclass", oldClass.Superclass, newClass.Superclass);
                    Compare(oldClass, "memory category", oldClass.MemoryCategory, newClass.MemoryCategory);

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

                        if (newMembers.ContainsKey(memberName))
                        {
                            MemberDescriptor newMember = newMembers[memberName];

                            // Check if any tags that were added to this member were also added to its parent class.
                            var memberTagDiffs = CompareTags(newMember, oldMember.Tags, newMember.Tags);
                            MergeTagDiffs(classTagDiffs, memberTagDiffs);

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
                                    Compare(newMember, "security", oldSecurity, newSecurity);
                                }
                                else
                                {
                                    ReadWriteSecurity oldSecurity = oldProp.Security;
                                    ReadWriteSecurity newSecurity = newProp.Security;

                                    string oldRead = oldSecurity.Read.Describe(true);
                                    string newRead = newSecurity.Read.Describe(true);
                                    Compare(newMember, "read permissions", oldRead, newRead);

                                    string oldWrite = oldSecurity.Write.Describe(true);
                                    string newWrite = newSecurity.Write.Describe(true);
                                    Compare(newMember, "write permissions", oldWrite, newWrite);
                                }
                                
                                Compare(newMember, "value-type", oldProp.ValueType, newProp.ValueType);
                                Compare(newMember, "serialization", oldProp.Serialization, newProp.Serialization);
                                Compare(newMember, "category", oldProp.Category, newProp.Category, true);
                            }
                            else if (newMember is FunctionDescriptor)
                            {
                                var oldFunc = oldMember as FunctionDescriptor;
                                var newFunc = newMember as FunctionDescriptor;

                                Compare(newMember, "security", oldFunc.Security, newFunc.Security);
                                Compare(newMember, "return-type", oldFunc.ReturnType, newFunc.ReturnType);
                                Compare(newMember, "parameters",  oldFunc.Parameters, newFunc.Parameters);
                            }
                            else if (newMember is CallbackDescriptor)
                            {
                                var oldCall = oldMember as CallbackDescriptor;
                                var newCall = newMember as CallbackDescriptor;

                                Compare(newMember, "security", oldCall.Security, newCall.Security);
                                Compare(newMember, "expected return-type", oldCall.ReturnType, newCall.ReturnType);
                                Compare(newMember, "parameters", oldCall.Parameters, newCall.Parameters);
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
                            // Remove Old Member
                            Removed(oldMember, false);
                        }
                    }
                }
            }

            // Diff Enums
            var oldEnums = oldApi.Enums;
            var newEnums = newApi.Enums;

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
                    var enumTagDiffs = CompareTags(newEnum, oldEnum.Tags, newEnum.Tags);

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

            // Peace out early if no diffs were recorded.
            if (results.Count == 0)
                return "";

            // Select diffs that are not parented to other diffs.
            List<Diff> diffs = results
                .Where(diff => !diff.HasParent)
                .ToList();

            // Run post-merger tasks.
            foreach (IDiffMerger postMerge in postMergers)
                postMerge.RunMergeTask(ref diffs);

            // Remove diffs that were discarded during a merge, and sort the results.
            diffs = diffs.Where(diff => !diff.Merged).ToList();
            diffs.Sort();
            
            // Setup actions for generating the final result, based on the requested format.
            DiffResultLineAdder addLineToResults;
            DiffResultFinalizer finalizeResults;

            List<string> lines = diffs.Select(diff => diff.ToString()).ToList();

            if (format == "HTML")
            {
                ReflectionDumper htmlDumper = new ReflectionDumper();
                Dictionary<string, Diff> diffLookup = diffs.ToDictionary(diff => diff.ToString());

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
                    if (newApi.Branch != null)
                        htmlDumper.NextLine();

                    string result = htmlDumper.ExportResults();

                    if (PostProcessHtml)
                        result = ApiDumpTool.PostProcessHtml(result);

                    return result;
                });

                if (newApi.Branch != null)
                {
                    if (newApi.Version == null)
                        newApi.Version = await ApiDumpTool.GetLiveVersion(newApi.Branch);

                    htmlDumper.OpenHtmlTag("h2");
                    htmlDumper.Write("Version " + newApi.Version);
                    htmlDumper.CloseHtmlTag("h2");
                    htmlDumper.NextLine(2);
                }
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