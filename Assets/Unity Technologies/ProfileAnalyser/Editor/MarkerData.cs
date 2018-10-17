using System.Collections.Generic;

namespace ProfileAnalyser
{
    public class MarkerData
    {
        public string name;
        public double msTotal;
        public int count;              // total number of marker calls in the timeline (multiple per frame)
        public int lastFrame;
        public int presentOnFrameCount; // number of frames containing this marker
        public int firstFrameIndex;
        public float msFrameAverage;    // average over all frames
        public float msMedian;          // median over all frames
        public float msLowerQuartile;   // over all frames
        public float msUpperQuartile;   // over all frames
        public float msMin;             // min total time per frame
        public float msMax;             // max total time per frame
        public int minIndividualFrameIndex;
        public int maxIndividualFrameIndex;
        public float msMinIndividual;   // min individual function call 
        public float msMaxIndividual;   // max individual function call
        public float msAtMedian;        // time at median frame
        public int medianFrameIndex;    // frame this markers median value is found on
        public int minFrameIndex;
        public int maxFrameIndex;
        public int minDepth;
        public int maxDepth;

        public int[] buckets = new int[20];   // Each bucket contains 'number of frames' for 'sum of markers in the frame' in that range
        public List<FrameTime> frames = new List<FrameTime>();

        public MarkerData(string markerName)
        {
            name = markerName;
            msTotal = 0;
            count = 0;
            lastFrame = -1;
            presentOnFrameCount = 0;
            firstFrameIndex = -1;
            msFrameAverage = 0;
            msMin = float.MaxValue;
            msMax = 0;
            minFrameIndex = 0;
            maxFrameIndex = 0;
            msMinIndividual = float.MaxValue;
            msMaxIndividual = 0;
            minIndividualFrameIndex = 0;
            maxIndividualFrameIndex = 0;
            minDepth = 0;
            maxDepth = 0;
            for (int b = 0; b < buckets.Length; b++)
                buckets[b] = 0;
        }

        public void ComputeBuckets(float min, float max)
        {
            float first = min;
            float last = max;
            float range = last - first;

            int maxBucketIndex = (buckets.Length - 1);

            for (int bucketIndex = 0; bucketIndex < buckets.Length; bucketIndex++)
            {
                buckets[bucketIndex] = 0;
            }

            foreach (FrameTime frameTime in frames)
            {
                var ms = frameTime.ms;
                //int frameIndex = frameTime.frameIndex;

                int bucketIndex = (range > 0) ? (int)((maxBucketIndex * (ms - first)) / range) : 0;
                if (bucketIndex < 0 || bucketIndex > maxBucketIndex)
                {
                    // This can happen if a single marker range is longer than the frame start end (which could occur if running on a separate thread)
                    // Debug.Log(string.Format("Marker {0} : {1}ms exceeds range {2}-{3} on frame {4}", marker.name, ms, first, last, 1+frameIndex));
                    if (bucketIndex > maxBucketIndex)
                        bucketIndex = maxBucketIndex;
                    else
                        bucketIndex = 0;
                }
                buckets[bucketIndex] += 1;
            }
        }
    }
}
