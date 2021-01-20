namespace Roblox.Reflection
{
    public struct Serialization
    {
        public bool CanSave;
        public bool CanLoad;

        private static string[] flagLabels = new string[4]
        {
            "<🕒> RuntimeOnly",

            "", "<📁> LoadOnly",

            "<💾|📁> Saves|Loads"
        };

        public override string ToString()
        {
            // sure would be nice if booleans could be 
            // casted as ints in C# like in C++ lol

            int saves = (CanSave ? 1 : 0);
            int loads = (CanLoad ? 1 : 0);

            string label = flagLabels[loads << 1 | saves];

            if (string.IsNullOrEmpty(label))
                return label;

            return $"[{label}]";
        }
    }
}