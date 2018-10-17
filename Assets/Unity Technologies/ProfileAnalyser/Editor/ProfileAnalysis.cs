using System.Collections.Generic;
using UnityEngine;

namespace ProfileAnalyser
{
    public class ProfileAnalysis
    {
        private FrameSummary m_frameSummary = new FrameSummary();
        private List<MarkerData> m_markers = new List<MarkerData>();
        private List<ThreadData> m_threads = new List<ThreadData>();

        public ProfileAnalysis()
        {
            m_frameSummary.first = 0;
            m_frameSummary.last = 0;
            m_frameSummary.count = 0;
            m_frameSummary.msTotal = 0.0;
            m_frameSummary.msMin = float.MaxValue;
            m_frameSummary.msMax = 0;
            m_frameSummary.minFrameIndex = 0;
            m_frameSummary.maxFrameIndex = 0;
            m_frameSummary.maxMarkerDepth = 0;
            for (int b = 0; b < m_frameSummary.buckets.Length; b++)
                m_frameSummary.buckets[b] = 0;

            m_markers.Clear();
            m_threads.Clear();
        }

        public void SetRange(int firstFrameIndex, int lastFrameIndex)
        {
            m_frameSummary.first = firstFrameIndex;
            m_frameSummary.last = lastFrameIndex;
        }

        public void AddMarker(MarkerData marker)
        {
            m_markers.Add(marker);
        }

        public void AddThread(ThreadData thread)
        {
            m_threads.Add(thread);
        }

        public void UpdateSummary(int frameIndex, float msFrame)
        {
            m_frameSummary.msTotal += msFrame;
            m_frameSummary.count += 1;
            if (msFrame < m_frameSummary.msMin)
            {
                m_frameSummary.msMin = msFrame;
                m_frameSummary.minFrameIndex = frameIndex;
            }
            if (msFrame > m_frameSummary.msMax)
            {
                m_frameSummary.msMax = msFrame;
                m_frameSummary.maxFrameIndex = frameIndex;
            }

            m_frameSummary.frames.Add(new FrameTime(frameIndex, msFrame, 1));
        }

        private float GetPercentageOffset(List<FrameTime> frames, float percent, out int outputFrameIndex)
        {
            int index = (int)((frames.Count-1) * percent / 100);
            outputFrameIndex = frames[index].frameIndex;

            // True median is half of the sum of the middle 2 frames for an even count. However this would be a value never recorded so we avoid that.
            return frames[index].ms;
        }

        private float GetThreadPercentageOffset(List<ThreadFrameTime> frames, float percent, out int outputFrameIndex)
        {
            int index = (int)((frames.Count-1) * percent / 100);
            outputFrameIndex = frames[index].frameIndex;

            // True median is half of the sum of the middle 2 frames for an even count. However this would be a value never recorded so we avoid that.
            return frames[index].ms;
        }

        public void SetupMarkers()
        {
            foreach (MarkerData marker in m_markers)
            {
                marker.msAtMedian = 0.0f;
                marker.msMin = float.MaxValue;
                marker.msMax = 0;
                marker.minFrameIndex = 0;
                marker.maxFrameIndex = 0;

                foreach (FrameTime frameTime in marker.frames)
                {
                    var ms = frameTime.ms;
                    int frameIndex = frameTime.frameIndex;

                    // Total time for marker over frame
                    if (ms < marker.msMin)
                    {
                        marker.msMin = ms;
                        marker.minFrameIndex = frameIndex;
                    }
                    if (ms > marker.msMax)
                    {
                        marker.msMax = ms;
                        marker.maxFrameIndex = frameIndex;
                    }

                    if (frameIndex == m_frameSummary.medianFrameIndex)
                        marker.msAtMedian = ms;
                }

                marker.msFrameAverage = (float)(marker.msTotal / marker.presentOnFrameCount);
                marker.frames.Sort();
                marker.msMedian = GetPercentageOffset(marker.frames, 50, out marker.medianFrameIndex);
                int unusedIndex;
                marker.msLowerQuartile = GetPercentageOffset(marker.frames, 25, out unusedIndex);
                marker.msUpperQuartile = GetPercentageOffset(marker.frames, 75, out unusedIndex);
                // No longer need the frame time list ?
                //marker.msFrame.Clear();
            }
        }

