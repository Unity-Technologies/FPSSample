using System;
using NUnit.Framework;


namespace NetcodeTests
{
    [TestFixture]
    public class FloatTests
    {
        [Test]
        public void Float_TestFixedPrecision()
        {
            var random = new Random(21831);

            const int RUNS = 10000;
            var samples = new int[RUNS];
            for (int i = 0; i < RUNS; ++i)
                samples[i] = (int)(random.NextDouble() * random.Next(0, 10000) * 1000);

            //for (int i = 0; i < RUNS - 1; ++i)
            //{
            //    float a = samples[i];
            //    float b = samples[i + 1];

            //    int delta1 = (int)((a - 0) * 1000);
            //    int delta2 = (int)((b - a) * 1000);

            //    float expected = b;
            //    float resultSum = resultSum + 
            //    float result = delta1 / 1000.0f + delta2 / 1000.0f;

            //    Assert.IsTrue(Math.Abs(expected - result) < 0.002);
            //}

            int resultSum = 0;
            for (int i = 0; i < RUNS; ++i)
            {
                int a = i == 0 ? 0 : samples[i - 1];
                int b = samples[i];

                int delta = b - a;

                resultSum = resultSum + delta;

                float expected = b / 1000.0f;
                float actual = resultSum / 1000.0f;

                float diff = Math.Abs(expected - actual);
                Assert.IsTrue(diff < 0.001);
            }
        }
    }
}
