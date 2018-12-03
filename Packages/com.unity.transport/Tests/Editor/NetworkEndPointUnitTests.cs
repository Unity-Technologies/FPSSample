using NUnit.Framework;
using System.Net;
using System.Net.Sockets;

namespace Unity.Networking.Transport.Tests
{
    public class NetworkEndPointUnitTests
    {
        [Test]
        public void NetworkEndPoint_Mashalling_WorksAsExpected()
        {
            ushort port = 12345;
            NetworkEndPoint nep = new NetworkEndPoint();
            EndPoint ep = new IPEndPoint(IPAddress.Loopback, port);

            nep = ep;

            Assert.That(nep.Family == NetworkFamily.UdpIpv4);
            Assert.That(nep.Port == port);

            var endpoint = NetworkEndPoint.ToEndPoint(nep);
            Assert.That(endpoint is IPEndPoint);
            var iep = (IPEndPoint) endpoint;
            Assert.That(iep.Port == port);
            Assert.That(iep.AddressFamily == AddressFamily.InterNetwork);
        }
    }
}