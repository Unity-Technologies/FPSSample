using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ScreenSpaceReflection))]
    public class HDScreenSpaceReflectionEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_ScreenFadeDistance;
        SerializedDataParameter m_RayMaxIterations;
        SerializedDataParameter m_DepthBufferThickness;
        SerializedDataParameter m_MinSmoothness;
        SerializedDataParameter m_SmoothnessFadeStart;
        SerializedDataParameter m_ReflectSky;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceReflection>(serializedObject);
            m_DepthBufferThickness = Unpack(o.Find(x => x.depthBufferThickness));
            m_RayMaxIterations = Unpack(o.Find(x => x.rayMaxIterations));
            m_ScreenFadeDistance = Unpack(o.Find(x => x.screenFadeDistance));
            m_MinSmoothness = Unpack(o.Find(x => x.minSmoothness));
            m_SmoothnessFadeStart = Unpack(o.Find(x => x.smoothnessFadeStart));
            m_ReflectSky          = Unpack(o.Find(x => x.reflectSky));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_ScreenFadeDistance,   CoreEditorUtils.GetContent("Screen Edge Fade Distance"));
            PropertyField(m_RayMaxIterations,     CoreEditorUtils.GetContent("Max Number of Ray Steps|Affects both correctness and performance."));
            PropertyField(m_DepthBufferThickness, CoreEditorUtils.GetContent("Object Thickness"));
            PropertyField(m_MinSmoothness,        CoreEditorUtils.GetContent("Min Smoothness|Smoothness value at which SSR is activated and the smoothness-controlled fade out stops."));
            PropertyField(m_SmoothnessFadeStart,  CoreEditorUtils.GetContent("Smoothness Fade Start|Smoothness value at which the smoothness-controlled fade out starts. The fade is in the range [Min Smoothness, Smoothness Fade Start], e.g. [0.8, 0.9]."));
            PropertyField(m_ReflectSky,           CoreEditorUtils.GetContent("Reflect sky|If disabled, sky reflection is never handled by SSR, and relies only on reflection probes."));

            m_RayMaxIterations.value.intValue       = Mathf.Max(0, m_RayMaxIterations.value.intValue);
            m_DepthBufferThickness.value.floatValue = Mathf.Clamp(m_DepthBufferThickness.value.floatValue, 0.001f, 1.0f);
            m_SmoothnessFadeStart.value.floatValue  = Mathf.Max(m_MinSmoothness.value.floatValue, m_SmoothnessFadeStart.value.floatValue);
        }
    }
}
