using System;
using NUnit.Framework;
using Unity.Networking.Transport.Utilities;

namespace Unity.Networking.Transport.Tests
{
    public class NativeMultiQueue_Tests
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
        public void NativeMultiQueue_SimpleScenarios()
        {
            using (NativeMultiQueue<int> eventQ = new NativeMultiQueue<int>(5))
            {
                for (int connection = 0; connection < 5; connection++)
                {
                    // Test Add
                    int item = 0;

                    eventQ.Enqueue(connection, 1);
                    eventQ.Enqueue(connection, 1);
                    eventQ.Enqueue(connection, 1);
                    eventQ.Enqueue(connection, 1);
                    eventQ.Enqueue(connection, 1);

                    // Add grows capacity
                    eventQ.Enqueue(connection, 1);

                    // Test Rem
                    Assert.True(eventQ.Dequeue(connection, out item));
                    Assert.True(eventQ.Dequeue(connection, out item));
                    Assert.True(eventQ.Dequeue(connection, out item));
                    Assert.True(eventQ.Dequeue(connection, out item));
                    Assert.True(eventQ.Dequeue(connection, out item));

                    // Remove with grown capacity
                    Assert.True(eventQ.Dequeue(connection, out item));
                }
            }
        }
    }
}