using System;
using Unity.Networking.Transport.Utilities;
using NUnit.Framework;
using UnityEngine;
using Unity.Collections;

// using FsCheck;

namespace Unity.Networking.Transport.Tests
{
    public class DataStreamTests
    {
        [Test]
        public void CreateStreamWithPartOfSourceByteArray()
        {
            byte[] byteArray =
            {
                (byte) 's', (byte) 'o', (byte) 'm', (byte) 'e',
                (byte) ' ', (byte) 'd', (byte) 'a', (byte) 't', (byte) 'a'
            };

            DataStreamWriter dataStream;
            using (dataStream = new DataStreamWriter(4, Allocator.Persistent))
            {
                dataStream.Write(byteArray, 4);
                Assert.AreEqual(dataStream.Length, 4);
                var reader = new DataStreamReader(dataStream, 0, dataStream.Length);
                var readerCtx = default(DataStreamReader.Context);
                for (int i = 0; i < reader.Length; ++i)
                {
                    Assert.AreEqual(byteArray[i], reader.ReadByte(ref readerCtx));
                }

                Assert.Throws<ArgumentOutOfRangeException>(() => { reader.ReadByte(ref readerCtx); });
            }
        }

        [Test]
        public void CreateStreamWithSourceByteArray()
        {
            byte[] byteArray = new byte[100];
            byteArray[0] = (byte) 'a';
            byteArray[1] = (byte) 'b';
            byteArray[2] = (byte) 'c';

            DataStreamWriter dataStream;
            using (dataStream = new DataStreamWriter(byteArray.Length, Allocator.Persistent))
            {
                dataStream.Write(byteArray, byteArray.Length);
                var reader = new DataStreamReader(dataStream, 0, byteArray.Length);
                var readerCtx = default(DataStreamReader.Context);
                for (int i = 0; i < reader.Length; ++i)
                {
                    Assert.AreEqual(byteArray[i], reader.ReadByte(ref readerCtx));
                }
            }
        }

        [Test]
        public void ReadingDataFromStreamWithSliceOffset()
        {
            using (var dataStream = new DataStreamWriter(100, Allocator.Persistent))
            {
                dataStream.Write((byte) 'a');
                dataStream.Write((byte) 'b');
                dataStream.Write((byte) 'c');
                dataStream.Write((byte) 'd');
                dataStream.Write((byte) 'e');
                dataStream.Write((byte) 'f');
                var reader = new DataStreamReader(dataStream, 3, 3);
                var readerCtx = default(DataStreamReader.Context);
                Assert.AreEqual('d', reader.ReadByte(ref readerCtx));
                Assert.AreEqual('e', reader.ReadByte(ref readerCtx));
                Assert.AreEqual('f', reader.ReadByte(ref readerCtx));
            }
        }

        [Test]
        public void CopyToByteArrayWithOffset()
        {
            byte[] byteArray = new byte[100];

            using (var dataStream = new DataStreamWriter(100, Allocator.Persistent))
            {
                dataStream.Write((byte) 'a');
                dataStream.Write((byte) 'b');
                dataStream.Write((byte) 'c');
                dataStream.Write((byte) 'd');
                dataStream.Write((byte) 'e');
                dataStream.Write((byte) 'f');

                dataStream.CopyTo(2, 3, ref byteArray);
                Assert.AreEqual(byteArray[0], (byte) 'c');
                Assert.AreEqual(byteArray[1], (byte) 'd');
                Assert.AreEqual(byteArray[2], (byte) 'e');
                Assert.AreNotEqual(byteArray[3], (byte) 'f');
            }
        }

        [Test]
        public void CopyToNativeArrayWithOffset()
        {
            using (var dataStream = new DataStreamWriter(100, Allocator.Persistent))
            using (var nativeArray = new NativeArray<byte>(100, Allocator.Persistent))
            {
                dataStream.Write((byte) 'a');
                dataStream.Write((byte) 'b');
                dataStream.Write((byte) 'c');
                dataStream.Write((byte) 'd');
                dataStream.Write((byte) 'e');
                dataStream.Write((byte) 'f');

                dataStream.CopyTo(2, 3, nativeArray);
                Assert.AreEqual(nativeArray[0], (byte) 'c');
                Assert.AreEqual(nativeArray[1], (byte) 'd');
                Assert.AreEqual(nativeArray[2], (byte) 'e');
                Assert.AreNotEqual(nativeArray[3], (byte) 'f');
            }
        }
    }
}