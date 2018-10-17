using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ListExtensionMethods
{
    public static void EraseSwap<T>(this List<T> list, int index)
    {
        int lastIndex = list.Count - 1;
        list[index] = list[lastIndex];
        list.RemoveAt(lastIndex);
    }

    public static void Shuffle<T>(this List<T> list)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var t = list[i];
            var r = Random.Range(i, list.Count);
            list[i] = list[r];
            list[r] = t;
        }
    }

}

public static class ArrayExtensionMethods
{
    public static int IndexOf<T>(this T[] array, T needle)
    {
        for (var i = 0; i < array.Length; i++)
        {
            if (array[i].Equals(needle))
                return i;
        }
        return -1;
    }

    public static void Clear<T>(this T[] array)
    {
        System.Array.Clear(array, 0, array.Length);
    }
}