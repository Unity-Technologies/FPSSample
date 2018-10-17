using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Drawing
{
    public sealed class IndexSet : ICollection<int>
    {
        List<uint> m_Masks = new List<uint>();

        public IndexSet() {}

        public IndexSet(IEnumerable<int> indices)
        {
            foreach (var index in indices)
                Add(index);
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (var i = 0; i < m_Masks.Count; i++)
            {
                var mask = m_Masks[i];
                if (mask == 0)
                    continue;
                for (var j = 0; j < 32; j++)
                {
                    if ((mask & (1 << j)) > 0)
                        yield return i * 32 + j;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void UnionWith(IEnumerable<int> other)
        {
            var otherSet = other as IndexSet;
            if (otherSet != null)
            {
                UnionWith(otherSet);
            }
            else
            {
                foreach (var index in other)
                    Add(index);
            }
        }

        public void UnionWith(IndexSet other)
        {
            for (var i = 0; i < Math.Min(m_Masks.Count, other.m_Masks.Count); i++)
                m_Masks[i] |= other.m_Masks[i];
            for (var i = m_Masks.Count; i < other.m_Masks.Count; i++)
                m_Masks.Add(other.m_Masks[i]);
        }

        public void IntersectWith(IEnumerable<int> other)
        {
            IntersectWith(other as IndexSet ?? new IndexSet(other));
        }

        public void IntersectWith(IndexSet other)
        {
            for (var i = 0; i < Math.Min(m_Masks.Count, other.m_Masks.Count); i++)
                m_Masks[i] &= other.m_Masks[i];
        }

        public void ExceptWith(IEnumerable<int> other)
        {
            var otherSet = other as IndexSet;
            if (otherSet != null)
            {
                ExceptWith(otherSet);
            }
            else
            {
                foreach (var index in other)
                    Remove(index);
            }
        }

        public void ExceptWith(IndexSet other)
        {
            for (var i = 0; i < Math.Min(m_Masks.Count, other.m_Masks.Count); i++)
                m_Masks[i] &= ~other.m_Masks[i];
        }

        public void SymmetricExceptWith(IEnumerable<int> other)
        {
            SymmetricExceptWith(other as IndexSet ?? new IndexSet(other));
        }

        public void SymmetricExceptWith(IndexSet other)
        {
            for (var i = 0; i < Math.Min(m_Masks.Count, other.m_Masks.Count); i++)
                m_Masks[i] ^= other.m_Masks[i];
        }

        public bool IsSubsetOf(IEnumerable<int> other)
        {
            return IsSubsetOf(other as IndexSet ?? new IndexSet(other));
        }

        public bool IsSubsetOf(IndexSet other)
        {
            for (var i = 0; i < Math.Min(m_Masks.Count, other.m_Masks.Count); i++)
            {
                var mask = m_Masks[i];
                var otherMask = other.m_Masks[i];
                if ((mask & otherMask) != mask)
                    return false;
            }

            for (var i = other.m_Masks.Count; i < m_Masks.Count; i++)
            {
                if (m_Masks[i] > 0)
                    return false;
            }

            return true;
        }

        public bool IsSupersetOf(IEnumerable<int> other)
        {
            return IsSupersetOf(other as IndexSet ?? new IndexSet(other));
        }

        public bool IsSupersetOf(IndexSet other)
        {
            for (var i = 0; i < Math.Min(m_Masks.Count, other.m_Masks.Count); i++)
            {
                var otherMask = other.m_Masks[i];
                var mask = m_Masks[i];
                if ((otherMask & mask) != otherMask)
                    return false;
            }

            for (var i = m_Masks.Count; i < other.m_Masks.Count; i++)
            {
                if (other.m_Masks[i] > 0)
                    return false;
            }

            return true;
        }

        public bool IsProperSupersetOf(IEnumerable<int> other)
        {
            return IsProperSupersetOf(other as IndexSet ?? new IndexSet(other));
        }

        public bool IsProperSupersetOf(IndexSet other)
        {
            var isProper = false;

            for (var i = 0; i < Math.Min(m_Masks.Count, other.m_Masks.Count); i++)
            {
                var mask = m_Masks[i];
                var otherMask = other.m_Masks[i];
                if ((otherMask & mask) != otherMask)
                    return false;
                if ((~otherMask & mask) > 0)
                    isProper = true;
            }

            for (var i = m_Masks.Count; i < other.m_Masks.Count; i++)
            {
                if (other.m_Masks[i] > 0)
                    return false;
            }

            if (!isProper)
            {
                for (var i = other.m_Masks.Count; i < m_Masks.Count; i++)
                {
                    if (m_Masks[i] > 0)
                        return true;
                }
            }

            return isProper;
        }

        public bool IsProperSubsetOf(IEnumerable<int> other)
        {
            return IsProperSubsetOf(other as IndexSet ?? new IndexSet(other));
        }

        public bool IsProperSubsetOf(IndexSet other)
        {
            var isProper = false;

            for (var i = 0; i < Math.Min(m_Masks.Count, other.m_Masks.Count); i++)
            {
                var mask = m_Masks[i];
                var otherMask = other.m_Masks[i];
                if ((mask & otherMask) != mask)
                    return false;
                if ((~mask & otherMask) > 0)
                    isProper = true;
            }

            for (var i = other.m_Masks.Count; i < m_Masks.Count; i++)
            {
                if (m_Masks[i] > 0)
                    return false;
            }

            if (!isProper)
            {
                for (var i = m_Masks.Count; i < other.m_Masks.Count; i++)
                {
                    if (other.m_Masks[i] > 0)
                        return true;
                }
            }

            return isProper;
        }

        public bool Overlaps(IEnumerable<int> other)
        {
            var otherSet = other as IndexSet;
            if (otherSet != null)
                return Overlaps(otherSet);

            foreach (var index in other)
            {
                if (Contains(index))
                    return true;
            }

            return false;
        }

        public bool Overlaps(IndexSet other)
        {
            for (var i = 0; i < Math.Min(m_Masks.Count, other.m_Masks.Count); i++)
            {
                if ((m_Masks[i] & other.m_Masks[i]) > 0)
                    return true;
            }
            return false;
        }

        public bool SetEquals(IEnumerable<int> other)
        {
            var otherSet = other as IndexSet;
            if (otherSet != null)
                return SetEquals(otherSet);

            foreach (var index in other)
            {
                if (!Contains(index))
                    return false;
            }
            return true;
        }

        public bool SetEquals(IndexSet other)
        {
            for (var i = 0; i < Math.Min(m_Masks.Count, other.m_Masks.Count); i++)
            {
                if (m_Masks[i] != other.m_Masks[i])
                    return false;
            }

            for (var i = other.m_Masks.Count; i < m_Masks.Count; i++)
            {
                if (m_Masks[i] > 0)
                    return false;
            }

            for (var i = m_Masks.Count; i < other.m_Masks.Count; i++)
            {
                if (other.m_Masks[i] > 0)
                    return false;
            }
            return true;
        }

        public bool Add(int index)
        {
            var maskIndex = index >> 5;
            var bitIndex = index & 31;

            for (var i = m_Masks.Count; i <= maskIndex; i++)
                m_Masks.Add(0);

            var mask = (uint)1 << bitIndex;
            var isNew = (m_Masks[maskIndex] & mask) == 0;
            m_Masks[maskIndex] |= mask;
            return isNew;
        }

        void ICollection<int>.Add(int index)
        {
            Add(index);
        }

        public void Clear()
        {
            for (var i = 0; i < m_Masks.Count; i++)
                m_Masks[i] = 0;
        }

        public bool Contains(int index)
        {
            var maskIndex = index >> 5;
            var bitIndex = index & 31;
            return maskIndex < m_Masks.Count && (m_Masks[maskIndex] & ((uint)1 << bitIndex)) > 0;
        }

        public void CopyTo(int[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(int index)
        {
            var maskIndex = index >> 5;
            var bitIndex = index & 31;
            if (maskIndex >= m_Masks.Count)
                return false;
            var mask = (uint)1 << bitIndex;
            var exists = (m_Masks[maskIndex] & mask) > 0;
            m_Masks[maskIndex] &= ~mask;
            return exists;
        }

        public int Count
        {
            get
            {
                var count = 0;
                foreach (var mask in m_Masks)
                {
                    for (var j = 0; j < 32; j++)
                    {
                        if ((mask & (1 << j)) > 0)
                            count++;
                    }
                }
                return count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }
    }
}
