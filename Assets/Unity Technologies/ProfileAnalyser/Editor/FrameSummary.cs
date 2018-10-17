using System.Collections.Generic;

namespace ProfileAnalyser
{
    public class FrameSummary
    {
        public double msTotal;
        public int first;
        public int last;
        public int count;   // Valid count may not be last-first

        public float msAverage;
        public float msMedian;
        public float msLowerQuartile;
        public float msUpperQuartile;
        public float msMin;
        public float msMax;

        public int medianFrameIndex;
        public int minFrameIndex;
        public int maxFrameIndex;

        public int maxMarkerDepth;
        public int totalMarkers;

        public int[] buckets = new int[20];   // Each bucket contains 'number of frames' for frametime in that range
        public List<FrameTime> frames = new List<FrameTime>();
    }
}
