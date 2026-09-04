using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable CS1591 // Script API is documented in runtime-api.json.

namespace AuroraScript.Runtime.Builtin
{
    /// <summary>
    /// Static binary codec for <see cref="ScriptUInt8Array"/>. The Type is not constructible.
    /// </summary>
    [AuroraNativeType("Conv8")]
    public sealed partial class Conv8Support : ScriptObject
    {
        private Conv8Support()
        {
        }

        /// <summary>One-byte width (bool, int8, uint8).</summary>
        [AuroraExport("BYTES1")]
        public static readonly double BYTES1 = 1;

        /// <summary>Two-byte width (int16, uint16).</summary>
        [AuroraExport("BYTES2")]
        public static readonly double BYTES2 = 2;

        /// <summary>Four-byte width (int32, uint32, float32).</summary>
        [AuroraExport("BYTES4")]
        public static readonly double BYTES4 = 4;

        /// <summary>Eight-byte width (int64, uint64, float64).</summary>
        [AuroraExport("BYTES8")]
        public static readonly double BYTES8 = 8;

        [AuroraExport("getBool", MatchFailure.Throw)]
        public static bool GetBoolCore(ScriptUInt8Array buffer, int offset) =>
            GetSpan(buffer, offset, 1)[0] != 0;

        [AuroraExport("setBool", MatchFailure.Throw)]
        public static void SetBoolCore(ScriptUInt8Array buffer, int offset, bool value) =>
            GetSpan(buffer, offset, 1)[0] = value ? (byte)1 : (byte)0;

        [AuroraExport("getInt8", MatchFailure.Throw)]
        public static int GetInt8Core(ScriptUInt8Array buffer, int offset) =>
            unchecked((sbyte)GetSpan(buffer, offset, 1)[0]);

        [AuroraExport("setInt8", MatchFailure.Throw)]
        public static void SetInt8Core(ScriptUInt8Array buffer, int offset, int value) =>
            GetSpan(buffer, offset, 1)[0] = unchecked((byte)value);

        [AuroraExport("getUInt8", MatchFailure.Throw)]
        public static int GetUInt8Core(ScriptUInt8Array buffer, int offset) =>
            GetSpan(buffer, offset, 1)[0];

        [AuroraExport("setUInt8", MatchFailure.Throw)]
        public static void SetUInt8Core(ScriptUInt8Array buffer, int offset, int value) =>
            GetSpan(buffer, offset, 1)[0] = unchecked((byte)value);

        [AuroraExport("getInt16", MatchFailure.Throw)]
        public static int GetInt16Core(
            ScriptUInt8Array buffer,
            int offset,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 2);
            return littleEndian
                ? BinaryPrimitives.ReadInt16LittleEndian(span)
                : BinaryPrimitives.ReadInt16BigEndian(span);
        }

