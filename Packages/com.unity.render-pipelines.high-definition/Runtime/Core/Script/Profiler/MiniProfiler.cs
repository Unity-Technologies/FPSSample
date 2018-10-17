using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public class MiniProfiler : MonoBehaviour
    {
        private bool m_Enable = false;

        private const int k_AverageFrameCount = 64;

        private int m_frameCount = 0;
        private float m_AccDeltaTime;
        private float m_AvgDeltaTime;

        internal class RecorderEntry
        {
            public string name;
            public float time;
            public int count;
            public float avgTime;
            public float avgCount;
            public float accTime;
            public int accCount;
            public Recorder recorder;
        };

        RecorderEntry[] recordersList =
        {
            new RecorderEntry() { name="RenderLoop.Draw" },
            new RecorderEntry() { name="Shadows.Draw" },
            new RecorderEntry() { name="RenderLoopNewBatcher.Draw" },
            new RecorderEntry() { name="ShadowLoopNewBatcher.Draw" },
            new RecorderEntry() { name="RenderLoopDevice.Idle" },
        };

        void OnEnable()
        {
            RegisterDebug("Frame Statistics");
        }

        void Ondisable()
        {
            UnRegisterDebug("Frame Statistics");
        }

        void Awake()
        {
            for (int i = 0; i < recordersList.Length; i++)
            {
                var sampler = Sampler.Get(recordersList[i].name);
                if (sampler != null)
                {
                    recordersList[i].recorder = sampler.GetRecorder();
                }
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                GraphicsSettings.useScriptableRenderPipelineBatching = !GraphicsSettings.useScriptableRenderPipelineBatching;
            }

            if (m_Enable)
            {

                // get timing & update average accumulators
                for (int i = 0; i < recordersList.Length; i++)
                {
                    recordersList[i].time = recordersList[i].recorder.elapsedNanoseconds / 1000000.0f;
                    recordersList[i].count = recordersList[i].recorder.sampleBlockCount;
                    recordersList[i].accTime += recordersList[i].time;
                    recordersList[i].accCount += recordersList[i].count;
                }

                m_AccDeltaTime += Time.deltaTime;

                m_frameCount++;
                // time to time, update average values & reset accumulators
                if (m_frameCount >= k_AverageFrameCount)
                {
                    for (int i = 0; i < recordersList.Length; i++)
                    {
                        recordersList[i].avgTime = recordersList[i].accTime * (1.0f / k_AverageFrameCount);
                        recordersList[i].avgCount = recordersList[i].accCount * (1.0f / k_AverageFrameCount);
                        recordersList[i].accTime = 0.0f;
                        recordersList[i].accCount = 0;

                    }

                    m_AvgDeltaTime = m_AccDeltaTime / k_AverageFrameCount;
                    m_AccDeltaTime = 0.0f;
                    m_frameCount = 0;
                }
            }

        }

        void OnGUI()
        {
            if (m_Enable)
            {
                GraphicsSettings.useScriptableRenderPipelineBatching = GUI.Toggle(new Rect(10, 28, 200, 20), GraphicsSettings.useScriptableRenderPipelineBatching, "SRP Batcher (F9)");
                GUI.skin.label.fontSize = 17;
                GUI.color = new Color(1, 1, 1, 1);
                float w = 800, h = 24 + (recordersList.Length + 10) * 18 + 8;

                GUILayout.BeginArea(new Rect(32, 50, w, h), "Mini Profiler", GUI.skin.window);
                string sLabel = System.String.Format("<b>{0:F2} FPS ({1:F2}ms)</b>\n", 1.0f / m_AvgDeltaTime, Time.deltaTime * 1000.0f);
                for (int i = 0; i < recordersList.Length; i++)
                {
                    sLabel += string.Format("{0:F2}ms (*{1:F2})\t({2:F2}ms *{3:F2})\t<b>{4}</b>\n", recordersList[i].avgTime, recordersList[i].avgCount, recordersList[i].time, recordersList[i].count, recordersList[i].name);
                }
                GUILayout.Label(sLabel);

                //Memory =========================================================/* Added by Ming Wai */
                long num1 = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() / 1024 / 1024;
                long num2 = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024 / 1024;
                long num3 = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1024 / 1024;
                long num4 = UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong() / 1024 / 1024;
                //long num5 = UnityEngine.Profiling.Profiler.GetTempAllocatorSize() / 1024 / 1024;

                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    "Allocated Mem For GfxDriver\n" +
                    "Total Allocated Mem\n" +
                    "Total Reserved Mem\n" +
                    "Total Unused Reserved Mem\n"//+
                                                 //"Temp Allocator Size\n"
                    );

                GUILayout.Label(
                    num1 + " mb\n" +
                    num2 + " mb\n" +
                    num3 + " mb\n" +
                    num4 + " mb\n"//+
                                  //num5+" mb\n"
                    );
                GUILayout.EndHorizontal();

                GUILayout.EndArea();
            }
        }

        public void RegisterDebug(string menuName)
        {
            List<DebugUI.Widget> widgets = new List<DebugUI.Widget>();
            widgets.AddRange(
                new DebugUI.Widget[]
            {
                    new DebugUI.Container
                    {
                        displayName = "Mini Profiler",
                        children =
                        {
                            new DebugUI.BoolField { displayName = "Enable Mini Profiler", getter = () => m_Enable, setter = value => m_Enable = value },
                            new DebugUI.BoolField { displayName = "Enable New Batcher", getter = () => GraphicsSettings.useScriptableRenderPipelineBatching , setter = value => GraphicsSettings.useScriptableRenderPipelineBatching  = value },
                        }
                    },
            });

            var panel = DebugManager.instance.GetPanel(menuName, true);
            panel.children.Add(widgets.ToArray());
        }

        public static void UnRegisterDebug(string menuName)
        {
            DebugManager.instance.RemovePanel(menuName);
        }
    }
}
