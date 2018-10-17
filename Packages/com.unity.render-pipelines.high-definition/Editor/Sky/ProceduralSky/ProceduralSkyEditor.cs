using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ProceduralSky))]
    public class ProceduralSkySettingsEditor
        : SkySettingsEditor
    {
        SerializedDataParameter m_SunSize;
        SerializedDataParameter m_SunSizeConvergence;
        SerializedDataParameter m_AtmosphericThickness;
        SerializedDataParameter m_SkyTint;
        SerializedDataParameter m_GroundColor;
        SerializedDataParameter m_EnableSunDisk;

        public override void OnEnable()
        {
            base.OnEnable();

            // Procedural sky orientation depends on the sun direction
            m_CommonUIElementsMask = 0xFFFFFFFF & ~(uint)(SkySettingsUIElement.Rotation);

            var o = new PropertyFetcher<ProceduralSky>(serializedObject);

            m_SunSize = Unpack(o.Find(x => x.sunSize));
            m_SunSizeConvergence = Unpack(o.Find(x => x.sunSizeConvergence));
            m_AtmosphericThickness = Unpack(o.Find(x => x.atmosphereThickness));
            m_SkyTint = Unpack(o.Find(x => x.skyTint));
            m_GroundColor = Unpack(o.Find(x => x.groundColor));
            m_EnableSunDisk = Unpack(o.Find(x => x.enableSunDisk));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_EnableSunDisk);
            PropertyField(m_SunSize);
            PropertyField(m_SunSizeConvergence);
            PropertyField(m_AtmosphericThickness);
            PropertyField(m_SkyTint);
            PropertyField(m_GroundColor);

            EditorGUILayout.Space();

            base.CommonSkySettingsGUI();
        }
    }
}
