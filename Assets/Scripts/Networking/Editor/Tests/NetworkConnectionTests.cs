using System;
using System.Collections.Generic;
using NUnit.Framework;


namespace NetcodeTests
{
    [TestFixture]
    public class NetworkConnectionTests
    {
        class PackageInfo
        {
        }

    //    [Test]
    //    public void NetworkConnection_PackageAckWorks()
    //    {
    //        TestTransport.Reset();
    //        var server = new TestTransport(0);
    //        var client = new TestTransport(1);

    //        client.Connect("0");
    //        client.ProcessNewConnections((int id) => {});
    //        server.ProcessNewConnections((int id) => {});

    //        var serverConnection = new NetworkConnection<PackageInfo>() { connectionId = 1, transport = server };
    //        var clientConnection = new NetworkConnection<PackageInfo>() { connectionId = 0, transport = client };

    //        var random = new Random(12315);

    //        var droppedServerSequences = new List<int>();
    //        var ackedServerSequences = new List<int>();
    //        var receivedServerSequences = new List<int>();

    //        var droppedClientSequences = new List<int>();
    //        var ackedClientSequences = new List<int>();
    //        var receivedClientSequences = new List<int>();

    //        const int RUNS = 10000;
    //        for (int i = 0; i < RUNS; ++i)
    //        {
    //            // Write server package
    //            {
    //                BitOutputStream output = new BitOutputStream(buffer);
    //                var result = serverConnection.WritePackageHeader(ref output);
    //                serverConnection.CompleteSendPackage(ref output);
    //                Assert.IsTrue(result != null);

    //                if (random.Next(0, 5) == 0)
    //                {
    //                    droppedServerSequences.Add(serverConnection.outSequence - 1);
    //                    client.DropPackages();
    //                }
    //            }

    //            // Process server package
    //            client.ProcessIncomingData((int connectionId, byte[] data, int size) =>
    //            {
    //                BitInputStream input = new BitInputStream(buffer, size);
    //                var packageSequence = clientConnection.ProcessPackageHeader(ref input, (int sequence, PackageInfo info, bool ack) =>
    //                {
    //                    if (ack)
    //                    {
    //                        Assert.IsTrue(!droppedClientSequences.Contains(sequence));
    //                        ackedClientSequences.Add(sequence);
    //                    }
    //                    else
    //                        Assert.IsTrue(droppedClientSequences.Contains(sequence));
    //                });

    //                if (packageSequence != 0)
    //                    receivedServerSequences.Add(packageSequence);
    //            });

    //            // Write client package
    //            {
    //                // Send package back to ack incoming packages
    //                BitOutputStream output = new BitOutputStream(buffer);
    //                var result = clientConnection.WritePackageHeader(ref output);
    //                clientConnection.CompleteSendPackage(ref output);
    //                Assert.IsTrue(result != null);

    //                if (random.Next(0, 5) == 0)
    //                {
    //                    droppedClientSequences.Add(clientConnection.outSequence - 1);
    //                    server.DropPackages();
    //                }
    //            }
    
    //            // Process client package
    //            server.ProcessIncomingData((int connectionId, byte[] data, int size) => 
    //            {
    //                BitInputStream input = new BitInputStream(buffer, size);
    //                var packageSequence = serverConnection.ProcessPackageHeader(ref input, (int sequence, PackageInfo info, bool ack) =>
    //                {
    //                    if (ack)
    //                    {
    //                        Assert.IsTrue(!ackedServerSequences.Contains(sequence));
    //                        ackedServerSequences.Add(sequence);
    //                    }
    //                    else
    //                        Assert.IsTrue(droppedServerSequences.Contains(sequence));

    //                });

    //                if (packageSequence != 0)
    //                    receivedClientSequences.Add(packageSequence);
    //            });
    //        }

    //        // Make sure we got all sequences acked
    //        for (int i = 1; i < RUNS - 16; ++i)
    //        {
    //            if (!droppedServerSequences.Contains(i))
    //            {
    //                Assert.IsTrue(ackedServerSequences.Contains(i));
    //                Assert.IsTrue(receivedServerSequences.Contains(i));
    //            }

    //            if (!droppedClientSequences.Contains(i))
    //            {
    //                Assert.IsTrue(ackedClientSequences.Contains(i));
    //                Assert.IsTrue(receivedClientSequences.Contains(i));
    //            }
    //        }
    //    }

    //    static byte[] buffer = new byte[1024];
    }
}
