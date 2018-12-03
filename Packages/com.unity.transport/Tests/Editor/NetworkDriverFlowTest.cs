using System;
using System.Diagnostics;
using NUnit.Framework;
using System.Net;
using Unity.Networking.Transport.Tests.Helpers;
using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Networking.Transport.Tests
{
    namespace Helpers
    {
        class SharedConstants
        {
            public const float TickRate = 60.0f;
            public const float TickInterval = 1.0f / TickRate;
        }

        public class UnreliableClient : IDisposable
        {
            public void Dispose()
            {
            }

            public void Tick()
            {
            }
        }

        public class UnreliableServer : IDisposable
        {
            private BasicNetworkDriver<IPCSocket> m_Driver;

            //private List<NetworkConnection> m_Connections;

            public UnreliableServer()
            {
                m_Driver = new BasicNetworkDriver<IPCSocket>(new NetworkDataStreamParameter
                    {size = NetworkParameterConstants.MTU});
            }

            public void Host(NetworkEndPoint endpoint)
            {
                m_Driver.Bind(endpoint);
                m_Driver.Listen();
            }

            public void Tick()
            {
                //NetworkConnection connection;
                while ((/*connection =*/ m_Driver.Accept()) != default(NetworkConnection))
                {
                    //m_Connections.Add(connection);
                }
            }

            public void OnConnection()
            {
            }

            public void OnDisconnection()
            {
            }

            public void OnData()
            {
            }

            public void Dispose()
            {
                m_Driver.Dispose();
            }
        }
    }

    public class NetworkDriverFlowTests
    {
        [Test]
        public void NetworkDriver_Simple_Flow([Values(100)] int iterations)
        {
            /*
            Stopwatch stopwatch = new Stopwatch();
            var frequency = Stopwatch.Frequency;
            
            stopwatch.Start();
            var frametime = (double) stopwatch.ElapsedTicks / frequency;
            double nexttick = 0.0f;
            
            UnreliableServer server = new UnreliableServer();
            server.Host(new IPCEndPoint("server"));
            UnreliableClient[] clients = new UnreliableClient[NetworkParams.Constants.MaximumConnectionsSupported];

            for (int i = 0; i < iterations; ++i)
            {
                server.Tick();
                foreach (var client in clients)
                {
                    client.Tick();
                }
            }
            */
        }
    }
}