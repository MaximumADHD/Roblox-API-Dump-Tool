namespace RobloxApiDumpTool
{
    public sealed class FunctionDescriptor : MemberDescriptor
    {
        public LuaType ReturnType;
        public Parameters Parameters;
        public Security Security;

        public override string GetSchema(bool detailed = true)
        {
            string schema = base.GetSchema(detailed)
                .Replace(".", ":");

            if (detailed)
                schema += "{Parameters} -> {ReturnType} {Security} {Tags} {ThreadSafety}";

            return schema;
        }
    }
}