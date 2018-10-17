using System;
using System.Collections.Generic;

namespace ProfileAnalyser
{
    public class ThreadData
    {
        public string threadNameWithIndex;
        public int threadGroupIndex;
        public string threadGroupName;
        public int threadsInGroup;
        public List<ThreadFrameTime> frames = new List<ThreadFrameTime>();

        public float msMedian;
        public float msLowerQuartile;
        public float msUpperQuartile;
        public float msMin;
        public float msMax;

        public int medianFrameIndex;
        public int minFrameIndex;
        public int maxFrameIndex;

        public ThreadData(string _threadName)
        {
            threadNameWithIndex = _threadName;

            var info = threadNameWithIndex.Split(':');
            threadGroupIndex = int.Parse(info[0]);
            threadGroupName = info[1];
            threadsInGroup = 1;

            msMedian = 0.0f;
            msLowerQuartile = 0.0f;
            msUpperQuartile = 0.0f;
            msMin = 0.0f;
            msMax = 0.0f;

            medianFrameIndex = -1;
            minFrameIndex = -1;
            maxFrameIndex = -1;
        }
    }
}
