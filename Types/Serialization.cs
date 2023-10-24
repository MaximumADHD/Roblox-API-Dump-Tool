namespace RobloxApiDumpTool
{
    public struct Serialization
    {
        public bool CanSave;
        public bool CanLoad;

        private static readonly string[] flagLabels = new string[4]
        {
            "🚫 None",

            "💾 SaveOnly", "📁 LoadOnly",

            "💾|📁 Serialized"
        };

        public string Describe(bool isDiff = false)
        {
            if (CanSave && !CanLoad && !isDiff)
                return "";

            if (CanSave == CanLoad && !isDiff)
                return "";

            int saves = (CanSave ? 1 : 0);
            int loads = (CanLoad ? 1 : 0);

            return $"[{flagLabels[loads << 1 | saves]}]";
        }

        public override string ToString() => Describe();
    }
}