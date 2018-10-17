using UnityEngine;
using UnityEngine.Assertions;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System;

namespace Experimental.Multiplayer
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct UIntFloat
    {
        [FieldOffset(0)]
        public float floatValue;

        [FieldOffset(0)]
        public uint intValue;

        [FieldOffset(0)]
        public double doubleValue;

        [FieldOffset(0)]
        public ulong longValue;
    }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    [Serializable]
    public class NakedPtrSafety
    {
        [SerializeField]
        int m_state = 0;

        public void MarkSafe() { m_state++; }
        public void Reset() { m_state = 0; }
        public bool isSafe() { return m_state > 0; }
    }
#endif


    [Serializable]
    public struct SerializedPointer
    {
        [SerializeField]
        internal Allocator alloc;
        [SerializeField]
        internal long ptrAddr;
        [SerializeField]
        internal int dataLength;

        public long PtrAddress { get { return ptrAddr; } }
        public Allocator Allocator { get { return alloc; } }
        public int DataLength { get { return dataLength; } }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [NativeSetClassTypeToNullOnSchedule, SerializeField]
        internal NakedPtrSafety safety;
        public bool isValid() { return safety != null && safety.isSafe(); }
#endif
        public static SerializedPointer Invalid
        {
            get { return default(SerializedPointer); }
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    unsafe public struct BitStream : IDisposable
    {
        internal Allocator m_Allocator;
        [NativeDisableUnsafePtrRestriction]
        internal byte* m_Buffer;

        internal int m_BitCount;
        internal int m_Length;

        internal ulong m_WriteScratchBuffer;
        internal int m_WriteByteIndex;
        internal int m_WriteBitIndex;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal int m_MinIndex;
        internal int m_MaxIndex;
        internal AtomicSafetyHandle m_Safety;
        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel m_DisposeSentinel;
        [NativeSetClassTypeToNullOnSchedule]
        internal NakedPtrSafety m_NakedPtrSafety;
#endif
        public int WriteCapacity { get { return m_Length - GetBytesWritten(); } }
        public unsafe byte* UnsafeDataPtr { get { return m_Buffer; } }
        public void IncreaseWritePtr(int length) { m_WriteByteIndex += length; }

        public BitStream(int length, Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS

            if (length > int.MaxValue)
                throw new ArgumentOutOfRangeException("length", string.Format("Length * sizeof(T) cannot exceed {0} bytes", int.MaxValue));
#endif
            m_Length = length;
            m_BitCount = length * 8;
            m_Allocator = allocator;
            m_WriteScratchBuffer = 0;
            m_WriteBitIndex = 0;
            m_WriteByteIndex = 0;

            m_Buffer = (byte*)UnsafeUtility.Malloc(length, UnsafeUtility.AlignOf<byte>(), m_Allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_MinIndex = 0;
            m_MaxIndex = length - 1;
#if UNITY_2018_3_OR_NEWER
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1, m_Allocator);
#else
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1);
#endif
            m_NakedPtrSafety = new NakedPtrSafety();
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
#endif
        }

        public BitStream(byte[] data, Allocator allocator, int size = -1)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (data == null)
                throw new ArgumentException("Data can not be null.", "data");
#endif
            int dataLength = data.Length;
            if (size != -1)
            {
                dataLength = size;
            }

            void* buffer = UnsafeUtility.Malloc(dataLength, UnsafeUtility.AlignOf<byte>(), allocator);
            
            fixed (byte* fixedBuffer = data)
            {
                UnsafeUtility.MemCpy(buffer, fixedBuffer, dataLength);
            }

            m_Buffer = (byte*)buffer;
            m_Length = dataLength;
            m_BitCount = m_Length * 8;
            m_Allocator = allocator;
            m_WriteScratchBuffer = 0;
            m_WriteBitIndex = 0;
            m_WriteByteIndex = dataLength;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_MinIndex = 0;
            m_MaxIndex = m_Length - 1;
#if UNITY_2018_3_OR_NEWER
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1, m_Allocator);
#else
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1);
#endif
            m_NakedPtrSafety = new NakedPtrSafety();
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
#endif
        }

        public bool IsCreated
        {
            get { return m_Buffer != null; }
        }

        public int Length { get { return m_Length; } }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if UNITY_2018_3_OR_NEWER
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#else
            DisposeSentinel.Dispose(m_Safety, ref m_DisposeSentinel);
#endif
#endif
            UnsafeUtility.Free(m_Buffer, m_Allocator);
            m_Buffer = (byte*)0;
            AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;

        }

        private void OnDomainUnload(object sender, EventArgs args)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_DisposeSentinel != null && m_NakedPtrSafety != null && m_NakedPtrSafety.isSafe())
#if UNITY_2018_3_OR_NEWER
                DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#else
                DisposeSentinel.Dispose(m_Safety, ref m_DisposeSentinel);
