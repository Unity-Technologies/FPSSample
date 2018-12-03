using Unity.Networking.Transport.PacketProcessors;
using NUnit.Framework;
using UnityEngine;
using Unity.Collections;

namespace Unity.Networking.Transport.Tests
{
    public class FragmenterTests
    {
        [Test]
        public void CreateAndDestroyFragmenter_NoLeaks()
        {
            Fragmenter f = new Fragmenter();
            f.Dispose();
        }

        Fragmenter fragmenter;

        [SetUp]
        public void Setup()
        {
            fragmenter = new Fragmenter();
        }

        [TearDown]
        public void TearDown()
        {
            fragmenter.Dispose();
        }

        [Test]
        public unsafe void DummyIntegrationTest()
        {
            int start, cSeed = 12345;
            start = cSeed;

            int maxSize = 65536;

            var arrOut = new NativeArray<byte>(maxSize, Allocator.Temp);
            NativeSlice<byte> arrIn;

            for (int a = 0; a < 1; a++)
            {
                byte[] packet = new byte[maxSize];

                System.Random r = new System.Random(cSeed);
                r.NextBytes(packet);

                // Overwrite first 4 bytes with seed so we can validate the packet even if it arrives Out Of Order
                packet[0] = (byte) (cSeed >> 0);
                packet[1] = (byte) (cSeed >> 8);
                packet[2] = (byte) (cSeed >> 16);
                packet[3] = (byte) (cSeed >> 24);

                arrOut.CopyFrom(packet);

                fragmenter.FragmentPacket(arrOut.Slice(0, 22 /*maxSize / 2*/), cSeed);

                cSeed++;
            }

            NativeArray<byte> buffer = new NativeArray<byte>(1400, Allocator.Temp);

            while (fragmenter.fragmentedOutgoing.Count > 0)
            {
                var f = fragmenter.fragmentedOutgoing.Dequeue();

                /*
                var bw = new ByteWriter(buffer.GetUnsafePtr(), buffer.Length);
                bw.Write(f.ID);
                bw.Write(f.SequenceNum);
                bw.Write(f.SequenceCnt);
                bw.Write(f.packetData.Length);
                bw.WriteBytes((byte*)f.packetData.GetUnsafePtr(), f.packetData.Length);

                var written = bw.GetBytesWritten();
                */


                var ret = fragmenter.DefragmentPacket(f, out arrIn);
                //var ret = fragmenter.DefragmentPacket(buffer.Slice(), arrIn);

                if (!ret)
                    continue;


                using (var strm = new DataStreamWriter(64, Allocator.Persistent))
                {
                    int seed = 0;
                    strm.Write(seed);
                    DataStreamReader reader = new DataStreamReader(strm, 0, 4);
                    var readerCtx = default(DataStreamReader.Context);
                    seed = reader.ReadInt(ref readerCtx);
                    Debug.Log("seed " + seed + " start " + start);
                    for (int i = 0; i < arrIn.Length; ++i)
                    {
                        if (arrIn[i] != arrOut[i])
                        {
                            Debug.Log("arr in = " + arrIn[i] + " arr out = " + arrOut[i]);
                        }

                        Assert.AreEqual((byte) arrIn[i], (byte) arrOut[i]);
                    }
                }
            }

            buffer.Dispose();
            arrOut.Dispose();
            //arrIn.Dispose();
        }
    }
}