        [AuroraExport("setInt16", MatchFailure.Throw)]
        public static void SetInt16Core(
            ScriptUInt8Array buffer,
            int offset,
            int value,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 2);
            if (littleEndian)
            {
                BinaryPrimitives.WriteInt16LittleEndian(span, unchecked((short)value));
            }
            else
            {
                BinaryPrimitives.WriteInt16BigEndian(span, unchecked((short)value));
            }
        }

        [AuroraExport("getUInt16", MatchFailure.Throw)]
        public static int GetUInt16Core(
            ScriptUInt8Array buffer,
            int offset,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 2);
            return littleEndian
                ? BinaryPrimitives.ReadUInt16LittleEndian(span)
                : BinaryPrimitives.ReadUInt16BigEndian(span);
        }

        [AuroraExport("setUInt16", MatchFailure.Throw)]
        public static void SetUInt16Core(
            ScriptUInt8Array buffer,
            int offset,
            int value,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 2);
            if (littleEndian)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(span, unchecked((ushort)value));
            }
            else
            {
                BinaryPrimitives.WriteUInt16BigEndian(span, unchecked((ushort)value));
            }
        }

        [AuroraExport("getInt32", MatchFailure.Throw)]
        public static int GetInt32Core(
            ScriptUInt8Array buffer,
            int offset,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 4);
            return littleEndian
                ? BinaryPrimitives.ReadInt32LittleEndian(span)
                : BinaryPrimitives.ReadInt32BigEndian(span);
        }

        [AuroraExport("setInt32", MatchFailure.Throw)]
        public static void SetInt32Core(
            ScriptUInt8Array buffer,
            int offset,
            int value,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 4);
            if (littleEndian)
            {
                BinaryPrimitives.WriteInt32LittleEndian(span, value);
            }
            else
            {
                BinaryPrimitives.WriteInt32BigEndian(span, value);
            }
        }

        [AuroraExport("getUInt32", MatchFailure.Throw)]
        public static double GetUInt32Core(
            ScriptUInt8Array buffer,
            int offset,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 4);
            return littleEndian
                ? BinaryPrimitives.ReadUInt32LittleEndian(span)
                : BinaryPrimitives.ReadUInt32BigEndian(span);
        }

        [AuroraExport("setUInt32", MatchFailure.Throw)]
        public static void SetUInt32Core(
            ScriptUInt8Array buffer,
            int offset,
            double value,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 4);
            var encoded = unchecked((uint)value);
            if (littleEndian)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(span, encoded);
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(span, encoded);
            }
        }

        [AuroraExport("getInt64", MatchFailure.Throw)]
        public static double GetInt64Core(
            ScriptUInt8Array buffer,
            int offset,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 8);
            var value = littleEndian
                ? BinaryPrimitives.ReadInt64LittleEndian(span)
                : BinaryPrimitives.ReadInt64BigEndian(span);
            return value;
        }

        [AuroraExport("setInt64", MatchFailure.Throw)]
        public static void SetInt64Core(
            ScriptUInt8Array buffer,
            int offset,
            double value,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 8);
            var encoded = unchecked((long)value);
            if (littleEndian)
            {
                BinaryPrimitives.WriteInt64LittleEndian(span, encoded);
            }
            else
            {
                BinaryPrimitives.WriteInt64BigEndian(span, encoded);
            }
        }

        [AuroraExport("getUInt64", MatchFailure.Throw)]
        public static double GetUInt64Core(
            ScriptUInt8Array buffer,
            int offset,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 8);
            var value = littleEndian
                ? BinaryPrimitives.ReadUInt64LittleEndian(span)
                : BinaryPrimitives.ReadUInt64BigEndian(span);
            return value;
        }

        [AuroraExport("setUInt64", MatchFailure.Throw)]
        public static void SetUInt64Core(
            ScriptUInt8Array buffer,
            int offset,
            double value,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 8);
            var encoded = unchecked((ulong)value);
            if (littleEndian)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(span, encoded);
            }
            else
            {
                BinaryPrimitives.WriteUInt64BigEndian(span, encoded);
            }
        }

        [AuroraExport("getFloat32", MatchFailure.Throw)]
        public static double GetFloat32Core(
            ScriptUInt8Array buffer,
            int offset,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 4);
            return littleEndian
                ? BinaryPrimitives.ReadSingleLittleEndian(span)
                : BinaryPrimitives.ReadSingleBigEndian(span);
        }

        [AuroraExport("setFloat32", MatchFailure.Throw)]
        public static void SetFloat32Core(
            ScriptUInt8Array buffer,
            int offset,
            double value,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 4);
            var encoded = (float)value;
            if (littleEndian)
            {
                BinaryPrimitives.WriteSingleLittleEndian(span, encoded);
            }
            else
            {
                BinaryPrimitives.WriteSingleBigEndian(span, encoded);
            }
        }

        [AuroraExport("getFloat64", MatchFailure.Throw)]
        public static double GetFloat64Core(
            ScriptUInt8Array buffer,
            int offset,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 8);
            return littleEndian
                ? BinaryPrimitives.ReadDoubleLittleEndian(span)
                : BinaryPrimitives.ReadDoubleBigEndian(span);
        }

        [AuroraExport("setFloat64", MatchFailure.Throw)]
        public static void SetFloat64Core(
            ScriptUInt8Array buffer,
            int offset,
            double value,
            bool littleEndian = true)
        {
            var span = GetSpan(buffer, offset, 8);
            if (littleEndian)
            {
                BinaryPrimitives.WriteDoubleLittleEndian(span, value);
            }
            else
            {
                BinaryPrimitives.WriteDoubleBigEndian(span, value);
            }
        }

        [AuroraExport("getString", MatchFailure.Throw)]
        public static string GetStringCore(
            ScriptUInt8Array buffer,
            int offset,
            int byteLength)
        {
            if (byteLength < 0)
            {
                ThrowOutOfRange();
            }

            var span = GetSpan(buffer, offset, byteLength);
            return Encoding.UTF8.GetString(span);
        }

        [AuroraExport("setString", MatchFailure.Throw)]
        public static int SetStringCore(
            ScriptUInt8Array buffer,
            int offset,
            string value)
        {
            value ??= string.Empty;
            var byteLength = Encoding.UTF8.GetByteCount(value);
            var span = GetSpan(buffer, offset, byteLength);
            return Encoding.UTF8.GetBytes(value, span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<byte> GetSpan(ScriptUInt8Array buffer, int offset, int size)
        {
            var bytes = buffer._items.AsSpan();
            if ((uint)offset > (uint)bytes.Length ||
                (uint)size > (uint)(bytes.Length - offset))
            {
                ThrowOutOfRange();
            }

            return bytes.Slice(offset, size);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOutOfRange()
        {
            throw new AuroraRuntimeException("Conv8 offset is outside the buffer.");
        }
    }
}
