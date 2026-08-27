using System.Runtime.InteropServices;

namespace AuroraScript.Runtime.Property
{


    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal readonly struct PropertyMeta
    {
        [FieldOffset(0)]
        public readonly ushort Slot;

        [FieldOffset(4)]
        public readonly PropertyFlags Flags;

        public PropertyMeta(ushort slot, PropertyFlags flags)
        {
            Slot = slot;
            Flags = flags;
        }
        public bool Writable => (Flags & PropertyFlags.Writable) == PropertyFlags.Writable;
        public bool Enumerable => (Flags & PropertyFlags.Enumerable) == PropertyFlags.Enumerable;
        public bool Configurable => (Flags & PropertyFlags.Configurable) == PropertyFlags.Configurable;
        public bool ModuleExport => (Flags & PropertyFlags.ModuleExport) != 0;
        public bool NativeFunction => (Flags & PropertyFlags.NativeFunction) != 0;

    }
}
