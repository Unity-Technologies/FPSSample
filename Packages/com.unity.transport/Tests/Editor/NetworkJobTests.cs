using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace Unity.Networking.Transport.Tests
{
    using LocalNetworkDriver = BasicNetworkDriver<IPCSocket>;
    using UdpCNetworkDriver = BasicNetworkDriver<IPv4UDPSocket>;

    public class NetworkJobTests
    {
        [SetUp]
        public void IPC_Setup()
        {
            IPCManager.Instance.Initialize(100);
        }

        [TearDown]
        public void IPC_TearDown()
        {
            IPCManager.Instance.Destroy();
        }

        void WaitForConnected(LocalNetworkDriver clientDriver, LocalNetworkDriver serverDriver,
            NetworkConnection clientToServer)
        {
            // Make sure connect message is sent
            clientDriver.ScheduleUpdate().Complete();
            // Make sure connection accept message is sent back
            serverDriver.ScheduleUpdate().Complete();
            // Handle the connection accept message
            clientDriver.ScheduleUpdate().Complete();
            DataStreamReader strmReader;
            // Make sure the connected message was received
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(clientDriver, out strmReader));            
        }
        
        [Test]
        public void ScheduleUpdateWorks()
        {
            var driver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            var updateHandle = driver.ScheduleUpdate();
            updateHandle.Complete();
            driver.Dispose();
        }
        [Test]
        public void ScheduleUpdateWithMissingDependencyThrowsException()
        {
            var driver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            var updateHandle = driver.ScheduleUpdate();
            Assert.Throws<InvalidOperationException>(() => { driver.ScheduleUpdate().Complete(); });
            updateHandle.Complete();
            driver.Dispose();
        }
        [Test]
        public void DataStremReaderIsOnlyUsableUntilUpdate()
        {
            var serverDriver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            serverDriver.Bind(IPCManager.Instance.CreateEndPoint());
            serverDriver.Listen();
            var clientDriver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            var clientToServer = clientDriver.Connect(serverDriver.LocalEndPoint());
            WaitForConnected(clientDriver, serverDriver, clientToServer);
            var strmWriter = new DataStreamWriter(4, Allocator.Temp);
            strmWriter.Write(42);
            clientToServer.Send(clientDriver, strmWriter);
            strmWriter.Dispose();
            clientDriver.ScheduleUpdate().Complete();
            var serverToClient = serverDriver.Accept();
            serverDriver.ScheduleUpdate().Complete();
            DataStreamReader strmReader;
            Assert.AreEqual(NetworkEvent.Type.Data, serverToClient.PopEvent(serverDriver, out strmReader));
            var ctx = default(DataStreamReader.Context);
            Assert.AreEqual(42, strmReader.ReadInt(ref ctx));
            ctx = default(DataStreamReader.Context);
            Assert.AreEqual(42, strmReader.ReadInt(ref ctx));
            serverDriver.ScheduleUpdate().Complete();
            ctx = default(DataStreamReader.Context);
            Assert.Throws<InvalidOperationException>(() => { strmReader.ReadInt(ref ctx); });
            clientDriver.Dispose();
            serverDriver.Dispose();
        }

        struct AcceptJob : IJob
        {
            public LocalNetworkDriver driver;
            public NativeArray<NetworkConnection> connections;
            public void Execute()
            {
                for (int i = 0; i < connections.Length; ++i)
                    connections[i] = driver.Accept();
            }
        }
        [Test]
        public void AcceptInJobWorks()
        {
            var serverDriver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            serverDriver.Bind(IPCManager.Instance.CreateEndPoint());
            serverDriver.Listen();
            var clientDriver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            /*var clientToServer =*/ clientDriver.Connect(serverDriver.LocalEndPoint());
            clientDriver.ScheduleUpdate().Complete();

            var serverToClient = new NativeArray<NetworkConnection>(1, Allocator.TempJob);
            var acceptJob = new AcceptJob {driver = serverDriver, connections = serverToClient};
            Assert.IsFalse(serverToClient[0].IsCreated);
            acceptJob.Schedule(serverDriver.ScheduleUpdate()).Complete();
            Assert.IsTrue(serverToClient[0].IsCreated);

            serverToClient.Dispose();
            clientDriver.Dispose();
            serverDriver.Dispose();
        }
        struct ReceiveJob : IJob
        {
            public LocalNetworkDriver driver;
            public NativeArray<NetworkConnection> connections;
            public NativeArray<int> result;
            public void Execute()
            {
                DataStreamReader strmReader;
                // Data
                connections[0].PopEvent(driver, out strmReader);
                var ctx = default(DataStreamReader.Context);
                result[0] = strmReader.ReadInt(ref ctx);
            }
        }
        [Test]
        public void ReceiveInJobWorks()
        {
            var serverDriver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            serverDriver.Bind(IPCManager.Instance.CreateEndPoint());
            serverDriver.Listen();
            var clientDriver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            var clientToServer = clientDriver.Connect(serverDriver.LocalEndPoint());
            WaitForConnected(clientDriver, serverDriver, clientToServer);
            var strmWriter = new DataStreamWriter(4, Allocator.Temp);
            strmWriter.Write(42);
            clientToServer.Send(clientDriver, strmWriter);
            strmWriter.Dispose();
            clientDriver.ScheduleUpdate().Complete();

            var serverToClient = new NativeArray<NetworkConnection>(1, Allocator.TempJob);
            var result = new NativeArray<int>(1, Allocator.TempJob);
            var recvJob = new ReceiveJob {driver = serverDriver, connections = serverToClient, result = result};
            Assert.AreNotEqual(42, result[0]);
            var acceptJob = new AcceptJob {driver = serverDriver, connections = serverToClient};
            recvJob.Schedule(serverDriver.ScheduleUpdate(acceptJob.Schedule())).Complete();
            Assert.AreEqual(42, result[0]);

            result.Dispose();
            serverToClient.Dispose();
            clientDriver.Dispose();
            serverDriver.Dispose();
        }

        struct SendJob : IJob
        {
            public LocalNetworkDriver driver;
            public NetworkConnection connection;
            public void Execute()
            {
                var strmWriter = new DataStreamWriter(4, Allocator.Temp);
                strmWriter.Write(42);
                connection.Send(driver, strmWriter);
                strmWriter.Dispose();
            }
        }
        [Test]
        public void SendInJobWorks()
        {
            var serverDriver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            serverDriver.Bind(IPCManager.Instance.CreateEndPoint());
            serverDriver.Listen();
            var clientDriver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            var clientToServer = clientDriver.Connect(serverDriver.LocalEndPoint());
            WaitForConnected(clientDriver, serverDriver, clientToServer);
            var sendJob = new SendJob {driver = clientDriver, connection = clientToServer};
            clientDriver.ScheduleUpdate(sendJob.Schedule()).Complete();
            var serverToClient = serverDriver.Accept();
            serverDriver.ScheduleUpdate().Complete();
            DataStreamReader strmReader;
            Assert.AreEqual(NetworkEvent.Type.Data, serverToClient.PopEvent(serverDriver, out strmReader));
            var ctx = default(DataStreamReader.Context);
            Assert.AreEqual(42, strmReader.ReadInt(ref ctx));
            clientDriver.Dispose();
            serverDriver.Dispose();
        }
        struct SendReceiveParallelJob : IJobParallelFor
        {
            public LocalNetworkDriver.Concurrent driver;
            public NativeArray<NetworkConnection> connections;
            public void Execute(int i)
            {
                DataStreamReader strmReader;
                // Data
                if (driver.PopEventForConnection(connections[i], out strmReader) != NetworkEvent.Type.Data)
                    throw new InvalidOperationException("Expected data: " + i);
                var ctx = default(DataStreamReader.Context);
                int result = strmReader.ReadInt(ref ctx);
                var strmWriter = new DataStreamWriter(4, Allocator.Temp);
                strmWriter.Write(result + 1);
                driver.Send(connections[i], strmWriter);
                strmWriter.Dispose();
            }
        }
        [Test]
        public void SendReceiveInParallelJobWorks()
        {
            var serverDriver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            serverDriver.Bind(IPCManager.Instance.CreateEndPoint());
            serverDriver.Listen();
            var clientDriver0 = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            var clientDriver1 = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            var serverToClient = new NativeArray<NetworkConnection>(2, Allocator.TempJob);
            var strmWriter = new DataStreamWriter(4, Allocator.Temp);
            strmWriter.Write(42);
            var clientToServer0 = clientDriver0.Connect(serverDriver.LocalEndPoint());
            var clientToServer1 = clientDriver1.Connect(serverDriver.LocalEndPoint());
            WaitForConnected(clientDriver0, serverDriver, clientToServer0);
            serverToClient[0] = serverDriver.Accept();
            Assert.IsTrue(serverToClient[0].IsCreated);
            WaitForConnected(clientDriver1, serverDriver, clientToServer1);
            serverToClient[1] = serverDriver.Accept();
            Assert.IsTrue(serverToClient[1].IsCreated);
            clientToServer0.Send(clientDriver0, strmWriter);
            clientToServer1.Send(clientDriver1, strmWriter);
            strmWriter.Dispose();
            clientDriver0.ScheduleUpdate().Complete();
            clientDriver1.ScheduleUpdate().Complete();

            var sendRecvJob = new SendReceiveParallelJob {driver = serverDriver.ToConcurrent(), connections = serverToClient};
            var jobHandle = serverDriver.ScheduleUpdate();
            jobHandle = sendRecvJob.Schedule(serverToClient.Length, 1, jobHandle);
            serverDriver.ScheduleUpdate(jobHandle).Complete();
            
            DataStreamReader strmReader;
            clientDriver0.ScheduleUpdate().Complete();
            Assert.AreEqual(NetworkEvent.Type.Data, clientToServer0.PopEvent(clientDriver0, out strmReader));
            var ctx = default(DataStreamReader.Context);
            Assert.AreEqual(43, strmReader.ReadInt(ref ctx));
            clientDriver1.ScheduleUpdate().Complete();
            Assert.AreEqual(NetworkEvent.Type.Data, clientToServer1.PopEvent(clientDriver1, out strmReader));
            ctx = default(DataStreamReader.Context);
            Assert.AreEqual(43, strmReader.ReadInt(ref ctx));

            serverToClient.Dispose();
            clientDriver0.Dispose();
            clientDriver1.Dispose();
            serverDriver.Dispose();
        }
    }
}