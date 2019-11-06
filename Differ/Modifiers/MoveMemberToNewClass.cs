using System;
using System.Collections.Generic;
using System.Linq;

namespace Roblox.Reflection
{
    /*
     * This modifier detects when an API member is removed from 
     * an object because it was moved to another class.
     * 
     * If a member is removed from one class and added to another,
     * then it is recorded as a move diff.
     * 
     * If multiple members of the same name are removed, and
     * a member is added to a superclass, it is replaced with
     * a merge diff.
     */

    public sealed class MoveMemberToNewClass : IDiffModifier
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

        private void moveMember(ref List<Diff> diffs, MemberDescriptor from, MemberDescriptor to)
        {
            var toClass = to.Class;
            var fromClass = from.Class;

            var fromDiff = diffs
                .Where(diff => diff.Type == DiffType.Remove)
                .Where(diff => diff.Target == from)
                .FirstOrDefault();

            var toDiff = diffs
                .Where(diff => diff.Type == DiffType.Add)
                .Where(diff => diff.Target == to)
                .FirstOrDefault();

            if (fromDiff != null)
                fromDiff.Disposed = true;

            if (toDiff != null)
                toDiff.Disposed = true;

            Diff moveDiff = new Diff()
            {
                Type = DiffType.Move,
                Target = from,

                From = { fromClass },
                To = { toClass }
            };

            diffs.Add(moveDiff);
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
                        if (!classLookup.ContainsKey(targetClass.Name))
                            continue;

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
                        else
                        {
                            // Maybe they're just moving?
                            moveMember(ref diffs, targetMember, otherMember);
                        }
                    }
                }
            }

            // Process the diffs and try grouping any 
            // that use the same member descriptor.

            foreach (MemberDescriptor member in merging.Keys)
            {
                List<MemberDescriptor> members = merging[member];
                
                if (members.Count > 1)
                {
                    Diff mergeDiff = new Diff();
                    mergeDiff.Type = DiffType.Merge;

                    var mergeInto = new DiffChangeList();
                    mergeInto.Add(member);
                    mergeDiff.To = mergeInto;

                    var mergeFrom = new DiffChangeList();
                    mergeFrom.AddRange(members);
                    
                    mergeDiff.From = mergeFrom;
                    mergeDiff.Target = member;

                    diffs.Add(mergeDiff);
                }
                else
                {
                    var target = members.First();
                    moveMember(ref diffs, target, member);

                    var newClassDiff = diffs
                        .Where(diff => diff.Type == DiffType.Add)
                        .Where(diff => diff.Target == member.Class)
                        .FirstOrDefault();

                    if (newClassDiff == null)
                        continue;

                    var newMemberDiff = diffs
                        .SelectMany(diff => diff.Children)
                        .Where(diff => diff.Target == member)
                        .FirstOrDefault();

                    if (newMemberDiff == null)
                        continue;

                    newClassDiff.RemoveChild(newMemberDiff);
                }
            }
        }
    }
}
