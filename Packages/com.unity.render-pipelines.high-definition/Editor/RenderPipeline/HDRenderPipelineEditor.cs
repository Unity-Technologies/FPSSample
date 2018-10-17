using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(HDRenderPipelineAsset))]
    public sealed class HDRenderPipelineEditor : Editor
    {
        SerializedHDRenderPipelineAsset m_SerializedHDRenderPipeline;
        HDRenderPipelineUI m_HDRenderPipelineUI = new HDRenderPipelineUI();

        void OnEnable()
        {
            m_SerializedHDRenderPipeline = new SerializedHDRenderPipelineAsset(serializedObject);
            m_HDRenderPipelineUI.Reset(m_SerializedHDRenderPipeline, Repaint);
        }

        public override void OnInspectorGUI()
        {
            var s = m_HDRenderPipelineUI;
            var d = m_SerializedHDRenderPipeline;
            var o = this;

            s.Update();
            d.Update();

            HDRenderPipelineUI.Inspector.Draw(s, d, o);

            d.Apply();
        }
    }
}
