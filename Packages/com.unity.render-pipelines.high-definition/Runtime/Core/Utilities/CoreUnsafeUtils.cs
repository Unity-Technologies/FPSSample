using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Experimental.Rendering
{
    public static unsafe class CoreUnsafeUtils
    {
        public static void CopyTo<T>(this List<T> list, void* dest, int count)
            where T : struct
        {
            var c = Mathf.Min(count, list.Count);
            for (int i = 0; i < c; ++i)
                UnsafeUtility.WriteArrayElement<T>(dest, i, list[i]);
        }

        public static void CopyTo<T>(this T[] list, void* dest, int count)
            where T : struct
        {
            var c = Mathf.Min(count, list.Length);
            for (int i = 0; i < c; ++i)
                UnsafeUtility.WriteArrayElement<T>(dest, i, list[i]);
        }

        public static void QuickSort<T>(int count, void* data)
            where T : struct, IComparable<T>
        {
            QuickSort<T>(data, 0, count - 1);
        }

        public static unsafe void QuickSort(uint[] arr, int left, int right)
        {
            fixed (uint* ptr = arr)
                CoreUnsafeUtils.QuickSort<uint>(ptr, left, right);
        }

        public static void QuickSort<T>(void* data, int left, int right)
            where T : struct, IComparable<T>
        {
            // For Recursion
            if (left < right)
            {
                int pivot = Partition<T>(data, left, right);

                if (pivot > 1)
                    QuickSort<T>(data, left, pivot);

                if (pivot + 1 < right)
                    QuickSort<T>(data, pivot + 1, right);
            }
        }

        // Just a sort function that doesn't allocate memory
        // Note: Shoud be repalc by a radix sort for positive integer
        static int Partition<T>(void* data, int left, int right)
            where T : struct, IComparable<T>
        {
            var pivot = UnsafeUtility.ReadArrayElement<T>(data, left);

            --left;
            ++right;
            while (true)
            {
                var lvalue = default(T);
                do { ++left; }
                while ((lvalue = UnsafeUtility.ReadArrayElement<T>(data, left)).CompareTo(pivot) < 0);

                var rvalue = default(T);
                do { --right; }
                while ((rvalue = UnsafeUtility.ReadArrayElement<T>(data, right)).CompareTo(pivot) > 0);

                if (left < right)
                {
                    UnsafeUtility.WriteArrayElement<T>(data, right, lvalue);
                    UnsafeUtility.WriteArrayElement<T>(data, left, rvalue);
                }
                else
                {
                    return right;
                }
            }
        }
    }
}
