using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using System.Threading;
using System.Text.RegularExpressions;
using System;

namespace ProfileAnalyser
{ 
    public class ProfileAnalyser
    {
        private int m_Progress = 0;
        private ProfilerFrameDataIterator m_frameData;
        private List<string> m_threadNames = new List<string>();
        private ProfileAnalysis m_analysis;
        private ProgressBarDisplay m_progressBar;
        public ProfileAnalyser(ProgressBarDisplay progressBar)
        {
            m_progressBar = progressBar;
        }

        public void QuickScan()
        {
            var frameData = new ProfilerFrameDataIterator();

            m_threadNames.Clear();
            int frameIndex = 0;
            int threadCount = frameData.GetThreadCount(0);
            frameData.SetRoot(frameIndex, 0);

            Dictionary<string, int> threadNameCount = new Dictionary<string, int>();
            for (int threadIndex = 0; threadIndex < threadCount; ++threadIndex)
            {
                frameData.SetRoot(frameIndex, threadIndex);

                var threadName = frameData.GetThreadName();
                if (!threadNameCount.ContainsKey(threadName))
                    threadNameCount.Add(threadName, 1);
                else
                    threadNameCount[threadName] += 1;
                m_threadNames.Add(ProfileData.ThreadNameWithIndex(threadNameCount[threadName], threadName));
            }

            frameData.Dispose();
        }

        public List<string> GetThreadNames()
        {
            return m_threadNames;
        }

        public ProfileData GrabFromProfiler(int firstFrameDisplayIndex, int lastFrameDisplayIndex)
        {
            ProfilerFrameDataIterator frameData = new ProfilerFrameDataIterator();
            int firstFrameIndex = firstFrameDisplayIndex - 1;
            int lastFrameIndex = lastFrameDisplayIndex - 1;
            ProfileData profileData = GetData(frameData, firstFrameIndex, lastFrameIndex);
            frameData.Dispose();
            return profileData;
        }

        private ProfileData GetData(ProfilerFrameDataIterator frameData, int firstFrameIndex, int lastFrameIndex)
        {
            var data = new ProfileData();
            data.SetFrameIndexOffset(firstFrameIndex);

            Dictionary<string, int> threadNameCount = new Dictionary<string, int>();
            for (int frameIndex = firstFrameIndex; frameIndex <= lastFrameIndex; ++frameIndex)
            {
                m_progressBar.AdvanceProgressBar();

                int threadCount = frameData.GetThreadCount(frameIndex);
                frameData.SetRoot(frameIndex, 0);

                var msFrame = frameData.frameTimeMS;

                ProfileFrame frame = new ProfileFrame();
                frame.msFrame = msFrame;
                data.Add(frame);

                threadNameCount.Clear();
                for (int threadIndex = 0; threadIndex < threadCount; ++threadIndex)
                {
                    frameData.SetRoot(frameIndex, threadIndex);

                    var threadName = frameData.GetThreadName();
                    if (threadName.Trim() == "")
                    {
                        Debug.Log(string.Format("Warning: Unnamed thread found on frame {0}. Corrupted data suspected, ignoring frame", frameIndex));
                        continue;
                    }

                    ProfileThread thread = new ProfileThread();
                    frame.Add(thread);

                    if (!threadNameCount.ContainsKey(threadName))
                        threadNameCount.Add(threadName,1);
                    else
                        threadNameCount[threadName] += 1;
                    data.AddThreadName(ProfileData.ThreadNameWithIndex(threadNameCount[threadName], threadName), thread);

                    const bool enterChildren = true;
                    while (frameData.Next(enterChildren))
                    {
                        var ms = frameData.durationMS;
                        var markerData = ProfileMarker.Create(frameData);
                        thread.Add(markerData);
                        data.AddMarkerName(frameData.name, markerData);
                    }
                }
            }

            return data;
        }


        private int GetClampedOffsetToFrame(ProfileData profileData, int frameIndex)
        {
            int frameOffset = profileData.DisplayFrameToOffset(frameIndex);
            if (frameOffset < 0)
            {
                Debug.Log(string.Format("Frame index {0} offset {1} < 0, clamping", frameIndex, frameOffset));
                frameOffset = 0;
            }
            if (frameOffset >= profileData.GetFrameCount())
            {
                Debug.Log(string.Format("Frame index {0} offset {1} >= frame count {2}, clamping", frameIndex, frameOffset, profileData.GetFrameCount()));
                frameOffset = profileData.GetFrameCount() - 1;
            }

            return frameOffset;
        }