        public void SetupMarkerBuckets()
        {
            foreach (MarkerData marker in m_markers)
            {
                marker.ComputeBuckets(marker.msMin, marker.msMax);
            }
        }

        public void SetupFrameBuckets(float timeScaleMax)
        {
            float first = 0;
            float last = timeScaleMax;
            float range = last - first;
            int maxBucketIndex = m_frameSummary.buckets.Length - 1;

            foreach (var frameData in m_frameSummary.frames)
            {
                var msFrame = frameData.ms;
                var frameIndex = frameData.frameIndex;

                int bucketIndex = (range > 0) ? (int)((maxBucketIndex * (msFrame - first)) / range) : 0;
                if (bucketIndex < 0 || bucketIndex > maxBucketIndex)
                {
                    // This should never happen
                    Debug.Log(string.Format("Frame {0}ms exceeds range {1}-{2} on frame {3}", msFrame, first, last, frameIndex));
                    if (bucketIndex > maxBucketIndex)
                        bucketIndex = maxBucketIndex;
                    else
                        bucketIndex = 0;
                }
                m_frameSummary.buckets[bucketIndex] += 1;
            }
        }

        private void CalculateThreadMedians()
        {
            foreach (var thread in m_threads)
            {
                thread.frames.Sort();
                int unusedIndex;
                thread.msMin = GetThreadPercentageOffset(thread.frames, 0, out thread.minFrameIndex);
                thread.msLowerQuartile = GetThreadPercentageOffset(thread.frames, 25, out unusedIndex);
                thread.msMedian = GetThreadPercentageOffset(thread.frames, 50, out thread.medianFrameIndex);
                thread.msUpperQuartile = GetThreadPercentageOffset(thread.frames, 75, out unusedIndex);
                thread.msMax = GetThreadPercentageOffset(thread.frames, 100, out thread.maxFrameIndex);
            }
        }

        public void Finalise(float timeScaleMax, int maxMarkerDepth)
        {
            m_frameSummary.msAverage = (float)(m_frameSummary.msTotal / m_frameSummary.count);
            m_frameSummary.frames.Sort();
            m_frameSummary.msMedian = GetPercentageOffset(m_frameSummary.frames, 50, out m_frameSummary.medianFrameIndex);
            int unusedIndex;
            m_frameSummary.msLowerQuartile = GetPercentageOffset(m_frameSummary.frames, 25, out unusedIndex);
            m_frameSummary.msUpperQuartile = GetPercentageOffset(m_frameSummary.frames, 75, out unusedIndex);
            // No longer need the frame time list ?
            //m_frameSummary.msFrame.Clear();
            m_frameSummary.maxMarkerDepth = maxMarkerDepth;

            if (timeScaleMax <= 0.0f)
            {
                // If max frame time range not specified then use the max frame value found.
                timeScaleMax = m_frameSummary.msMax;
            }

            SetupMarkers();
            SetupMarkerBuckets();
            SetupFrameBuckets(timeScaleMax);

            // Sort in median order (highest first)
            m_markers.Sort(SortByAtMedian);

            CalculateThreadMedians();
        }

        private int SortByAtMedian(MarkerData a, MarkerData b)
        {
            return -a.msAtMedian.CompareTo(b.msAtMedian);
        }

        public List<MarkerData> GetMarkers()
        {
            return m_markers;
        }

        public List<ThreadData> GetThreads()
        {
            return m_threads;
        }

        public ThreadData GetThreadByName(string threadNameWithIndex)
        {
            foreach (var thread in m_threads)
            {
                if (thread.threadNameWithIndex == threadNameWithIndex)
                    return thread;
            }

            return null;
        }

        public FrameSummary GetFrameSummary()
        {
            return m_frameSummary;
        }

        public MarkerData GetMarker(int index)
        {
            if (index < 0 || index >= m_markers.Count)
                return null;

            return m_markers[index];
        }

        public int GetMarkerIndexByName(string markerName)
        {
            if (markerName == null)
                return -1;

            for (int index = 0; index < m_markers.Count; index++)
            {
                var marker = m_markers[index];
                if (marker.name == markerName)
                {
                    return index;
                }
            }

            return -1;
        }

        public MarkerData GetMarkerByName(string markerName)
        {
            if (markerName == null)
                return null;

            for (int index = 0; index < m_markers.Count; index++)
            {
                var marker = m_markers[index];
                if (marker.name == markerName)
                {
                    return marker;
                }
            }

            return null;
        }
    }
}