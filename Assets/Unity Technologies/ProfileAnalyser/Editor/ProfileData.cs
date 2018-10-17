using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

namespace ProfileAnalyser
{
    [Serializable]
    public class ProfileData
    {
        static int latestVersion = 1;
        private int version = latestVersion;
        private int frameIndexOffset = 0;
        private List<ProfileFrame> frames = new List<ProfileFrame>();
        private List<string> markerNames = new List<string>();
        private List<string> threadNames = new List<string>();

        public ProfileData()
        {
        }

        static public string ThreadNameWithIndex(int index, string threadName)
        {
            return string.Format("{0}:{1}", index, threadName);
        }

        public void SetFrameIndexOffset(int offset)
        {
            frameIndexOffset = offset;
        }

        public int GetFrameCount()
        {
            return frames.Count;
        }

        public ProfileFrame GetFrame(int offset)
        {
            return frames[offset];
        }

        public List<string> GetThreadNames()
        {
            return threadNames;
        }

        public int OffsetToDisplayFrame(int offset)
        {
            return offset + (1 + frameIndexOffset);
        }

        public int DisplayFrameToOffset(int displayFrame)
        {
            return displayFrame - (1 + frameIndexOffset);
        }

        public void AddThreadName(string threadName, ProfileThread thread)
        {
            threadName = CorrectThreadName(threadName);

            int index = threadNames.IndexOf(threadName);
            if (index == -1)
            {
                threadNames.Add(threadName);
                index = threadNames.Count - 1;
            }

            thread.threadIndex = index;
        }

        public void AddMarkerName(string markerName, ProfileMarker marker)
        {
            int index = markerNames.IndexOf(markerName);
            if (index == -1)
            {
                markerNames.Add(markerName);
                index = markerNames.Count - 1;
            }

            marker.nameIndex = index;
        }

        public string GetThreadName(ProfileThread thread)
        {
            return threadNames[thread.threadIndex];
        }
        public string GetMarkerName(ProfileMarker marker)
        {
            return markerNames[marker.nameIndex];
        }

        public void Add(ProfileFrame frame)
        {
            frames.Add(frame);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(version);
            writer.Write(frameIndexOffset);

            writer.Write(frames.Count);
            foreach (var frame in frames)
            {
                frame.Write(writer);
            };

            writer.Write(markerNames.Count);
            foreach (var markerName in markerNames)
            {
                writer.Write(markerName);
            };

            writer.Write(threadNames.Count);
            foreach (var threadName in threadNames)
            {
                writer.Write(threadName);
            };
        }

        private string CorrectThreadName(string threadNameWithIndex)
        {
            var info = threadNameWithIndex.Split(':');
            if (info.Length >= 2)
            {
                string threadGroupIndexString = info[0];
                string threadName = info[1];
                if (threadName.Trim() == "")
                {
                    // Scan seen with no thread name
                    threadNameWithIndex = string.Format("{0}:[Unknown]", threadGroupIndexString);
                }
                else
                {
                    // Some scans have thread names such as 
                    // "1:Worker Thread 0" 
                    // "1:Worker Thread 1" 
                    // rather than
                    // "1:Worker Thread"
                    // "2:Worker Thread"
                    // Update to the second format so the 'All' case is correctly determined
                    Regex trailingDigit = new Regex(@"^(.*)[\s]*([\d+])$");
                    Match m = trailingDigit.Match(threadName);
                    if (m.Success)
                    {
                        string threadNamePrefix = m.Groups[1].Value;
                        int threadGroupIndex = 1 + int.Parse(m.Groups[2].Value);

                        threadNameWithIndex = string.Format("{0}:{1}", threadGroupIndex, threadNamePrefix);
                    }
                }
            }

            return threadNameWithIndex;
        }

