using UnityEngine;

namespace NetworkCompression
{
    public enum IOStreamType
    {
        Raw,
        Huffman
    }

    public static class NetworkCompressionConstants
    {
        public const int k_NumBuckets = 16;
        public const int k_MaxHuffmanSymbolLength = 6;

        public static byte[] k_BucketSizes = new byte[]
        {
        0, 0, 1, 2, 3, 4, 6, 8, 10, 12, 15, 18, 21, 24, 27, 32
        };

        public static uint[] k_BucketOffsets = new uint[]
        {
        0, 1, 2, 4, 8, 16, 32, 96, 352, 1376, 5472, 38240, 300384, 2397536, 19174752, 153392480
        };
    }
}