using System;

namespace Roblox.Reflection
{
    public enum ThreadSafetyType
    {
        Unsafe,
        Safe,

        Unknown,
        ReadOnly = Safe
    }

    public class ThreadSafety
    {
        public readonly ThreadSafetyType Type;
        public override string ToString() => Describe();

        public ThreadSafety(ThreadSafetyType type)
        {
            Type = type;
        }

        public ThreadSafety(string type)
        {
            if (Enum.TryParse(type, out Type))
                return;

            Type = ThreadSafetyType.Unknown;
        }

        public static implicit operator ThreadSafety(string type)
        {
            return new ThreadSafety(type);
        }

        public static implicit operator ThreadSafety(ThreadSafetyType type)
        {
            return new ThreadSafety(type);
        }

        public string Describe(MemberType? memberType = null)
        {
            string name = $"{Type}";
            var empty = (name == "Unknown");

            if (!empty && memberType.HasValue)
            {
                switch (memberType.Value)
                {
                    case MemberType.Callback:
                    case MemberType.Function:
                    case MemberType.Event:
                    {
                        empty = (Type == ThreadSafetyType.Unsafe);
                        break;
                    }
                    case MemberType.Property:
                    {
                        empty = (Type == ThreadSafetyType.Safe);
                        break;
                    }
                }
            }

            if (empty)
                return "";
            
            return $"{{🧬{name}}}";
        }
    }
}
