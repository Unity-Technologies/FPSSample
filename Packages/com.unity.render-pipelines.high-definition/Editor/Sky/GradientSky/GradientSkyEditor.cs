using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(GradientSky))]
    public class GradientSkySettingsEditor : SkySettingsEditor
    {
        SerializedDataParameter m_Bottom;
        SerializedDataParameter m_Middle;
        SerializedDataParameter m_Top;
        SerializedDataParameter m_GradientMultiplier;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<GradientSky>(serializedObject);

            m_Bottom = Unpack(o.Find(x => x.bottom));
            m_Middle = Unpack(o.Find(x => x.middle));
            m_Top = Unpack(o.Find(x => x.top));
            m_GradientMultiplier = Unpack(o.Find(x => x.gradientDiffusion));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Top);
            PropertyField(m_Middle);
            PropertyField(m_Bottom);
            PropertyField(m_GradientMultiplier);

            EditorGUILayout.Space();

            m_CommonUIElementsMask = (uint)SkySettingsUIElement.UpdateMode;

            base.CommonSkySettingsGUI();
        }
    }
}
