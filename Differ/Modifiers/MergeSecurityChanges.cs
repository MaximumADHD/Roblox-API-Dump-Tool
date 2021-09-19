using System.Collections.Generic;
using System.Linq;

namespace RobloxApiDumpTool
{
    public sealed class MergeSecurityChanges : IDiffModifier
    {
        public ModifierOrder Order => ModifierOrder.PostMemberDiff;

        public void RunModifier(ref List<Diff> diffs)
        {
            List<Diff> memberSecurityDiffs = diffs
                .Where(diff => diff.Target is MemberDescriptor)
                .Where(diff => diff.Type == DiffType.Change)
                .Where(diff => diff.Field == "security")
                .ToList();

            if (memberSecurityDiffs.Count == 0)
                return;

            var classMap = new Dictionary<ClassDescriptor, List<Diff>>();

            foreach (Diff diff in memberSecurityDiffs)
            {
                var memberDesc = diff.Target as MemberDescriptor;
                var classDesc = memberDesc.Class;

                if (!classMap.ContainsKey(classDesc))
                    classMap.Add(classDesc, new List<Diff>());

                classMap[classDesc].Add(diff);
            }

            foreach (ClassDescriptor classDesc in classMap.Keys)
            {
                var memberDiffs = classMap[classDesc];
                var firstDiff = memberDiffs[0];

                string baseOldSec = firstDiff.From.ToString();
                string baseNewSec = firstDiff.To.ToString();
                
                bool securityChangesMatch = true;

                for (int i = 1; i < memberDiffs.Count; i++)
                {
                    var memberDiff = memberDiffs[i];

                    string oldSec = memberDiff.From.ToString();
                    string newSec = memberDiff.To.ToString();

                    if (baseOldSec != oldSec || baseNewSec != newSec)
                    {
                        securityChangesMatch = false;
                        break;
                    }
                }

                if (securityChangesMatch)
                {
                    var changed = memberDiffs.Select(diff => diff.Target);

                    var unchanged = classDesc.Members
                        .Except(changed)
                        .Count();

                    if (unchanged == 0)
                    {
                        foreach (Diff memberDiff in memberDiffs)
                            memberDiff.Disposed = true;

                        Diff securityClassDiff = new Diff()
                        {
                            Type = DiffType.Change,

                            Target = classDesc,
                            Field = "security",

                            From = firstDiff.From,
                            To = firstDiff.To
                        };

                        diffs.Add(securityClassDiff);
                    }
                }
            }
        }
    }
}

