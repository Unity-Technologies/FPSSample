using UnityEditor;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(HDShadowSettings))]
    public class HDShadowSettingsEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_MaxShadowDistance;

        SerializedDataParameter m_CascadeShadowSplitCount;

        SerializedDataParameter[] m_CascadeShadowSplits = new SerializedDataParameter[3];
        SerializedDataParameter[] m_CascadeShadowBorders = new SerializedDataParameter[4];

        public override void OnEnable()
        {
            var o = new PropertyFetcher<HDShadowSettings>(serializedObject);

            m_MaxShadowDistance = Unpack(o.Find(x => x.maxShadowDistance));
            m_CascadeShadowSplitCount = Unpack(o.Find(x => x.cascadeShadowSplitCount));
            m_CascadeShadowSplits[0] = Unpack(o.Find(x => x.cascadeShadowSplit0));
            m_CascadeShadowSplits[1] = Unpack(o.Find(x => x.cascadeShadowSplit1));
            m_CascadeShadowSplits[2] = Unpack(o.Find(x => x.cascadeShadowSplit2));
            m_CascadeShadowBorders[0] = Unpack(o.Find(x => x.cascadeShadowBorder0));
            m_CascadeShadowBorders[1] = Unpack(o.Find(x => x.cascadeShadowBorder1));
            m_CascadeShadowBorders[2] = Unpack(o.Find(x => x.cascadeShadowBorder2));
            m_CascadeShadowBorders[3] = Unpack(o.Find(x => x.cascadeShadowBorder3));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_MaxShadowDistance, CoreEditorUtils.GetContent("Max Distance"));

            EditorGUILayout.Space();
            PropertyField(m_CascadeShadowSplitCount, CoreEditorUtils.GetContent("Cascade Count"));

            if (!m_CascadeShadowSplitCount.value.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                int splitCount = m_CascadeShadowSplitCount.value.intValue;
                for (int i = 0; i < splitCount - 1; i++)
                {
                    PropertyField(m_CascadeShadowSplits[i], CoreEditorUtils.GetContent(string.Format("Split {0}", i + 1)));
                }

                if (LightLoop.s_UseCascadeBorders)
                {
                    EditorGUILayout.Space();

                    for (int i = 0; i < splitCount; i++)
                    {
                        PropertyField(m_CascadeShadowBorders[i], CoreEditorUtils.GetContent(string.Format("Border {0}", i + 1)));
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}
