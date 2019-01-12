namespace Roblox.Reflection
{
    public sealed class CallbackDescriptor : MemberDescriptor
    {
        public LuaType ReturnType;
        public Parameters Parameters;
        public Security Security;
    }
}