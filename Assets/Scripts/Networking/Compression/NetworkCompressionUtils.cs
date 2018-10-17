using UnityEngine;

namespace NetworkCompression
{
    public static class NetworkCompressionUtils
    {
        public static int CalculateBucket(uint value)
        {
            int bucketIndex = 0;
            while (bucketIndex + 1 < NetworkCompressionConstants.k_NumBuckets && value >= NetworkCompressionConstants.k_BucketOffsets[bucketIndex + 1]) // TODO: use CountLeadingZeros to do this in constant time
                bucketIndex++;

            return bucketIndex;
        }

        public static int CountLeadingZeros(uint value)
        {
            int n = 0;
            while (value != 0)
            {
                value >>= 1;
                n++;
            }
            return 32 - n;
        }

        public static int CalculateNumGammaBits(long value)
        {
            int numOutputBits = 1;
            int numPrefixBits = 0;
            while (value >= (1L << numOutputBits))  // RUTODO: Unroll this and merge with bit output. How do we actually verify inlining in C#?
            {
                value -= (1L << numOutputBits);
                numOutputBits += 2;
                numPrefixBits++;
            }
            return numPrefixBits + 1 + numOutputBits;
        }



        public static void GenerateLengthLimitedHuffmanCodeLengths(byte[] symbolLengths, int symbolLengthsOffset, int[] symbolFrequencies, int alphabetSize, int maxCodeLength)
        {
            GameDebug.Assert(alphabetSize <= 255);
            GameDebug.Assert(maxCodeLength <= 8);

            var sortEntries = new SortEntry[alphabetSize];
            int lastNonZeroIndex = 0;
            int numSortEntries = 0;
            for (int symbol = 0; symbol < alphabetSize; symbol++)
            {
                symbolLengths[symbol + symbolLengthsOffset] = 0;

                int frequency = symbolFrequencies[symbol];
                if (frequency > 0)
                {
                    lastNonZeroIndex = symbol;
                    sortEntries[numSortEntries].frequency = frequency;
                    sortEntries[numSortEntries].symbol = (byte)symbol;
                    numSortEntries++;
                }
            }

            if (numSortEntries == 1)
            {
                symbolLengths[lastNonZeroIndex] = 1;
                return;
            }

            System.Array.Resize(ref sortEntries, numSortEntries);
            System.Array.Sort(sortEntries, (a, b) => a.frequency - b.frequency);

            int numNodes = alphabetSize * maxCodeLength * 2;
            var nodes = new Node[numNodes];
            for (int i = 0; i < numNodes; i++)
                nodes[i] = new Node();

            int nodesPointer = 0;
            int numPrevNodes = 0;
            int prevNodesPointer = 0;
            for (int length = 1; length <= maxCodeLength; length++)
            {
                int num_a = numPrevNodes;
                int num_b = numSortEntries;

                int aPointer = prevNodesPointer;
                prevNodesPointer = nodesPointer;
                numPrevNodes = 0;
                while (num_a >= 2 || num_b > 0)
                {
                    if (num_b > 0 && (num_a < 2 || sortEntries[numSortEntries - num_b].frequency <= nodes[aPointer + 0].frequency + nodes[aPointer + 1].frequency))
                    {
                        var e = sortEntries[numSortEntries - num_b];
                        var node = nodes[nodesPointer++];
                        node.frequency = e.frequency;
                        node.symbol = e.symbol;
                        num_b--;
                    }
                    else
                    {
                        var node = nodes[nodesPointer++];
                        node.frequency = nodes[aPointer + 0].frequency + nodes[aPointer + 1].frequency;
                        node.symbol = 0xFF;
                        node.leftChild = nodes[aPointer + 0];
                        node.rightChild = nodes[aPointer + 1];
                        num_a -= 2;
                        aPointer += 2;
                    }
                    numPrevNodes++;
                }
            }

            int numActive = 2 * numSortEntries - 2;
            for (int i = 0; i < numActive; i++)
            {
                GenerateLengthLimitedHuffmanCodeLengthsRecursive(nodes[prevNodesPointer + i], symbolLengths);
            }
        }

