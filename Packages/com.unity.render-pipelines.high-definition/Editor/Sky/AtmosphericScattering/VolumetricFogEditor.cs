using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(VolumetricFog))]
    public class VolumetricFogEditor : AtmosphericScatteringEditor
    {
        SerializedDataParameter m_Albedo;
        SerializedDataParameter m_MeanFreePath;
        SerializedDataParameter m_BaseHeight;
        SerializedDataParameter m_MeanHeight;
        SerializedDataParameter m_Anisotropy;
        SerializedDataParameter m_GlobalLightProbeDimmer;
        SerializedDataParameter m_EnableDistantFog;

        static GUIContent s_AlbedoLabel                 = new GUIContent("Single Scattering Albedo", "Hue and saturation control the color of the fog (the wavelength of in-scattered light). Value controls scattering (0 = max absorption & no scattering, 1 = no absorption & max scattering).");
        static GUIContent s_MeanFreePathLabel           = new GUIContent("Base Fog Distance", "Controls the density, which determines how far you can see through the fog. A.k.a. \"mean free path length\". At this distance, 63% of background light is lost in the fog (due to absorption and out-scattering).");
        static GUIContent s_BaseHeightLabel             = new GUIContent("Base Height", "Height at which the exponential density falloff starts.");
        static GUIContent s_MeanHeightLabel             = new GUIContent("Mean Height", "Controls the rate of falloff of the height fog. Higher values stretch the fog vertically.");
        static GUIContent s_AnisotropyLabel             = new GUIContent("Global Anisotropy", "Controls the angular distribution of scattered light. 0 is isotropic, 1 is forward scattering, -1 is backward scattering.");
        static GUIContent s_GlobalLightProbeDimmerLabel = new GUIContent("Global Light Probe Dimmer", "Reduces the intensity of the global light probe.");
        static GUIContent s_EnableDistantFog            = new GUIContent("Enable Distant Fog", "Activates the fog with precomputed lighting behind the volumetrically-lit frustum.");

        public override void OnEnable()
        {
            base.OnEnable();
            var o = new PropertyFetcher<VolumetricFog>(serializedObject);

            m_Albedo                 = Unpack(o.Find(x => x.albedo));
            m_MeanFreePath           = Unpack(o.Find(x => x.meanFreePath));
            m_BaseHeight             = Unpack(o.Find(x => x.baseHeight));
            m_MeanHeight             = Unpack(o.Find(x => x.meanHeight));
            m_Anisotropy             = Unpack(o.Find(x => x.anisotropy));
            m_GlobalLightProbeDimmer = Unpack(o.Find(x => x.globalLightProbeDimmer));
            m_EnableDistantFog       = Unpack(o.Find(x => x.enableDistantFog));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Albedo,                 s_AlbedoLabel);
            PropertyField(m_MeanFreePath,           s_MeanFreePathLabel);
            PropertyField(m_BaseHeight,             s_BaseHeightLabel);
            PropertyField(m_MeanHeight,             s_MeanHeightLabel);
            PropertyField(m_Anisotropy,             s_AnisotropyLabel);
            PropertyField(m_GlobalLightProbeDimmer, s_GlobalLightProbeDimmerLabel);
            PropertyField(m_MaxFogDistance);
            PropertyField(m_EnableDistantFog,       s_EnableDistantFog);

            if (m_EnableDistantFog.value.boolValue)
            {
                EditorGUI.indentLevel++;
                base.OnInspectorGUI(); // Color
                EditorGUI.indentLevel--;
            }
        }
    }
}
