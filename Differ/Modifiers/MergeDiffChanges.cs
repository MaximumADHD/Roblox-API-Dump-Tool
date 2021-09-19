using System.Collections.Generic;
using System.Linq;

namespace RobloxApiDumpTool
{
    public sealed class MergeSimilarChangeDiffs : IDiffModifier
    {
        public ModifierOrder Order => ModifierOrder.PostMemberDiff;

        public void RunModifier(ref List<Diff> diffs)
        {
            List<Diff> changeDiffs = diffs
                .Where(diff => diff.Type == DiffType.Change)
                .ToList();

            foreach (Diff diff in changeDiffs)
            {
                if (!diff.Disposed)
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

                            similar.Disposed = true;
                        }
                    }
                }
            }
        }
    }
}

