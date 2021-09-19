using System.Collections.Generic;

namespace RobloxApiDumpTool
{
    public enum ModifierOrder
    {
        PreMemberDiff = 0,
        PostMemberDiff = 1
    };
    
    public interface IDiffModifier
    {
        ModifierOrder Order { get; }
        void RunModifier(ref List<Diff> diffs);
    }
}