#endif
#endif
        }

        public void CopyTo(int start, int length, NativeArray<byte> dest)
        {
            ValidateSizeParameters(start, length, dest.Length);
            
            void* dstPtr = dest.GetUnsafePtr();
            UnsafeUtility.MemCpy(dstPtr, m_Buffer + start, length);
        }
        
        public void CopyTo(int start, int length, ref byte[] dest)
        {
            ValidateSizeParameters(start, length, dest.Length);

            fixed (byte* ptr = dest) 
            {
                UnsafeUtility.MemCpy(ptr, m_Buffer + start, length);
            }
        }

        void ValidateSizeParameters(int start, int length, int dstLength)
        {
            if (start != -1 && length + start > m_Length)
                throw new ArgumentOutOfRangeException("start+length", "The sum of start and length can not be larger than the BitStream's Length");

            if (length > dstLength)
                throw new ArgumentOutOfRangeException("length", "Length must be <= than the length of the destination");

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            if (dstLength != m_Length)
                throw new ArgumentOutOfRangeException("dest", "The length of the destination array should be equal to the BitStream length.");
#endif
        }

        public void CopyTo(NativeArray<byte> dest)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            if (dest.Length != m_Length)
                throw new ArgumentOutOfRangeException("dest", "The length of the destination NativeArray<byte> should be equal to the BitStream length.");
#endif
            void* dstPtr = dest.GetUnsafePtr();
            UnsafeUtility.MemCpy(dstPtr, (void*)m_Buffer, m_BitCount);
        }

        public BitStream GetBitStreamCopy(int start, int length, Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (length + start > m_Length)
                throw new ArgumentOutOfRangeException("start+length", "The sum of start and length can not be larger than the BitStream's Length");

            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", "allocator");
#endif
            BitStream stream = new BitStream(length, allocator);
            void* dst = stream.GetUnsafePtr();
            void* src = (this.GetUnsafePtr() + start);
            UnsafeUtility.MemCpy(dst, src, length);
            
            return stream;
        }
        
        // Writer
        public unsafe void WriteBits(uint value, int bits)
        {
            Assert.IsTrue(bits > 0);
            Assert.IsTrue(bits <= 32);
            Debug.Assert(GetBitsWritten() + bits <= m_BitCount);
            //Assert.AreEqual(value, value & ((1ULL << bits) - 1), "WriteBits value is larger than the bits");
            if (GetBitsWritten() + bits > m_BitCount)
                return;

            m_WriteScratchBuffer |= (ulong)(value) << m_WriteBitIndex;
            m_WriteBitIndex += bits;

            while (m_WriteBitIndex >= 8)
            {
                m_Buffer[m_WriteByteIndex++] = (byte)(m_WriteScratchBuffer & 0xff);
                m_WriteScratchBuffer >>= 8;
                m_WriteBitIndex -= 8;
            }
        }

        public void WriteAlign()
        {
            if (m_WriteBitIndex > 0)
                WriteBits(0, 8 - m_WriteBitIndex);
        }

        public unsafe void WriteBytes(byte* data, int bytes)
        {
            if (GetBytesWritten() + bytes <= m_BitCount * 8)
            {
                WriteAlign();
                UnsafeUtility.MemCpy(m_Buffer + m_WriteByteIndex, data, bytes);
                m_WriteByteIndex += bytes;
            }
            else
                throw new System.ArgumentOutOfRangeException();
        }

        public int GetWriteAlignBits()
        {
            return (8 - GetBitsWritten() % 8) % 8;
        }

        public int GetBitsWritten()
        {
            return m_WriteByteIndex * 8 - m_WriteBitIndex;
        }

        public int GetWriteBitsAvailable()
        {
            return m_BitCount - GetBitsWritten();
        }

        public int GetBytesWritten()
        {
            //return (GetBitsWritten() + 7) / 8;
            return AlignToPowerOfTwo(GetBitsWritten(), 8) / 8;
        }

        public void Write(byte value)
        {
            WriteBits((uint)value, sizeof(byte) * 8);
        }

        public void Write(byte[] value)
        {
            unsafe
            {
                fixed (byte* p = value)
                {
                    WriteBytes(p, value.Length);
                }
            }
        }

        public void Write(short value)
        {
            WriteBits((uint)value, sizeof(short) * 8);
        }
        public void Write(ushort value)
        {
            WriteBits(value, sizeof(ushort) * 8);
        }

        public void Write(int value)
        {
            WriteBits((uint)value, sizeof(int) * 8);
        }

        public void Write(uint value)
        {
            WriteBits(value, sizeof(uint) * 8);
        }

        public void Write(float value)
        {
            UIntFloat uf = new UIntFloat();
            uf.floatValue = value;
            Write((int)uf.intValue);
        }

        static int AlignToPowerOfTwo(int value, int alignment)
        {
            return (value + (alignment - 1)) & ~(alignment - 1);
        }

        public int Reset()
        {
            m_WriteBitIndex = m_WriteByteIndex = 0;
            m_WriteScratchBuffer = 0;

            return 0;
        }

        public BitSlice GetBitSlice(int offset, int length)
        {
            return new BitSlice(ref this, offset, length);
        }
    }

    public static class BitStreamUnsafeUtility
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static AtomicSafetyHandle GetAtomicSafetyHandle(BitStream bs)
        {
            return bs.m_Safety;
        }

        public static void SetAtomicSafetyHandle(ref BitStream bs, AtomicSafetyHandle safety)
        {
            bs.m_Safety = safety;
        }

