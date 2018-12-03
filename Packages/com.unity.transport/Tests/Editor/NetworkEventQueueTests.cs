using NUnit.Framework;
namespace Unity.Networking.Transport.Tests
{
    public class NetworkEventQueueTests
    {
        [Test]
        public void PopFromEmptyQueueReturnsEmpty()
        {
            var queue = new NetworkEventQueue(1);
            int offset, size;
            Assert.AreEqual(NetworkEvent.Type.Empty, queue.PopEventForConnection(0, out offset, out size));
            queue.Dispose();
        }

        [Test]
        public void PushToSingleConnectionCanBePoppedPerConnection()
        {
            var queue = new NetworkEventQueue(1);
            int offset, size;
            queue.PushEvent(new NetworkEvent{connectionId = 0, offset = 0, size = 0, type = NetworkEvent.Type.Data});
            Assert.AreEqual(NetworkEvent.Type.Data, queue.PopEventForConnection(0, out offset, out size));
            Assert.AreEqual(NetworkEvent.Type.Empty, queue.PopEventForConnection(0, out offset, out size));
            queue.Dispose();
        }
        
        [Test]
        public void PushToSingleConnectionCanBePoppedGlobally()
        {
            var queue = new NetworkEventQueue(1);
            int offset, size;
            int id;
            queue.PushEvent(new NetworkEvent{connectionId = 0, offset = 0, size = 0, type = NetworkEvent.Type.Data});
            Assert.AreEqual(NetworkEvent.Type.Data, queue.PopEvent(out id, out offset, out size));
            Assert.AreEqual(0, id);
            Assert.AreEqual(NetworkEvent.Type.Empty, queue.PopEvent(out id, out offset, out size));
            queue.Dispose();
        }

        [Test]
        public void PushToSingleConnectionCanGrowMaxEvents()
        {
            var queue = new NetworkEventQueue(1);
            int offset, size;
            for (int i = 0; i < 16; ++i)
                queue.PushEvent(new NetworkEvent{connectionId = 0, offset = 0, size = 0, type = NetworkEvent.Type.Data});
            for (int i = 0; i < 16; ++i)
                Assert.AreEqual(NetworkEvent.Type.Data, queue.PopEventForConnection(0, out offset, out size));
            Assert.AreEqual(NetworkEvent.Type.Empty, queue.PopEventForConnection(0, out offset, out size));
            queue.Dispose();
        }

        [Test]
        public void PushToMultipleConnectionsCanGrowMaxEvents()
        {
            var queue = new NetworkEventQueue(1);
            int offset, size;
            for (int con = 0; con < 16; ++con)
            {
                for (int i = 0; i < con; ++i)
                {
                    queue.PushEvent(new NetworkEvent
                        {connectionId = con, offset = con, size = i, type = NetworkEvent.Type.Data});
                }
            }

            for (int con = 0; con < 16; ++con)
            {
                for (int i = 0; i < con; ++i)
                {
                    Assert.AreEqual(NetworkEvent.Type.Data, queue.PopEventForConnection(con, out offset, out size));
                    Assert.AreEqual(con, offset);
                    Assert.AreEqual(i, size);
                }

                Assert.AreEqual(NetworkEvent.Type.Empty, queue.PopEventForConnection(con, out offset, out size));
            }
            queue.Dispose();
        }

        [Test]
        public void PopMixingMethodsOnSingleConnectionWorks()
        {
            var queue = new NetworkEventQueue(16);
            int offset, size;
            for (int i = 0; i < 16; ++i)
                queue.PushEvent(new NetworkEvent{connectionId = 0, offset = i, size = 0, type = NetworkEvent.Type.Data});
            int id;
            for (int i = 0; i < 16/2; ++i)
            {
                Assert.AreEqual(NetworkEvent.Type.Data, queue.PopEvent(out id, out offset, out size));
                Assert.AreEqual(0, id);
                Assert.AreEqual(i*2, offset);
                Assert.AreEqual(NetworkEvent.Type.Data, queue.PopEventForConnection(0, out offset, out size));
                Assert.AreEqual(i*2+1, offset);
            }

            Assert.AreEqual(NetworkEvent.Type.Empty, queue.PopEvent(out id, out offset, out size));
            Assert.AreEqual(NetworkEvent.Type.Empty, queue.PopEventForConnection(0, out offset, out size));
            queue.Dispose();
        }

