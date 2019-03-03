using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NetcodeTests
{
    [TestFixture]
    public class ReaderWriterTests
    {
        [Test]
        public void NetworkReadWrite_TestRandomValues()
        {
            var random = new System.Random(192831);

            var buffer = new uint[1024 * 1024];
            for (int i = 0; i < 100; ++i)
            {
                var schema = NetworkTestUtils.GenerateRandomSchema(64, random.Next());
                var values = NetworkTestUtils.GenerateRandomValues(schema, random.Next());

                NetworkTestUtils.WriteValues(values, buffer, schema);
                NetworkTestUtils.ReadAndAssertValues(values, buffer, schema);
            }
        }
    }
}
