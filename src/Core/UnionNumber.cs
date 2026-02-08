using System.Runtime.InteropServices;

namespace AuroraScript.Core
{
    /// <summary>
    /// Represents a specialized numeric storage structure that uses a union layout to store multiple numeric types.
    /// This allows for efficient conversion and access of data as different numeric widths (Int64, Double, Int32, etc.)
    /// within a single 8-byte (64-bit) memory slot.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct UnionNumber
    {
        /// <summary> Initializes a new instance with four 16-bit integers. </summary>
        public UnionNumber(short hight1, short low1, short hight2, short low2)
        {
            Int16ValueH1 = hight1;
            Int16ValueL1 = low1;
            Int16ValueH2 = hight2;
            Int16ValueL2 = low2;
        }

        /// <summary> Initializes a new instance with four unsigned 16-bit integers. </summary>
        public UnionNumber(ushort hight1, ushort low1, ushort hight2, ushort low2)
        {
            UInt16ValueH1 = hight1;
            UInt16ValueL1 = low1;
            UInt16ValueH2 = hight2;
            UInt16ValueL2 = low2;
        }

        /// <summary> Initializes a new instance with two 32-bit integers (High and Low). </summary>
        public UnionNumber(int hight, int low)
        {
            Int32ValueH = hight;
            Int32ValueL = low;
        }

        /// <summary> Initializes a new instance with a 64-bit double precision floating point number. </summary>
        public UnionNumber(double doubleValue)
        {
            DoubleValue = doubleValue;
        }

        /// <summary> Initializes a new instance with a 64-bit signed integer. </summary>
        public UnionNumber(long int64Value)
        {
            Int64Value = int64Value;
        }

        /// <summary> Initializes a new instance with two 32-bit single precision floating point numbers. </summary>
        public UnionNumber(float hight, float low)
        {
            FloatValueH = hight;
            FloatValueL = low;
        }

        // --- 64-bit views ---
        /// <summary> Gets or sets the 64-bit signed integer value. </summary>
        [FieldOffset(0)] public long Int64Value;
        /// <summary> Gets or sets the 64-bit double precision floating point value. </summary>
        [FieldOffset(0)] public double DoubleValue;

        // --- 32-bit views ---
        /// <summary> Gets or sets the first 32-bit float value. </summary>
        [FieldOffset(0)] public float FloatValueH;
        /// <summary> Gets or sets the second 32-bit float value. </summary>
        [FieldOffset(4)] public float FloatValueL;
        /// <summary> Gets or sets the first 32-bit integer value. </summary>
        [FieldOffset(0)] public int Int32ValueH;
        /// <summary> Gets or sets the second 32-bit integer value. </summary>
        [FieldOffset(4)] public int Int32ValueL;

        // --- 16-bit views ---
        /// <summary> Gets or sets the first 16-bit integer part. </summary>
        [FieldOffset(0)] public short Int16ValueH1;
        /// <summary> Gets or sets the second 16-bit integer part. </summary>
        [FieldOffset(2)] public short Int16ValueL1;
        /// <summary> Gets or sets the third 16-bit integer part. </summary>
        [FieldOffset(4)] public short Int16ValueH2;
        /// <summary> Gets or sets the fourth 16-bit integer part. </summary>
        [FieldOffset(6)] public short Int16ValueL2;

        /// <summary> Gets or sets the first 16-bit unsigned integer part. </summary>
        [FieldOffset(0)] public ushort UInt16ValueH1;
        /// <summary> Gets or sets the second 16-bit unsigned integer part. </summary>
        [FieldOffset(2)] public ushort UInt16ValueL1;
        /// <summary> Gets or sets the third 16-bit unsigned integer part. </summary>
        [FieldOffset(4)] public ushort UInt16ValueH2;
        /// <summary> Gets or sets the fourth 16-bit unsigned integer part. </summary>
        [FieldOffset(6)] public ushort UInt16ValueL2;

        // --- Byte views ---
        /// <summary> Gets or sets the first byte. </summary>
        [FieldOffset(0)] public byte ByteValue1;
        /// <summary> Gets or sets the second byte. </summary>
        [FieldOffset(1)] public byte ByteValue2;
        /// <summary> Gets or sets the third byte. </summary>
        [FieldOffset(2)] public byte ByteValue3;
        /// <summary> Gets or sets the fourth byte. </summary>
        [FieldOffset(3)] public byte ByteValue4;
        /// <summary> Gets or sets the fifth byte. </summary>
        [FieldOffset(4)] public byte ByteValue5;
        /// <summary> Gets or sets the sixth byte. </summary>
        [FieldOffset(5)] public byte ByteValue6;
        /// <summary> Gets or sets the seventh byte. </summary>
        [FieldOffset(6)] public byte ByteValue7;
        /// <summary> Gets or sets the eighth byte. </summary>
        [FieldOffset(7)] public byte ByteValue8;

        // --- Boolean view ---
        /// <summary> Gets or sets the value as a boolean (mapped to offset 0). </summary>
        [FieldOffset(0)] public bool BooleanValue;
    }
}
