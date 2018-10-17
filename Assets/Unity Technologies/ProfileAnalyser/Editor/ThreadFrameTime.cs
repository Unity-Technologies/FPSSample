using System;

namespace ProfileAnalyser
{
    public class ThreadFrameTime : IComparable<ThreadFrameTime>
    {
        public int frameIndex;
        public float ms;
        public float msIdle;

        public ThreadFrameTime(int index, float msTime, float msTimeIdle)
        {
            frameIndex = index;
            ms = msTime;
            msIdle = msTimeIdle;
        }

        public int CompareTo(ThreadFrameTime other)
        {
            return ms.CompareTo(other.ms);
        }
    }
}
