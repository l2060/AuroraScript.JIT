using System;

namespace AuroraScript.Runtime.Property
{
    internal readonly struct TransitionKey : IEquatable<TransitionKey>
    {
        public readonly string Name;
        public readonly PropertyFlags Flags;

        public TransitionKey(string name, PropertyFlags flags)
        {
            Name = name;
            Flags = flags;
        }

        public bool Equals(TransitionKey other) => Name == other.Name && Flags == other.Flags;

        public override bool Equals(object obj) => obj is TransitionKey other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Name);
            hash.Add(Flags);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
