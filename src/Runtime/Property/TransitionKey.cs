using System;
using System.Runtime.InteropServices;

namespace AuroraScript.Runtime.Property
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal readonly struct TransitionKey : IEquatable<TransitionKey>
    {

        [FieldOffset(0)]
        public readonly string Name;

        [FieldOffset(8)]
        public readonly PropertyFlags Flags;

        public TransitionKey(string name, PropertyFlags flags)
        {
            Name = name;
            Flags = flags;
        }

        public bool Equals(TransitionKey other) => Name == other.Name && Flags == other.Flags;

        public override bool Equals(object? obj) => obj is TransitionKey other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Name);
            hash.Add(Flags);
            return hash.ToHashCode();
        }
    }
}
