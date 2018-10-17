using NUnit.Framework;
using System;

namespace NetcodeTests
{
    [TestFixture]
    public class BitStreamTests
    {
        [Test]
        public void BitStream_UIntPacked_RandomUInt()
        {
            var buffer = new byte[1024 * 64];
            var output = new BitOutputStream(buffer);

            var values = new uint[1024];
            var random = new Random(1032);
            for (int i = 0; i < 1024; ++i)
            {
                values[i] = (uint)random.Next(int.MaxValue);
                output.WriteUIntPacked(values[i]);
            }
            output.Flush();

            var input = new BitInputStream(buffer);
            for (int i = 0; i < 1024; ++i)
            {
                var value = input.ReadUIntPacked();
                Assert.AreEqual(values[i], value);
            }
        }

        [Test]
        public void BitStream_UIntPacked_RandomByte()
        {
            var buffer = new byte[1024 * 64];
            var output = new BitOutputStream(buffer);

            var values = new uint[1024];
            var random = new Random(1032);
            for (int i = 0; i < 1024; ++i)
            {
                values[i] = (uint)random.Next(255);
                output.WriteUIntPacked(values[i]);
            }
            output.Flush();

            var input = new BitInputStream(buffer);
            for (int i = 0; i < 1024; ++i)
            {
                var value = input.ReadUIntPacked();
                Assert.AreEqual(values[i], value);
            }
        }

        [Test]
        public void BitStream_IntDelta_Random()
        {
            var buffer = new byte[1024 * 64];
            var output = new BitOutputStream(buffer);

            var values = new int[1024];
            var random = new Random(1032);
            long previous = 0;
            for (int i = 0; i < 1024; ++i)
            {
                values[i] = random.Next(int.MinValue, int.MaxValue);
                output.WriteIntDelta(values[i], previous);
                previous = values[i];
            }
            output.Flush();

            var input = new BitInputStream(buffer);
            previous = 0;
            for (int i = 0; i < 1024; ++i)
            {
                var value = input.ReadIntDelta(previous);
                Assert.AreEqual(values[i], value);
                previous = value;
            }
        }

        [Test]
        public void BitStream_Align()
        {
            var random = new Random(1293);

            var numbers = new int[1024];
            var buffer = new byte[1024 * 64];

            for (int runs = 0; runs < 1000; ++runs)
            {
                for (int i = 0; i < 1024; ++i)
                    numbers[i] = random.Next(1, 33);

                var output = new BitOutputStream(buffer);
                for (int i = 0; i < 1024; ++i)
                {
                    output.WriteBits((uint)numbers[i], numbers[i]);
                    if(i % 3 == 0)
                        output.Align();
                }

                var input = new BitInputStream(buffer);
                for (int i = 0; i < 1024; ++i)
                {
                    var value = input.ReadBits(numbers[i]);
                    Assert.AreEqual((uint)numbers[i], value);
                    if (i % 3 == 0)
                        input.Align();
                }
            }
        }

        [Test]
        public void BitStream_AlignAndByteArray()
        {
            var random = new Random(1293);

            var numbers = new int[1024];

            var payload = new byte[32];
            var payloadCompare = new byte[32];
            random.NextBytes(payload);

            var buffer = new byte[1024 * 1024];

            for (int runs = 0; runs < 1; ++runs)
            {
                for (int i = 0; i < 1024; ++i)
                    numbers[i] = random.Next(1, 33);

                var output = new BitOutputStream(buffer);
                for (int i = 0; i < 1024; ++i)
                {
                    output.WriteBits((uint)numbers[i], numbers[i]);
                    if (i % 3 == 0)
                        output.WriteBytes(payload, 0, numbers[i]);
                }

                var input = new BitInputStream(buffer);
                for (int i = 0; i < 1024; ++i)
                {
                    var value = input.ReadBits(numbers[i]);
                    Assert.AreEqual((uint)numbers[i], value);
                    if (i % 3 == 0)
                    {
                        input.ReadBytes(payloadCompare, 0, numbers[i]);
                        Assert.AreEqual(0, NetworkUtils.MemCmp(payload, 0, payloadCompare, 0, numbers[i]));
                    }
                }
            }
        }
    }
}
