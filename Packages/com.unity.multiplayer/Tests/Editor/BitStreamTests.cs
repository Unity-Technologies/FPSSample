using Experimental.Multiplayer.Utilities;
using NUnit.Framework;
using UnityEngine;
using Unity.Collections;

// using FsCheck;

namespace Experimental.Multiplayer.Tests
{
    public class BitStreamTests
    {        
        [Test]
        public void CreateStreamWithPartOfSourceByteArray()
        {
            byte[] byteArray =
            {
                (byte) 's', (byte) 'o', (byte) 'm', (byte) 'e', 
                (byte) ' ', (byte) 'd', (byte) 'a', (byte) 't', (byte) 'a'
            };

            BitStream bitStream;
            using (bitStream = new BitStream(byteArray, Allocator.Persistent, 4))
            {
                Assert.AreEqual(bitStream.Length, 4);
                var slice = new BitSlice(ref bitStream, 0, bitStream.Length);
                for (int i = 0; i < slice.Length; ++i)
                {
                    Assert.AreEqual(byteArray[i], slice.ReadByte());
                }
                // TODO: Make sure it doesn't read out of bounds
                Assert.AreNotEqual((byte)' ', slice.ReadByte());
            }
        }
        
        [Test]
        public void CreateStreamWithSourceByteArray()
        {
            byte[] byteArray = new byte[100];
            byteArray[0] = (byte)'a';
            byteArray[1] = (byte)'b';
            byteArray[2] = (byte)'c';

            BitStream bitStream;
            using (bitStream = new BitStream(byteArray, Allocator.Persistent))
            {
                var slice = new BitSlice(ref bitStream, 0, byteArray.Length);
                for (int i = 0; i < slice.Length; ++i)
                {
                    Assert.AreEqual(byteArray[i], slice.ReadByte());
                }
            }
        }

        [Test]
        public void ReadingDataFromStreamWithSliceOffset()
        {
            using (var bitStream = new BitStream(100, Allocator.Persistent))
            {
                bitStream.Write((byte)'a');
                bitStream.Write((byte)'b');
                bitStream.Write((byte)'c');
                bitStream.Write((byte)'d');
                bitStream.Write((byte)'e');
                bitStream.Write((byte)'f');
                var slice = bitStream.GetBitSlice(3, 3);
                Assert.AreEqual('d', slice.ReadByte());
                Assert.AreEqual('e', slice.ReadByte());
                Assert.AreEqual('f', slice.ReadByte());
            }
        }

        [Test]
        public void CopyToByteArrayWithOffset()
        {
            byte[] byteArray = new byte[100];

            using (var bitStream = new BitStream(100, Allocator.Persistent))
            {
                bitStream.Write((byte)'a');
                bitStream.Write((byte)'b');
                bitStream.Write((byte)'c');
                bitStream.Write((byte)'d');
                bitStream.Write((byte)'e');
                bitStream.Write((byte)'f');

                bitStream.CopyTo(2, 3, ref byteArray);
                Assert.AreEqual(byteArray[0], (byte)'c');
                Assert.AreEqual(byteArray[1], (byte)'d');
                Assert.AreEqual(byteArray[2], (byte)'e');
                Assert.AreNotEqual(byteArray[3], (byte)'f');
            }
        }

        [Test]
        public void CopyToNativeArrayWithOffset()
        {
            using (var bitStream = new BitStream(100, Allocator.Persistent))
            using (var nativeArray = new NativeArray<byte>(100, Allocator.Persistent))
            {
                bitStream.Write((byte)'a');
                bitStream.Write((byte)'b');
                bitStream.Write((byte)'c');
                bitStream.Write((byte)'d');
                bitStream.Write((byte)'e');
                bitStream.Write((byte)'f');

                bitStream.CopyTo(2, 3, nativeArray);
                Assert.AreEqual(nativeArray[0], (byte)'c');
                Assert.AreEqual(nativeArray[1], (byte)'d');
                Assert.AreEqual(nativeArray[2], (byte)'e');
                Assert.AreNotEqual(nativeArray[3], (byte)'f');
            }
        }
    }
}
