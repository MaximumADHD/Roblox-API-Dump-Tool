namespace Roblox.Reflection
{
    public sealed class EventDescriptor : MemberDescriptor
    {
        public Security Security;
        public Parameters Parameters;

        public override string GetSchema(bool detailed = true)
        {
            string schema = base.GetSchema();

            if (detailed)
                schema += "{Parameters} {Security} {Tags} {ThreadSafety}";

            return schema;
        }
    }
}