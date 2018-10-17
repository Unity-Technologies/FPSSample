namespace NetworkCompression
{
    public interface IInputStream
    {
        void Initialize(NetworkCompressionModel model, byte[] buffer, int bufferOffset);

        uint ReadRawBits(int numbits);
        void ReadRawBytes(byte[] dstBuffer, int dstIndex, int count);

        void SkipRawBits(int numbits);
        void SkipRawBytes(int count);


        uint ReadPackedNibble(int context);
        uint ReadPackedUInt(int context);
        int ReadPackedIntDelta(int baseline, int context);
        uint ReadPackedUIntDelta(uint baseline, int context);

        int GetBitPosition2();
        NetworkCompressionModel GetModel();
    }
}