using System;
using NUnit.Framework;


namespace NetcodeTests
{
    [TestFixture]
    public class ToUInt16Tests
    {
        [Test]
        public void Sequence_ToUInt16_NegativeThrows()
        {
            //Assert.Throws<ApplicationException>(() =>
            //{
            //    Sequence.ToUInt16(-1);
            //});
        }

        [Test]
        public void Sequence_ToUInt16_TestValid()
        {
            Assert.AreEqual(0, Sequence.ToUInt16(0));
            Assert.AreEqual(ushort.MaxValue, Sequence.ToUInt16(ushort.MaxValue));

            for (int i = 7; i < ushort.MaxValue; i += 100)
                Assert.AreEqual(i, Sequence.ToUInt16(i));
        }

        [Test]
        public void Sequence_ToUInt16_TestOverflow()
        {
            ushort sequence = (ushort.MaxValue - 10);
            for (int i = sequence; i < sequence + 20; ++i, ++sequence)
                Assert.AreEqual(sequence, Sequence.ToUInt16(i));
        }

        [Test]
        public void Sequence_FromUInt16_TestValid()
        {
            int[] diffs = { 0, 1, -1, -7, 29, -2391, 9430, -13120, 14091 };

            for (int i = 0; i < 100000; i += 337)
            {
                foreach (var diff in diffs)
                {
                    int test = i + diff;
                    if (test > 0)
                    {
                        ushort reduced = Sequence.ToUInt16(test);
                        int restored = Sequence.FromUInt16(reduced, i);
                        Assert.AreEqual(restored, test);
                    }
                }
            }
        }

        [Test]
        public void Sequence_FromUInt16_TestInValid()
        {
            int[] diffs = { 35012, -41092 };

            for (int i = 0; i < 100000; i += 337)
            {
                foreach (var diff in diffs)
                {
                    int test = i + diff;
                    if (test > 0)
                    {
                        ushort reduced = Sequence.ToUInt16(test);
                        int restored = Sequence.FromUInt16(reduced, i);
                        Assert.AreEqual(-1, restored);
                    }
                }
            }
        }
    }
}