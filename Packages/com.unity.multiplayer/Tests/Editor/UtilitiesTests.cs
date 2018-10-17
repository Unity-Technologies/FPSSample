using System;
using NUnit.Framework;

using Experimental.Multiplayer.Utilities;

namespace Experimental.Multiplayer.Tests
{
    public class BucketQ_Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void BucketQ_SimpleScenarios()
        {
            using (BucketQ<int> eventQ = new BucketQ<int>(5, 5))
            {
                for (int connection = 0; connection < 5; connection++)
                {

                    // Test Add
                    int item = 0;

                    Assert.True(eventQ.Enqueue(connection, 1) == 0);
                    Assert.True(eventQ.Enqueue(connection, 1) == 0);
                    Assert.True(eventQ.Enqueue(connection, 1) == 0);
                    Assert.True(eventQ.Enqueue(connection, 1) == 0);
                    Assert.True(eventQ.Enqueue(connection, 1) == 0);

                    // Fail Add
                    Assert.True(eventQ.Enqueue(connection, 1) == -1);

                    // Test Rem
                    Assert.True(eventQ.TryDequeue(connection, out item));
                    Assert.True(eventQ.TryDequeue(connection, out item));
                    Assert.True(eventQ.TryDequeue(connection, out item));
                    Assert.True(eventQ.TryDequeue(connection, out item));
                    Assert.True(eventQ.TryDequeue(connection, out item));

                    // Fail Rem
                    Assert.True(!eventQ.TryDequeue(connection, out item));

                    // Multiple inserts and removals
                    System.Random r = new System.Random();


                    int steps = 5;
                    for (int s = 1; s < steps; s++)
                    {
                        for (int i = 0; i < 1000; i++)
                        {
                            var it = r.Next(1, 1000);
                            for (int j = 0; j < s; j++)
                            {
                                Assert.True(eventQ.Enqueue(connection, it + j) == 0);
                            }

                            for (int j = 0; j < s; j++)
                            {
                                int o;
                                Assert.True(eventQ.TryDequeue(connection, out o));
                                Assert.True(o == (it + j));
                            }
                        }
                    }
                }
            }
        }
    }
}