        public static void GenerateHuffmanCodes(byte[] symboLCodes, int symbolCodesOffset, byte[] symbolLengths, int symbolLengthsOffset, int alphabetSize, int maxCodeLength)
        {
            GameDebug.Assert(alphabetSize <= 256);
            GameDebug.Assert(maxCodeLength <= 8);

            var lengthCounts = new byte[maxCodeLength + 1];
            var symbolList = new byte[maxCodeLength + 1, alphabetSize];

            //byte[] symbol_list[(MAX_HUFFMAN_CODE_LENGTH + 1u) * MAX_NUM_HUFFMAN_SYMBOLS];
            for (int symbol = 0; symbol < alphabetSize; symbol++)
            {
                int length = symbolLengths[symbol + symbolLengthsOffset];
                GameDebug.Assert(length <= maxCodeLength);
                symbolList[length, lengthCounts[length]++] = (byte)symbol;
            }

            uint nextCodeWord = 0;
            for (int length = 1; length <= maxCodeLength; length++)
            {
                int length_count = lengthCounts[length];
                for (int i = 0; i < length_count; i++)
                {
                    int symbol = symbolList[length, i];
                    GameDebug.Assert(symbolLengths[symbol + symbolLengthsOffset] == length);
                    symboLCodes[symbol + symbolCodesOffset] = (byte)ReverseBits(nextCodeWord++, length);
                }
                nextCodeWord <<= 1;
            }
        }

        // decode table entries: (symbol << 8) | length
        public static void GenerateHuffmanDecodeTable(ushort[] decodeTable, int decodeTableOffset, byte[] symbolLengths, byte[] symbolCodes, int alphabetSize, int maxCodeLength)
        {
            GameDebug.Assert(alphabetSize <= 256);
            GameDebug.Assert(maxCodeLength <= 8);

            uint maxCode = 1u << maxCodeLength;
            for (int symbol = 0; symbol < alphabetSize; symbol++)
            {
                int length = symbolLengths[symbol];
                GameDebug.Assert(length <= maxCodeLength);
                if (length > 0)
                {
                    uint code = symbolCodes[symbol];
                    uint step = 1u << length;
                    do
                    {
                        decodeTable[decodeTableOffset + code] = (ushort)(symbol << 8 | length);
                        code += step;
                    } while (code < maxCode);
                }
            }
        }

        private static uint ReverseBits(uint value, int num_bits)
        {
            value = ((value & 0x55555555u) << 1) | ((value & 0xAAAAAAAAu) >> 1);
            value = ((value & 0x33333333u) << 2) | ((value & 0xCCCCCCCCu) >> 2);
            value = ((value & 0x0F0F0F0Fu) << 4) | ((value & 0xF0F0F0F0u) >> 4);
            value = ((value & 0x00FF00FFu) << 8) | ((value & 0xFF00FF00u) >> 8);
            value = (value << 16) | (value >> 16);
            return value >> (32 - num_bits);
        }

        private class Node
        {
            public byte symbol;
            public int frequency;
            public Node leftChild;
            public Node rightChild;
        };

        private struct SortEntry
        {
            public byte symbol;
            public int frequency;
        };

        private static void GenerateLengthLimitedHuffmanCodeLengthsRecursive(Node node, byte[] symbolLengths)
        {
            if (node.symbol == 0xFF)
            {
                GenerateLengthLimitedHuffmanCodeLengthsRecursive(node.leftChild, symbolLengths);
                GenerateLengthLimitedHuffmanCodeLengthsRecursive(node.rightChild, symbolLengths);
            }
            else
            {
                symbolLengths[node.symbol]++;
            }
        }
    }
}