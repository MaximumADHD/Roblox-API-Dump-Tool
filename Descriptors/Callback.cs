using System.Collections.Generic;

namespace RobloxApiDumpTool
{
    public sealed class CallbackDescriptor : MemberDescriptor
    {
        public Security Security;
        public LuaType ReturnType;
        public Parameters Parameters;

        public override string GetSchema(bool detailed = true)
        {
            string schema = base.GetSchema();

            if (detailed)
                schema += "{Parameters} -> {ReturnType} {Capabilities} {Security} {Tags} {ThreadSafety}";

            return schema;
        }
    }
}