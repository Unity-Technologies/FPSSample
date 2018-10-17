using System.Runtime.InteropServices;

class ConversionUtility
{
    public static float UInt32ToFloat(uint value) { return new UIntFloat() { intValue = value }.floatValue; }
    public static uint FloatToUInt32(float value) { return new UIntFloat() { floatValue = value }.intValue; }

    public static double DoubleToUInt64(ulong value) { return new ULongDouble() { longValue = value }.doubleValue; }
    public static ulong UInt64ToDouble(double value) { return new ULongDouble() { doubleValue = value }.longValue; }

    [StructLayout(LayoutKind.Explicit)]
    struct UIntFloat
    {
        [FieldOffset(0)]
        public float floatValue;
        [FieldOffset(0)]
        public uint intValue;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct ULongDouble
    {
        [FieldOffset(0)]
        public double doubleValue;
        [FieldOffset(0)]
        public ulong longValue;
    }
}
