using AuroraScript.Runtime.Types;
using System;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Partial implementation of <see cref="ScriptDatum"/> providing implicit conversion operators
    /// for seamless interop between .NET types and AuroraScript values.
    /// </summary>
    public partial struct ScriptDatum
    {
        /// <summary> Implicitly converts a .NET <see cref="Boolean"/> to a <see cref="ScriptDatum"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ScriptDatum(Boolean value)
        {
            return new ScriptDatum { Kind = ValueKind.Boolean, Boolean = value };
        }

        /// <summary> Implicitly converts a .NET <see cref="String"/> to a <see cref="ScriptDatum"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ScriptDatum(string value)
        {
            return new ScriptDatum { Kind = ValueKind.String, String = StringValue.Of(value) };
        }

        /// <summary> Implicitly converts a .NET <see cref="Int64"/> to a <see cref="ScriptDatum"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ScriptDatum(Int64 value)
        {
            return new ScriptDatum { Kind = ValueKind.Number, Number = value };
        }

        /// <summary> Implicitly converts a .NET <see cref="UInt64"/> to a <see cref="ScriptDatum"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ScriptDatum(UInt64 value)
        {
            return new ScriptDatum { Kind = ValueKind.Number, Number = value };
        }

        /// <summary> Implicitly converts a .NET <see cref="Int32"/> to a <see cref="ScriptDatum"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ScriptDatum(Int32 value)
        {
            return new ScriptDatum { Kind = ValueKind.Number, Number = value };
        }

        /// <summary> Implicitly converts a .NET <see cref="UInt32"/> to a <see cref="ScriptDatum"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ScriptDatum(UInt32 value)
        {
            return new ScriptDatum { Kind = ValueKind.Number, Number = value };
        }

        /// <summary> Implicitly converts a .NET <see cref="Int16"/> to a <see cref="ScriptDatum"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ScriptDatum(Int16 value)
        {
            return new ScriptDatum { Kind = ValueKind.Number, Number = value };
        }

        /// <summary> Implicitly converts a .NET <see cref="UInt16"/> to a <see cref="ScriptDatum"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ScriptDatum(UInt16 value)
        {
            return new ScriptDatum { Kind = ValueKind.Number, Number = value };
        }

        /// <summary> Implicitly converts a .NET <see cref="Double"/> to a <see cref="ScriptDatum"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ScriptDatum(double value)
        {
            return new ScriptDatum { Kind = ValueKind.Number, Number = value };
        }

        /// <summary> Implicitly converts a .NET <see cref="Single"/> to a <see cref="ScriptDatum"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ScriptDatum(float value)
        {
            return new ScriptDatum { Kind = ValueKind.Number, Number = value };
        }
    }
}