#endif
        public unsafe static BitStream ConvertExistingDataFromNativeSliceToBitStream(NativeSlice<byte> slice)
        {
            BitStream stream = new BitStream()
            {
                m_Allocator = Allocator.None,
                m_Buffer = (byte*)slice.GetUnsafePtr(),
                m_Length = slice.Length,
                m_BitCount = slice.Length * 8,
                m_WriteBitIndex = 0,
                m_WriteByteIndex = slice.Length,
                m_WriteScratchBuffer = 0
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                ,m_MinIndex = 0,
                m_MaxIndex = slice.Length - 1
#endif
            };

            return stream;
        }

        public unsafe static byte* GetUnsafePtr(this BitStream bs)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(bs.m_Safety);
#endif
            return bs.m_Buffer;
        }

        public unsafe static byte* GetUnsafeReadOnlyPtr(this BitStream bs)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(bs.m_Safety);
#endif
            return bs.m_Buffer;
        }

        unsafe public static byte* GetUnsafeBufferPointerWithoutChecks(this BitStream bs)
        {
            return bs.m_Buffer;
        }
    }

    unsafe public struct BitSlice
    {
        private byte* m_bufferPtr;
        int m_BitCount;
        ulong m_ReadScratchBuffer;
        int m_ReadBitIndex;
        int m_ReadByteIndex;
        int m_Length;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif
        public BitSlice(ref BitStream bs, int offset, int length)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = bs.m_Safety;

#endif
            m_bufferPtr = bs.GetUnsafePtr() + offset;
            m_BitCount = bs.m_BitCount;
            m_ReadScratchBuffer = 0;
            m_ReadBitIndex = 0;
            m_ReadByteIndex = 0;
            m_Length = length;
            //compute starting position
        }

        public int Length
        {
            get { return m_Length; }
        }

        public bool isValid
        {
            get { return m_bufferPtr != null; }
        }
        static int AlignToPowerOfTwo(int value, int alignment)
        {
            return (value + (alignment - 1)) & ~(alignment - 1);
        }

        public unsafe uint ReadBits(int bits)
        {
            while (m_ReadBitIndex < 32)
            {
                m_ReadScratchBuffer |= (ulong)m_bufferPtr[m_ReadByteIndex++] << m_ReadBitIndex;
                m_ReadBitIndex += 8;
            }
            return ReadBitsInternal(bits);
        }

        uint ReadBitsInternal(int bits)
        {
            var data = m_ReadScratchBuffer & (((ulong)1 << bits) - 1);
            m_ReadScratchBuffer >>= bits;
            m_ReadBitIndex -= bits;
            return (uint)data;
        }

        public void ReadAlign()
        {
            int remainderBits = m_ReadBitIndex % 8;
            if (remainderBits != 0)
            {
                var value = ReadBitsInternal(remainderBits);
            }
            m_ReadByteIndex -= m_ReadBitIndex / 8;
            m_ReadBitIndex = 0;
            m_ReadScratchBuffer = 0;
        }

        public unsafe void ReadBytes(byte* data, int length)
        {
            if (GetBytesRead() + length > (m_BitCount / 8))
            {
                throw new System.ArgumentOutOfRangeException();
            }
            ReadAlign();
            UnsafeUtility.MemCpy(data, m_bufferPtr + m_ReadByteIndex, length);
            m_ReadByteIndex += length;
        }
        
        public byte[] ReadBytesIntoArray(ref byte[] dest, int length)
        {
            for (var i = 0; i < length; ++i)
                dest[i] = ReadByte();
            return dest;
        }

        public byte[] ReadBytesAsArray(int length)
        {
            var array = new byte[length];
            for (var i = 0; i < array.Length; ++i)
                array[i] = ReadByte();
            return array;
        }

        public int GetReadAlignBits()
        {
            return (8 - GetBitsRead() % 8) % 8;
        }

        public int GetBitsRead()
        {
            return m_ReadByteIndex * 8 - m_ReadBitIndex;
        }

        public int GetBytesRead()
        {
            return AlignToPowerOfTwo(GetBitsRead(), 8) / 8;
        }

        public byte ReadByte()
        {
            return (byte)ReadBits(sizeof(byte) * 8);
        }

        public short ReadShort()
        {
            return (short)ReadBits(sizeof(short) * 8);
        }

        public int ReadInt()
        {
            return (int)ReadBits(sizeof(int) * 8);
        }

        public uint ReadUInt()
        {
            return ReadBits(sizeof(int) * 8);
        }

        public float ReadFloat()
        {
            UIntFloat uf = new UIntFloat();
            uf.intValue = (uint)ReadInt();
            return uf.floatValue;
        }

        public void* GetUnsafePtr()
        {
            return m_bufferPtr;
        }
    }
}
