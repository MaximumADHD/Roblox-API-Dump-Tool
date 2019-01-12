namespace Roblox.Reflection
{
    public sealed class PropertyDescriptor : MemberDescriptor
    {
        public string Category;
        public LuaType ValueType;
        public ReadWriteSecurity Security;
        public Serialization Serialization;

        public override int CompareTo(object other)
        {
            if (other is PropertyDescriptor)
            {
                var otherDesc = other as PropertyDescriptor;

                if (Class != otherDesc.Class)
                    return Class.CompareTo(otherDesc.Class);

                bool thisIsCamel = char.IsLower(Name[0]);
                bool otherIsCamel = char.IsLower(otherDesc.Name[0]);

                // Upcast the comparison if this is a camelCase condition.
                // camelCase members should always appear last in the member type listing.
                if (thisIsCamel != otherIsCamel)
                    return base.CompareTo(other);

                // Compare by categories.
                if (Category != otherDesc.Category)
                {
                    return Category.CompareTo(otherDesc.Category);
                }
            }

            return base.CompareTo(other);
        }
    }
}