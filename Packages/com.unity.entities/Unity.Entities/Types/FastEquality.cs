using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;

[assembly: InternalsVisibleTo("Unity.Entities.Tests")]

namespace Unity.Entities
{
    public static class FastEquality
    {
        internal static TypeInfo CreateTypeInfo(Type type)
        {
            var begin = 0;
            var end = 0;
            var hash = 0;

            var layouts = new List<Layout>();

            CreateLayoutRecurse(type, 0, layouts, ref begin, ref end, ref hash);

            if (begin != end)
                layouts.Add(new Layout {offset = begin, count = end - begin, Aligned4 = false});

            var layoutsArray = layouts.ToArray();

            for (var i = 0; i != layoutsArray.Length; i++)
                if (layoutsArray[i].count % 4 == 0 && layoutsArray[i].offset % 4 == 0)
                {
                    layoutsArray[i].count /= 4;
                    layoutsArray[i].Aligned4 = true;
                }

            return new TypeInfo { Layouts = layoutsArray, Hash = hash };
        }

        public struct Layout
        {
            public int offset;
            public int count;
            public bool Aligned4;

            public override string ToString()
            {
                return $"offset: {offset} count: {count} Aligned4: {Aligned4}";
            }
        }

        public struct TypeInfo
        {
            public Layout[] Layouts;
            public int Hash;

            public static TypeInfo Null => new TypeInfo();
        }

        private unsafe struct PointerSize
        {
#pragma warning disable 0169 // "never used" warning
            private void* pter;
#pragma warning restore 0169
        }

        struct FieldData
        {
            public int Offset;
            public FieldInfo Field;
        }

        static FixedBufferAttribute GetFixedBufferAttribute(FieldInfo field)
        {
            foreach (var attribute in field.GetCustomAttributes(typeof(FixedBufferAttribute)))
            {
                return (FixedBufferAttribute)attribute;
            }

            return null;
        }

        static void CombineHash(ref int hash, params int[] values)
        {
            foreach (var value in values)
            {
                hash *= FNV_32_PRIME;
                hash ^= value;
            }
        }

        private static void CreateLayoutRecurse(Type type, int baseOffset, List<Layout> layouts, ref int begin,
            ref int end, ref int typeHash)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fieldsWithOffset = new FieldData[fields.Length];
            for (int i = 0; i != fields.Length; i++)
            {
                fieldsWithOffset[i].Offset = UnsafeUtility.GetFieldOffset(fields[i]);
                fieldsWithOffset[i].Field = fields[i];
            }

            Array.Sort(fieldsWithOffset, (a, b) => a.Offset - b.Offset);

            foreach (var fieldWithOffset in fieldsWithOffset)
            {
                var field = fieldWithOffset.Field;
                var fixedBuffer = GetFixedBufferAttribute(field);
                var offset = baseOffset + fieldWithOffset.Offset;

                if (fixedBuffer != null)
                {
                    var stride = UnsafeUtility.SizeOf(fixedBuffer.ElementType);
                    for (int i = 0; i < fixedBuffer.Length; ++i)
                    {
                        CreateLayoutRecurse(fixedBuffer.ElementType, offset + i * stride, layouts, ref begin, ref end, ref typeHash);
                    }
                }
                else if (field.FieldType.IsPrimitive || field.FieldType.IsPointer || field.FieldType.IsClass || field.FieldType.IsEnum)
                {
                    CombineHash(ref typeHash, offset, (int)Type.GetTypeCode(field.FieldType));

                    var sizeOf = -1;
                    if (field.FieldType.IsPointer || field.FieldType.IsClass)
                        sizeOf = UnsafeUtility.SizeOf<PointerSize>();
                    else if (field.FieldType.IsEnum)
                    {
                        //@TODO: Workaround IL2CPP bug
                        // sizeOf = UnsafeUtility.SizeOf(field.FieldType);
                        sizeOf = UnsafeUtility.SizeOf(field.FieldType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)[0].FieldType);
                    }
                    else
                        sizeOf = UnsafeUtility.SizeOf(field.FieldType);

                    if (end != offset)
                    {
                        layouts.Add(new Layout {offset = begin, count = end - begin, Aligned4 = false});
                        begin = offset;
                        end = offset + sizeOf;
                    }
                    else
                    {
                        end += sizeOf;
                    }
                }
                else
                {
                    CreateLayoutRecurse(field.FieldType, offset, layouts, ref begin, ref end, ref typeHash);
                }
            }
        }

        private const int FNV_32_PRIME = 0x01000193;

        //@TODO: Encode type in hashcode...

        public static unsafe int GetHashCode<T>(T lhs, TypeInfo typeInfo) where T : struct
        {
            return GetHashCode(UnsafeUtility.AddressOf(ref lhs), typeInfo);
        }

        public static unsafe int GetHashCode<T>(ref T lhs, TypeInfo typeInfo) where T : struct
        {
            return GetHashCode(UnsafeUtility.AddressOf(ref lhs), typeInfo);
        }

        public static unsafe int GetHashCode(void* dataPtr, TypeInfo typeInfo)
        {
            var layout = typeInfo.Layouts;
            var data = (byte*) dataPtr;
            uint hash = 0;

            for (var k = 0; k != layout.Length; k++)
                if (layout[k].Aligned4)
                {
                    var dataInt = (uint*) (data + layout[k].offset);
                    var count = layout[k].count;
                    for (var i = 0; i != count; i++)
                    {
                        hash *= FNV_32_PRIME;
                        hash ^= dataInt[i];
                    }
                }
                else
                {
                    var dataByte = data + layout[k].offset;
                    var count = layout[k].count;
                    for (var i = 0; i != count; i++)
                    {
                        hash *= FNV_32_PRIME;
                        hash ^= dataByte[i];
                    }
                }

            return (int) hash;
        }

        public static unsafe bool Equals<T>(T lhs, T rhs, TypeInfo typeInfo) where T : struct
        {
            return Equals(UnsafeUtility.AddressOf(ref lhs), UnsafeUtility.AddressOf(ref rhs), typeInfo);
        }

        public static unsafe bool Equals<T>(ref T lhs, ref T rhs, TypeInfo typeInfo) where T : struct
        {
            return Equals(UnsafeUtility.AddressOf(ref lhs), UnsafeUtility.AddressOf(ref rhs), typeInfo);
        }

        public static unsafe bool Equals(void* lhsPtr, void* rhsPtr, TypeInfo typeInfo)
        {
            var layout = typeInfo.Layouts;
            var lhs = (byte*) lhsPtr;
            var rhs = (byte*) rhsPtr;

            var same = true;

            for (var k = 0; k != layout.Length; k++)
                if (layout[k].Aligned4)
                {
                    var offset = layout[k].offset;
                    var lhsInt = (uint*) (lhs + offset);
                    var rhsInt = (uint*) (rhs + offset);
                    var count = layout[k].count;
                    for (var i = 0; i != count; i++)
                        same &= lhsInt[i] == rhsInt[i];
                }
                else
                {
                    var offset = layout[k].offset;
                    var lhsByte = lhs + offset;
                    var rhsByte = rhs + offset;
                    var count = layout[k].count;
                    for (var i = 0; i != count; i++)
                        same &= lhsByte[i] == rhsByte[i];
                }

            return same;
        }
    }
}
