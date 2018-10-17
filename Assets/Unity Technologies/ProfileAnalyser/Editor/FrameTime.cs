
using System;

namespace ProfileAnalyser
{
    public class FrameTime : IComparable<FrameTime>
    {
        public int frameIndex;
        public float ms;
        public int count;

        public FrameTime(int index, float msTime, int _count)
        {
            frameIndex = index;
            ms = msTime;
            count = _count;
        }

        public int CompareTo(FrameTime other)
        {
            return ms.CompareTo(other.ms);
        }
    }
}
