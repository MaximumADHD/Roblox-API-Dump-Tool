using System.Collections.Generic;

namespace Roblox.Reflection
{
    public enum IDiffMergerOrder
    {
        PreMemberDiff = 0,
        PostMemberDiff = 1
    };
    
    public interface IDiffMerger
    {
        IDiffMergerOrder Order { get; }
        void RunMergeTask(ref List<Diff> diffs);
    }
}
