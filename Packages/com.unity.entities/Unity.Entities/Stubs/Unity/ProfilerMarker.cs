#if !UNITY_2018_3_OR_NEWER
using System;
using System.Diagnostics;

namespace Unity.Profiling
{
    public struct ProfilerMarker
    {
        public ProfilerMarker(string name)
        {
        }

        [Conditional("ENABLE_PROFILER")]
        public void Begin()
        {
        }

        [Conditional("ENABLE_PROFILER")]
        public void Begin(UnityEngine.Object contextUnityObject)
        {
        }

        [Conditional("ENABLE_PROFILER")]
        public void End()
        {
        }

        public struct AutoScope : IDisposable
        {
            public void Dispose()
            {
            }
        }

        public AutoScope Auto()
        {
            return new AutoScope();
        }
    }
}
#endif