namespace Roblox.Reflection
{
    public struct Serialization
    {
        public bool CanSave;
        public bool CanLoad;

        private static string[] flagLabels = new string[4]
        {
            "<🕒> Runtime-only",

            "<💾> Save-only", "<📁> Load-only",

            "<💾|📁> Saves & Loads"
        };

        public override string ToString()
        {
            // sure would be nice if booleans could be 
            // casted as ints in C# like in C++ lol

            int saves = (CanSave ? 1 : 0);
            int loads = (CanLoad ? 1 : 0);

            return '[' + flagLabels[saves << 1 | loads] + ']';
        }
    }
}