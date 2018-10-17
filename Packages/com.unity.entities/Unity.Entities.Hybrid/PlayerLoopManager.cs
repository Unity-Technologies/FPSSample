using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Entities
{
    public static class PlayerLoopManager
    {
        struct UnloadMethod : IComparable<UnloadMethod>
        {
            public CallbackFunction Function;
            public int Ordering;

            public int CompareTo(UnloadMethod other)
            {
                return Ordering - other.Ordering;
            }
        }

        static List<UnloadMethod> k_DomainUnloadMethods;

        public delegate void CallbackFunction();

        /// <summary>
        /// Register a function to be called when the scripting domain is unloading.
        /// </summary>
        /// <param name="callback">The function to call</param>
        /// <param name="ordering">The ordering. Lower ordering values get called earlier.</param>
        public static void RegisterDomainUnload(CallbackFunction callback, int ordering = 0)
        {
            if (k_DomainUnloadMethods == null)
            {
                var go = new GameObject();
                go.hideFlags = HideFlags.HideInHierarchy;
                if (Application.isPlaying)
                    UnityEngine.Object.DontDestroyOnLoad(go);
                else
                    go.hideFlags = HideFlags.HideAndDontSave;

                go.AddComponent<PlayerLoopDisableManager>().IsActive = true;

                k_DomainUnloadMethods = new List<UnloadMethod>();
            }

            k_DomainUnloadMethods.Add(new UnloadMethod { Function = callback, Ordering = ordering });
        }

        internal static void InvokeBeforeDomainUnload()
        {
            if (k_DomainUnloadMethods != null)
            {
                InvokeMethods(k_DomainUnloadMethods);
            }

            k_DomainUnloadMethods = null;
        }

        static void InvokeMethods(List<UnloadMethod> callbacks)
        {
            callbacks.Sort();

            foreach (var m in callbacks)
            {
                var callback = m.Function;

#if !UNITY_WINRT
                UnityEngine.Profiling.Profiler.BeginSample(callback.Method.DeclaringType.Name + "." + callback.Method.Name);
#endif

                // Isolate systems from each other
                try
                {
                    callback();
                }
                catch (Exception exc)
                {
                    Debug.LogException(exc);
                }


#if !UNITY_WINRT
                UnityEngine.Profiling.Profiler.EndSample();
#endif
            }
        }
    }
}
