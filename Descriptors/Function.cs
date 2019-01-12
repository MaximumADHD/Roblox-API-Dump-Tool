namespace Roblox.Reflection
{
    public sealed class FunctionDescriptor : MemberDescriptor
    {
        public LuaType ReturnType;
        public Parameters Parameters;
        public Security Security;
    }
}