        public ProfileData(BinaryReader reader)
        {
            version = reader.ReadInt32();
            if (version != latestVersion)
            {
                throw new Exception(String.Format("File version unsupported : {0} != {1} expected", version, latestVersion));
            }

            frameIndexOffset = reader.ReadInt32();
            int frameCount = reader.ReadInt32();
            frames.Clear();
            for (int frame = 0; frame < frameCount; frame++)
            {
                frames.Add(new ProfileFrame(reader));
            }

            int markerCount = reader.ReadInt32();
            markerNames.Clear();
            for (int marker = 0; marker < markerCount; marker++)
            {
                markerNames.Add(reader.ReadString());
            }

            int threadCount = reader.ReadInt32();
            threadNames.Clear();
            for (int thread = 0; thread < threadCount; thread++)
            {
                var threadNameWithIndex = reader.ReadString();

                threadNameWithIndex = CorrectThreadName(threadNameWithIndex);

                threadNames.Add(threadNameWithIndex);
            }
        }

        
        public static bool Save(string filename, ProfileData data)
        {
            if (filename.EndsWith(".json"))
            {
                var json = JsonUtility.ToJson(data);
                File.WriteAllText(filename, json);
            }
            else if (filename.EndsWith(".padata"))
            {
                FileStream stream = File.Create(filename);
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, data);
                stream.Close();
            }
            else if (filename.EndsWith(".pdata"))
            {
                FileStream stream = File.Create(filename);
                using (var writer = new BinaryWriter(stream))
                {
                    data.Write(writer);
                }
            }

            return true;
        }

        public static bool Load(string filename, out ProfileData data)
        {
            if (filename.EndsWith(".json"))
            {
                string json = File.ReadAllText(filename);
                data = JsonUtility.FromJson<ProfileData>(json);
            }
            else if (filename.EndsWith(".padata"))
            {
                FileStream stream = File.OpenRead(filename);
                var formatter = new BinaryFormatter();
                data = (ProfileData)formatter.Deserialize(stream);
                stream.Close();

                if (data.version != latestVersion)
                {
                    Debug.Log(String.Format("Incorrect file version in {0} : (file {1} != {2} expected", filename, data.version, latestVersion));
                    data = null;
                    return false;
                }
            }
            else if (filename.EndsWith(".pdata"))
            {
                FileStream stream = File.OpenRead(filename);
                using (var reader = new BinaryReader(stream))
                {
                    try{
                        data = new ProfileData(reader);
                    }
                    catch (Exception e)
                    {
                        Debug.Log(String.Format("Incorrect file version in {0} : {1}", filename, e.ToString()));
                        data = null;
                        return false;
                    }
                }
            }
            else
            {
                data = null;
                return false;
            }

            //When loaded from disk the frame index offset is currently reset in the profiler view
            data.frameIndexOffset = 0;
            return true;
        }
    }

    [Serializable]
    public class ProfileFrame
    {
        public List<ProfileThread> threads = new List<ProfileThread>();
        public float msFrame;

        public ProfileFrame()
        {
        }

        public void Add(ProfileThread thread)
        {
            threads.Add(thread);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(msFrame);
            writer.Write(threads.Count);
            foreach (var thread in threads)
            {
                thread.Write(writer);
            };
        }

        public ProfileFrame(BinaryReader reader)
        {
            msFrame = reader.ReadSingle();
            int threadCount = reader.ReadInt32();
            threads.Clear();
            for (int thread = 0; thread < threadCount; thread++)
            {
                threads.Add(new ProfileThread(reader));
            }
        }
    }

    [Serializable]
    public class ProfileThread
    {
        public List<ProfileMarker> markers = new List<ProfileMarker>();
        public int threadIndex;

        public ProfileThread()
        {
        }

        public void Add(ProfileMarker marker)
        {
            markers.Add(marker);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(threadIndex);
            writer.Write(markers.Count);
            foreach (var marker in markers)
            {
                marker.Write(writer);
            };
        }

        public ProfileThread(BinaryReader reader)
        {
            threadIndex = reader.ReadInt32();
            int markerCount = reader.ReadInt32();
            markers.Clear();
            for (int marker = 0; marker < markerCount; marker++)
            {
                markers.Add(new ProfileMarker(reader));
            }
        }
    }

    [Serializable]
    public class ProfileMarker
    {
        public int nameIndex;
        public float msFrame;
        public int depth;

        public ProfileMarker()
        {
        }

        public static ProfileMarker Create(ProfilerFrameDataIterator frameData)
        {
            var item = new ProfileMarker
            {
                msFrame = frameData.durationMS,
                depth = frameData.depth
            };

            return item;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(nameIndex);
            writer.Write(msFrame);
            writer.Write(depth);
        }

        public ProfileMarker(BinaryReader reader)
        {
            nameIndex = reader.ReadInt32();
            msFrame = reader.ReadSingle();
            depth = reader.ReadInt32();
        }
    }
}