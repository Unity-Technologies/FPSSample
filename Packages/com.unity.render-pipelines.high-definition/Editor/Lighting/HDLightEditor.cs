using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Light), typeof(HDRenderPipelineAsset))]
    sealed partial class HDLightEditor : LightEditor
    {
        public SerializedHDLight m_SerializedHDLight;

        HDAdditionalLightData[] m_AdditionalLightDatas;
        AdditionalShadowData[] m_AdditionalShadowDatas;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Get & automatically add additional HD data if not present
            m_AdditionalLightDatas = CoreEditorUtils.GetAdditionalData<HDAdditionalLightData>(targets, HDAdditionalLightData.InitDefaultHDAdditionalLightData);
            m_AdditionalShadowDatas = CoreEditorUtils.GetAdditionalData<AdditionalShadowData>(targets, HDAdditionalShadowData.InitDefaultHDAdditionalShadowData);
            m_SerializedHDLight = new SerializedHDLight(m_AdditionalLightDatas, m_AdditionalShadowDatas, settings);

            // Update emissive mesh and light intensity when undo/redo
            Undo.undoRedoPerformed += () =>
            {
                m_SerializedHDLight.serializedLightDatas.ApplyModifiedProperties();
                foreach (var hdLightData in m_AdditionalLightDatas)
                    if (hdLightData != null)
                        hdLightData.UpdateAreaLightEmissiveMesh();
            };

            // If the light is disabled in the editor we force the light upgrade from his inspector
            foreach (var additionalLightData in m_AdditionalLightDatas)
                additionalLightData.UpgradeLight();
        }

        public override void OnInspectorGUI()
        {
            m_SerializedHDLight.Update();

            //add space before the first collapsible area
            EditorGUILayout.Space();

            // Disable the default light editor for the release, it is just use for development
            /*
            // Temporary toggle to go back to the old editor & separated additional datas
            bool useOldInspector = m_AdditionalLightData.useOldInspector.boolValue;

            if (GUILayout.Button("Toggle default light editor"))
                useOldInspector = !useOldInspector;

            m_AdditionalLightData.useOldInspector.boolValue = useOldInspector;

            if (useOldInspector)
            {
                DrawDefaultInspector();
                ApplyAdditionalComponentsVisibility(false);
                m_SerializedAdditionalShadowData.ApplyModifiedProperties();
                m_SerializedAdditionalLightData.ApplyModifiedProperties();
                return;
            }
            */

            // New editor
            ApplyAdditionalComponentsVisibility(true);

            HDLightUI.Inspector.Draw(m_SerializedHDLight, this);

            m_SerializedHDLight.Apply();

            if (m_SerializedHDLight.needUpdateAreaLightEmissiveMeshComponents)
                UpdateAreaLightEmissiveMeshComponents();
        }

        void UpdateAreaLightEmissiveMeshComponents()
        {
            foreach (var hdLightData in m_AdditionalLightDatas)
            {
                hdLightData.UpdateAreaLightEmissiveMesh();

                MeshRenderer emissiveMeshRenderer = hdLightData.GetComponent<MeshRenderer>();
                MeshFilter emissiveMeshFilter = hdLightData.GetComponent<MeshFilter>();

                // If the display emissive mesh is disabled, skip to the next selected light
                if (emissiveMeshFilter == null || emissiveMeshRenderer == null)
                    continue;

                // We only load the mesh and it's material here, because we can't do that inside HDAdditionalLightData (Editor assembly)
                // Every other properties of the mesh is updated in HDAdditionalLightData to support timeline and editor records
                emissiveMeshFilter.mesh = HDEditorUtils.LoadAsset<Mesh>("Runtime/RenderPipelineResources/Mesh/Quad.FBX");
                if (emissiveMeshRenderer.sharedMaterial == null)
                    emissiveMeshRenderer.material = new Material(Shader.Find("HDRenderPipeline/Unlit"));
            }

            m_SerializedHDLight.needUpdateAreaLightEmissiveMeshComponents = false;
        }

        // Internal utilities
        void ApplyAdditionalComponentsVisibility(bool hide)
        {
            // UX team decided that we should always show component in inspector.
            // However already authored scene save this settings, so force the component to be visible
            // var flags = hide ? HideFlags.HideInInspector : HideFlags.None;
            var flags = HideFlags.None;

            foreach (var t in m_SerializedHDLight.serializedLightDatas.targetObjects)
                ((HDAdditionalLightData)t).hideFlags = flags;

            foreach (var t in m_SerializedHDLight.serializedShadowDatas.targetObjects)
                ((AdditionalShadowData)t).hideFlags = flags;
        }
        
        protected override void OnSceneGUI()
        {
            m_SerializedHDLight.Update();


            HDAdditionalLightData src = (HDAdditionalLightData)m_SerializedHDLight.serializedLightDatas.targetObject;
            Light light = (Light)target;
            if (src.lightTypeExtent == LightTypeExtent.Punctual && (light.type == LightType.Directional || light.type == LightType.Point))
            {
                //use legacy handles
                base.OnSceneGUI();
                return;
            }

            HDLightUI.DrawHandles(m_SerializedHDLight, this);
        }

        internal Color legacyLightColor
        {
            get
            {
                Light light = (Light)target;
                return light.enabled ? LightEditor.kGizmoLight : LightEditor.kGizmoDisabledLight;
            }
        }
    }
}