        [Test]
        public void PopMixingMethodsOnMultipleConnectionsWorks()
        {
            var queue = new NetworkEventQueue(16);
            int offset, size;
            for (int i = 0; i < 16; ++i)
            {
                for (int con = 0; con < 16; ++con)
                    queue.PushEvent(new NetworkEvent
                        {connectionId = con, offset = con, size = i, type = NetworkEvent.Type.Data});
            }

            // Pop half the events from connection 10, make sure everything else is still in sync
            
            int id;
            for (int i = 0; i < 8; ++i)
            {
                Assert.AreEqual(NetworkEvent.Type.Data, queue.PopEventForConnection(10, out offset, out size));
                Assert.AreEqual(10, offset);
                Assert.AreEqual(i, size);
            }
            for (int i = 0; i < 16; ++i)
            {
                for (int con = 0; con < 16; ++con)
                {
                    if (con == 10 && i < 8)
                        continue;
                    Assert.AreEqual(NetworkEvent.Type.Data, queue.PopEvent(out id, out offset, out size));
                    Assert.AreEqual(con, id);
                    Assert.AreEqual(con, offset);
                    Assert.AreEqual(i, size);
                }
            }

            Assert.AreEqual(NetworkEvent.Type.Empty, queue.PopEventForConnection(10, out offset, out size));
            Assert.AreEqual(NetworkEvent.Type.Empty, queue.PopEvent(out id, out offset, out size));
            queue.Dispose();
        }
    }

    public class NetworkEventQueueConcurrentTests
    {
        [Test]
        public void PopFromEmptyQueueReturnsEmpty()
        {
            var queue = new NetworkEventQueue(1);
            var cq = queue.ToConcurrent();
            int offset, size;
            Assert.AreEqual(NetworkEvent.Type.Empty, cq.PopEventForConnection(0, out offset, out size));
            queue.Dispose();
        }
        [Test]
        public void PopFromSingleConnectionWorks()
        {
            var queue = new NetworkEventQueue(1);
            var cq = queue.ToConcurrent();
            int offset, size;
            queue.PushEvent(new NetworkEvent{connectionId = 0, offset = 0, size = 0, type = NetworkEvent.Type.Data});
            Assert.AreEqual(NetworkEvent.Type.Data, cq.PopEventForConnection(0, out offset, out size));
            Assert.AreEqual(NetworkEvent.Type.Empty, cq.PopEventForConnection(0, out offset, out size));
            queue.Dispose();
        }
        [Test]
        public void PopFromMultipleConnectionsWorks()
        {
            var queue = new NetworkEventQueue(1);
            var cq = queue.ToConcurrent();
            int offset, size;
            for (int i = 0; i < 16; ++i)
                queue.PushEvent(new NetworkEvent{connectionId = i, offset = i, size = 0, type = NetworkEvent.Type.Data});
            for (int i = 0; i < 16; ++i)
            {
                Assert.AreEqual(NetworkEvent.Type.Data, cq.PopEventForConnection(i, out offset, out size));
                Assert.AreEqual(i, offset);
                Assert.AreEqual(NetworkEvent.Type.Empty, cq.PopEventForConnection(i, out offset, out size));
            }

            queue.Dispose();
        }
        [Test]
        public void PopFromMultipleConnectionsWithGrowingEventsWorks()
        {
            var queue = new NetworkEventQueue(1);
            var cq = queue.ToConcurrent();
            int offset, size;
            for (int con = 0; con < 16; ++con)
            {
                for (int i = 0; i < con; ++i)
                {
                    queue.PushEvent(new NetworkEvent
                        {connectionId = con, offset = con, size = i, type = NetworkEvent.Type.Data});
                }
            }

            for (int con = 0; con < 16; ++con)
            {
                for (int i = 0; i < con; ++i)
                {
                    Assert.AreEqual(NetworkEvent.Type.Data, cq.PopEventForConnection(con, out offset, out size));
                    Assert.AreEqual(con, offset);
                    Assert.AreEqual(i, size);
                }

                Assert.AreEqual(NetworkEvent.Type.Empty, cq.PopEventForConnection(con, out offset, out size));
            }
            queue.Dispose();
        }
    }
}