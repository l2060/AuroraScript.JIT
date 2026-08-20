using System;

namespace AuroraScript.Runtime.Types.TypeConstruct
{
    /// <summary>Constructor shared by the fixed-length primitive array types.</summary>
    internal sealed class PackedArrayConstructor : ScriptType
    {
        internal static readonly PackedArrayConstructor Int32 = new("Int32Array", PackedArrayKind.Int32);
        internal static readonly PackedArrayConstructor Int8 = new("Int8Array", PackedArrayKind.Int8);
        internal static readonly PackedArrayConstructor Float64 = new("Float64Array", PackedArrayKind.Float64);
        internal static readonly PackedArrayConstructor Boolean = new("BooleanArray", PackedArrayKind.Boolean);
        internal static readonly PackedArrayConstructor UInt8 = new("UInt8Array", PackedArrayKind.UInt8);
        internal static readonly PackedArrayConstructor Int16 = new("Int16Array", PackedArrayKind.Int16);
        internal static readonly PackedArrayConstructor UInt16 = new("UInt16Array", PackedArrayKind.UInt16);
        internal static readonly PackedArrayConstructor UInt32 = new("UInt32Array", PackedArrayKind.UInt32);
        internal static readonly PackedArrayConstructor Int64 = new("Int64Array", PackedArrayKind.Int64);
        internal static readonly PackedArrayConstructor UInt64 = new("UInt64Array", PackedArrayKind.UInt64);

        private readonly PackedArrayKind _kind;

        private PackedArrayConstructor(string name, PackedArrayKind kind) : base(name, callable: false)
        {
            _kind = kind;
            Frozen();
        }

        public override void Construct(ScriptContext context, Span<ScriptDatum> arguments, ref ScriptDatum result)
        {
            if (arguments.Length > 1)
            {
                throw new AuroraRuntimeException(Name + " accepts at most one length argument.");
            }

            var length = arguments.Length == 0
                ? 0
                : ScriptPackedArray.ValidateLength(arguments[0]);
            ScriptObject array = _kind switch
            {
                PackedArrayKind.Int32 => new ScriptInt32Array(length),
                PackedArrayKind.Int8 => new ScriptInt8Array(length),
                PackedArrayKind.Float64 => new ScriptFloat64Array(length),
                PackedArrayKind.Boolean => new ScriptBooleanArray(length),
                PackedArrayKind.UInt8 => new ScriptUInt8Array(length),
                PackedArrayKind.Int16 => new ScriptInt16Array(length),
                PackedArrayKind.UInt16 => new ScriptUInt16Array(length),
                PackedArrayKind.UInt32 => new ScriptUInt32Array(length),
                PackedArrayKind.Int64 => new ScriptInt64Array(length),
                PackedArrayKind.UInt64 => new ScriptUInt64Array(length),
                _ => throw new InvalidOperationException("Unknown packed-array kind.")
            };
            ScriptDatum.WriteAsObject(ref result, array);
        }

        private enum PackedArrayKind : byte
        {
            Int32,
            Int8,
            Float64,
            Boolean,
            UInt8,
            Int16,
            UInt16,
            UInt32,
            Int64,
            UInt64
        }
    }
}
