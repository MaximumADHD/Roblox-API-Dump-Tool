using System.Collections.Generic;
using System.Linq;

namespace Roblox.Reflection
{
    public sealed class MergeRenamedClasses : IDiffMerger
    {
        public void RunMergeTask(ref List<Diff> diffs)
        {
            List<Diff> memberedClassDiffs = diffs
                .Where(diff =>  diff.Target is ClassDescriptor)
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
                    // Ignore merged diffs.
                    if (newClassDiff.Merged)
                        continue;

                    // Grab the summary version of the new diff.
                    Descriptor newClass = newClassDiff.Target;
                    string newDiff = newClassDiff.WriteDiffTxt(false);

                    foreach (Diff oldClassDiff in oldClassDiffs)
                    {
                        // Ignore merged diffs.
                        if (oldClassDiff.Merged)
                            continue;

                        // Grab the summary version of the old diff.
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

                            // Merge the original class diffs.
                            oldClassDiff.Merged = true;
                            newClassDiff.Merged = true;
                        }
                    }
                }
            }
        }
    }
}