        public ProfileAnalysis Analyse(ProfileData profileData, int firstFrameIndex, int lastFrameIndex, string threadFilter, int depthFilter, string nameFilter, string nameExclude, float timeScaleMax = 0)
        {
            m_Progress = 0;
            if (profileData == null)
            {
                return null;
            }

            int frameCount = 1 + (lastFrameIndex - firstFrameIndex);
            if (frameCount <= 0)
            {
                return null;
            }

            bool filterThreads = (!string.IsNullOrEmpty(threadFilter) && threadFilter != "All");
            bool filterThreadGroup = false;
            string threadGroupPrefix = "All:";
            if (filterThreads && threadFilter.StartsWith(threadGroupPrefix))
            {
                threadFilter = threadFilter.Substring(threadGroupPrefix.Length);
                filterThreadGroup = true;
            }
            bool processMarkers = (threadFilter != "None");

            ProfileAnalysis analysis = new ProfileAnalysis();
            analysis.SetRange(firstFrameIndex,lastFrameIndex);

            m_threadNames.Clear();

            int firstFrameOffset = GetClampedOffsetToFrame(profileData, firstFrameIndex);
            int lastFrameOffset = GetClampedOffsetToFrame(profileData, lastFrameIndex);

            List<string> nameFilters = new List<string>();
            if (!string.IsNullOrEmpty(nameFilter))
            {
                Regex whitespace = new Regex("[ \t]+", RegexOptions.IgnoreCase);
                string nameFilterSingleWhiteSpace = whitespace.Replace(nameFilter, " ").Trim();
                if (!string.IsNullOrEmpty(nameFilterSingleWhiteSpace))
                    nameFilters.AddRange(nameFilterSingleWhiteSpace.Split(' '));
            }

            List<string> nameExcludes = new List<string>();
            if (!string.IsNullOrEmpty(nameExclude))
            {
                Regex whitespace = new Regex("[ \t]+", RegexOptions.IgnoreCase);
                string nameExcludeSingleWhiteSpace = whitespace.Replace(nameExclude, " ").Trim();
                if (!string.IsNullOrEmpty(nameExcludeSingleWhiteSpace))
                    nameExcludes.AddRange(nameExcludeSingleWhiteSpace.Split(' '));
            }

            int maxMarkerDepthFound = 0;
            Dictionary<string, ThreadData> threads = new Dictionary<string, ThreadData>();
            Dictionary<string, MarkerData> markers = new Dictionary<string, MarkerData>();
            Dictionary<string, int> allMarkers = new Dictionary<string,int>();
            for (int frameOffset = firstFrameOffset; frameOffset <= lastFrameOffset; frameOffset++)
            {
                var frameData = profileData.GetFrame(frameOffset);
                var msFrame = frameData.msFrame;

                int frameIndex = profileData.OffsetToDisplayFrame(frameOffset);
                analysis.UpdateSummary(frameIndex, msFrame);

                if (processMarkers)
                {
                    for (int threadIndex = 0; threadIndex < frameData.threads.Count; threadIndex++)
                    {
                        float msTimeOfMinDepthMarkers = 0.0f;
                        float msIdleTimeOfMinDepthMarkers = 0.0f;

                        var threadData = frameData.threads[threadIndex];
                        var threadNameWithIndex = profileData.GetThreadName(threadData);

                        ThreadData thread;
                        if (!threads.ContainsKey(threadNameWithIndex))
                        {
                            m_threadNames.Add(threadNameWithIndex);

                            thread = new ThreadData(threadNameWithIndex);

                            analysis.AddThread(thread);
                            threads[threadNameWithIndex] = thread;

                            // Update threadsInGroup for all thread records of the same group name
                            foreach (var threadAt in threads.Values)
                            {
                                if (threadAt == thread)
                                    continue;
                                
                                if (thread.threadGroupName == threadAt.threadGroupName)
                                {
                                    threadAt.threadsInGroup += 1;
                                    thread.threadsInGroup += 1;
                                }
                            }
                        }
                        else
                        {
                            thread = threads[threadNameWithIndex];
                        }

                        bool include = true;
                        if (filterThreads)
                        {
                            if (filterThreadGroup)
                            {
                                var threadName = threadNameWithIndex.Substring(threadNameWithIndex.IndexOf(':') + 1);

                                if (threadFilter != threadName)
                                    include = false;
                            }
                            else
                            {
                                if (threadFilter != threadNameWithIndex)
                                    include = false;
                            }
                        }

                        foreach (var markerData in threadData.markers)
                        {
                            var markerName = profileData.GetMarkerName(markerData);

                            if (!allMarkers.ContainsKey(markerName))
                                allMarkers.Add(markerName,0);
                            else
                                allMarkers[markerName]++;

                            var ms = markerData.msFrame;
                            var markerDepth = markerData.depth;
                            if (markerDepth > maxMarkerDepthFound)
                                maxMarkerDepthFound = markerDepth;

                            if (markerDepth == 1)
                            {
                                if (markerName == "Idle")
                                    msIdleTimeOfMinDepthMarkers += ms;
                                else
                                    msTimeOfMinDepthMarkers += ms;
                            }

                            if (!include)
                                continue;
                            
                            if (depthFilter>=0 && markerDepth != depthFilter)
                                continue;

                            if (nameFilters.Count>0)
                            {
                                // 'And' list
                                bool match = true;
                                foreach (var subString in nameFilters)
                                {
                                    //if (!markerName.Contains(subString)) // Case sensitive
                                    if (markerName.IndexOf(subString, StringComparison.OrdinalIgnoreCase) < 0)
                                    {
                                        match = false;
                                        break;
                                    }
                                }
                                if (!match)
                                    continue;
                            }

                            if (nameExcludes.Count>0)
                            {
                                // 'Or' list
                                bool match = false;
                                foreach (var subString in nameExcludes)
                                {
                                    if (markerName.IndexOf(subString, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        match = true;
                                        break;
                                    }
                                }
                                if (match)
                                    continue;
                            }

                            MarkerData marker;
                            if (markers.ContainsKey(markerName))
                            {
                                marker = markers[markerName];
                            }
                            else
                            {
                                marker = new MarkerData(markerName);
                                marker.firstFrameIndex = frameIndex;
                                marker.minDepth = markerDepth;
                                marker.maxDepth = markerDepth;
                                analysis.AddMarker(marker);
                                markers.Add(markerName, marker);
                            }

                            if (frameIndex != marker.lastFrame)
                            {
                                marker.presentOnFrameCount += 1;
                                marker.frames.Add(new FrameTime(frameIndex, 0.0f, 1));
                                marker.lastFrame = frameIndex;
                            }
                            else
                            {
                                marker.frames[marker.frames.Count - 1].count += 1;
                            }
                            marker.count += 1;
                            marker.msTotal += ms;

                            // Individual marker time (not total over frame)
                            if (ms < marker.msMinIndividual)
                            {
                                marker.msMinIndividual = ms;
                                marker.minIndividualFrameIndex = frameIndex;
                            }
                            if (ms > marker.msMaxIndividual)
                            {
                                marker.msMaxIndividual = ms;
                                marker.maxIndividualFrameIndex = frameIndex;
                            }

                            // Record highest depth foun
                            if (markerDepth<marker.minDepth)
                                marker.minDepth = markerDepth;
                            if (markerDepth > marker.maxDepth)
                                marker.maxDepth = markerDepth;

                            marker.frames[marker.frames.Count - 1].ms += ms;
                        }

                        thread.frames.Add(new ThreadFrameTime(frameIndex, msTimeOfMinDepthMarkers, msIdleTimeOfMinDepthMarkers));
                    }
                }

                m_Progress = (100 * (frameOffset - firstFrameOffset)) / frameCount;
            }

            analysis.GetFrameSummary().totalMarkers = allMarkers.Count;
            analysis.Finalise(timeScaleMax, maxMarkerDepthFound);

            /*
            for (int frameOffset = firstFrameOffset; frameOffset <= lastFrameOffset; frameOffset++)
            {
                var frameData = profileData.GetFrame(frameOffset);
                int frameIndex = profileData.OffsetToDisplayFrame(frameOffset);
                foreach (var threadData in frameData.threads)
                { 
                    var threadNameWithIndex = profileData.GetThreadName(threadData);

                    if (filterThreads && threadFilter != threadNameWithIndex)
                        continue;

                    const bool enterChildren = true;
                    foreach (var markerData in threadData.markers)
                    {
                        var markerName = markerData.name;
                        var ms = markerData.msFrame;
                        var markerDepth = markerData.depth;
                        if (depthFilter>=0 && markerDepth != depthFilter)
                            continue;

                        MarkerData marker = markers[markerName];
                        bucketIndex = (range > 0) ? (int)(((marker.buckets.Length-1) * (ms - first)) / range) : 0;
                        if (bucketIndex<0 || bucketIndex > (marker.buckets.Length - 1))
                        {
                            // This can happen if a single marker range is longer than the frame start end (which could occur if running on a separate thread)
                            // Debug.Log(string.Format("Marker {0} : {1}ms exceeds range {2}-{3} on frame {4}", marker.name, ms, first, last, frameIndex));
                            if (bucketIndex > (marker.buckets.Length - 1))
                                bucketIndex = (marker.buckets.Length - 1);
                            else
                                bucketIndex = 0;
                        }
                        marker.individualBuckets[bucketIndex] += 1;
                    }
                }
            }
*/
            m_Progress = 100;
            return analysis;
        }

        public int GetProgress()
        {
            return m_Progress;
        }
    }
}