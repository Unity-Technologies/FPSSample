using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class ScreenSpaceLightingEditor : VolumeComponentEditor
    {
        protected SerializedDataParameter m_RayLevel;
        protected SerializedDataParameter m_RayMinLevel;
        protected SerializedDataParameter m_RayMaxLevel;
        protected SerializedDataParameter m_RayMaxIterations;
        protected SerializedDataParameter m_DepthBufferThickness;
        protected SerializedDataParameter m_ScreenWeightDistance;
        protected SerializedDataParameter m_RayMaxScreenDistance;
        protected SerializedDataParameter m_RayBlendScreenDistance;
        protected SerializedDataParameter m_RayMarchBehindObjects;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceLighting>(serializedObject);

            m_RayLevel = Unpack(o.Find(x => x.rayLevel));
            m_RayMinLevel = Unpack(o.Find(x => x.rayMinLevel));
            m_RayMaxLevel = Unpack(o.Find(x => x.rayMaxLevel));
            m_RayMaxIterations = Unpack(o.Find(x => x.rayMaxIterations));
            m_DepthBufferThickness = Unpack(o.Find(x => x.depthBufferThickness));
            m_ScreenWeightDistance = Unpack(o.Find(x => x.screenWeightDistance));
            m_RayMaxScreenDistance = Unpack(o.Find(x => x.rayMaxScreenDistance));
            m_RayBlendScreenDistance = Unpack(o.Find(x => x.rayBlendScreenDistance));
            m_RayMarchBehindObjects = Unpack(o.Find(x => x.rayMarchBehindObjects));
        }

        public override void OnInspectorGUI()
        {
            OnCommonInspectorGUI();
            EditorGUILayout.Separator();
            OnHiZInspectorGUI();
            EditorGUILayout.Separator();
            OnProxyInspectorGUI();
        }

        protected virtual void OnHiZInspectorGUI()
        {
            EditorGUILayout.LabelField(CoreEditorUtils.GetContent("HiZ Settings"));
            PropertyField(m_RayMinLevel, CoreEditorUtils.GetContent("Ray Min Level"));
            PropertyField(m_RayMaxLevel, CoreEditorUtils.GetContent("Ray Max Level"));
            PropertyField(m_RayMaxIterations, CoreEditorUtils.GetContent("Ray Max Iterations"));
            PropertyField(m_DepthBufferThickness, CoreEditorUtils.GetContent("Depth Buffer Thickness"));
            PropertyField(m_RayMaxScreenDistance, CoreEditorUtils.GetContent("Max Raymarched Distance (NDC)"));
            PropertyField(m_RayBlendScreenDistance, CoreEditorUtils.GetContent("Blended Raymarched Distance (NDC)"));
            PropertyField(m_RayMarchBehindObjects, CoreEditorUtils.GetContent("Ray March Behind Objects"));

            m_RayMinLevel.value.intValue = Mathf.Clamp(m_RayMinLevel.value.intValue, 0, m_RayMaxLevel.value.intValue);
            m_RayMaxLevel.value.intValue = Mathf.Max(0, m_RayMaxLevel.value.intValue);
            m_RayMaxIterations.value.intValue = Mathf.Max(0, m_RayMaxIterations.value.intValue);
            m_DepthBufferThickness.value.floatValue = Mathf.Clamp(m_DepthBufferThickness.value.floatValue, 0.001f, 1.0f);
            m_RayMaxScreenDistance.value.floatValue = Mathf.Clamp(m_RayMaxScreenDistance.value.floatValue, 0.001f, 1.0f);
            m_RayBlendScreenDistance.value.floatValue = Mathf.Clamp(m_RayBlendScreenDistance.value.floatValue, 0.0f, m_RayMaxScreenDistance.value.floatValue);
        }

        protected virtual void OnProxyInspectorGUI()
        {
        }

        protected virtual void OnCommonInspectorGUI()
        {
            EditorGUILayout.LabelField(CoreEditorUtils.GetContent("Common Settings"));
            PropertyField(m_ScreenWeightDistance, CoreEditorUtils.GetContent("Screen Weight Distance"));
        }
    }
}
