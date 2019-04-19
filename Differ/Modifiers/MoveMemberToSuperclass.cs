using System.Collections.Generic;
using System.Linq;

namespace Roblox.Reflection
{
    /*
     * This modifier detects when an API member is removed from 
     * an object because it was moved to a superclass. 
     * 
     * Instead of listing it as removed, it is replaced with a 
     * merge diff that describes the superclass change.
     */

    public sealed class MoveMemberToSuperclass : IDiffModifier
    {
        public ModifierOrder Order => ModifierOrder.PostMemberDiff;

        private static IEnumerable<Diff> collectMemberDiffs(IEnumerable<Diff> diffs, DiffType type)
        {
            var typed = diffs.Where((diff) => type == diff.Type);
            var classes = typed.Where((diff) => diff.Target is ClassDescriptor);

            var members = typed
                .Where((diff) => diff.Target is MemberDescriptor)
                .ToList();

            foreach (Diff classDiff in classes)
            {
                var classMembers = collectMemberDiffs(classDiff.Children, type);
                members.AddRange(classMembers);
            }

            return members;
        }

        public void RunModifier(ref List<Diff> diffs)
        {
            var merging = new Dictionary<MemberDescriptor, List<MemberDescriptor>>();

            var added = collectMemberDiffs(diffs, DiffType.Add);
            var removed = collectMemberDiffs(diffs, DiffType.Remove);

            foreach (Diff targetDiff in removed)
            {
                var targetMember = targetDiff.Target as MemberDescriptor;
                string targetName = targetMember.Name;

                foreach (Diff otherDiff in added)
                {
                    var otherMember = otherDiff.Target as MemberDescriptor;
                    string otherName = otherMember.Name;

                    if (targetName == otherName)
                    {
                        // Grab the classes of the two members we have selected.
                        ClassDescriptor targetClass = targetMember.Class;
                        ClassDescriptor otherClass = otherMember.Class;

                        // Because the databases of these two members are different, this uses 
                        // the database of the other member, since it should be the newer one.
                        var database = otherClass.Database;
                        var classLookup = database.Classes;

                        // Override targetClass with its corresponding entry in the classLookup.
                        // This is necessary to have a snapshot of the newer class hierarchy.
                        targetClass = classLookup[targetClass.Name];

                        // Now test the ancestry of the two classes.
                        if (otherClass.IsAncestorOf(targetClass))
                        {
                            if (!merging.ContainsKey(otherMember))
                            {
                                var mergers = new List<MemberDescriptor>();
                                merging.Add(otherMember, mergers);
                            }

                            merging[otherMember].Add(targetMember);
                            targetDiff.Disposed = true;
                        }
                    }
                }
            }

            // Process the diffs and try grouping any 
            // that use the same member descriptor.

            foreach (MemberDescriptor member in merging.Keys)
            {
                List<MemberDescriptor> members = merging[member];

                Diff mergeDiff = new Diff();
                mergeDiff.Type = DiffType.Merge;
                
                var mergeInto = new DiffChangeList();
                mergeInto.Add(member);
                mergeDiff.To = mergeInto;
                
                if (members.Count > 1)
                {
                    var mergeFrom = new DiffChangeList();
                    mergeFrom.AddRange(members);
                    
                    mergeDiff.From = mergeFrom;
                    mergeDiff.Target = member;
                }
                else
                {
                    var targetMember = members.First();
                    mergeDiff.Target = targetMember;
                }

                diffs.Add(mergeDiff);
            }
        }
    }
}
