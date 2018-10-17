using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(IndirectLightingController))]
    public class IndirectLightingControllerEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_IndirectDiffuseIntensity;
        SerializedDataParameter m_IndirectSpecularIntensity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<IndirectLightingController>(serializedObject);

            m_IndirectSpecularIntensity = Unpack(o.Find(x => x.indirectSpecularIntensity));
            m_IndirectDiffuseIntensity = Unpack(o.Find(x => x.indirectDiffuseIntensity));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_IndirectDiffuseIntensity, CoreEditorUtils.GetContent("Indirect Diffuse Intensity|Multiplier for the baked diffuse lighting."));
            PropertyField(m_IndirectSpecularIntensity, CoreEditorUtils.GetContent("Indirect Specular Intensity|Multiplier for the reflected specular light."));
        }
    }
}
