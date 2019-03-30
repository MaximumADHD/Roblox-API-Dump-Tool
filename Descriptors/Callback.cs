namespace Roblox.Reflection
{
    public sealed class CallbackDescriptor : MemberDescriptor
    {
        public LuaType ReturnType;
        public Parameters Parameters;
        public Security Security;

        public override string GetSchema(bool detailed = true)
        {
            string schema = base.GetSchema();

            if (detailed)
                schema = "{ReturnType} " + schema + "{Parameters} {Security} {Tags}";

            return schema;
        }
    }
}