using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(DecalProjectorComponent))]
    [CanEditMultipleObjects]
    public class DecalProjectorComponentEditor : Editor
    {
        private MaterialEditor m_MaterialEditor = null;
        private DecalProjectorComponent m_DecalProjectorComponent = null;
        private SerializedProperty m_MaterialProperty;
        private SerializedProperty m_DrawDistanceProperty;
        private SerializedProperty m_FadeScaleProperty;
        private SerializedProperty m_UVScaleProperty;
        private SerializedProperty m_UVBiasProperty;
        private SerializedProperty m_AffectsTransparencyProperty;
        private SerializedProperty m_Size;
        private SerializedProperty m_IsCropModeEnabledProperty;

        private DecalProjectorComponentHandle m_Handle = new DecalProjectorComponentHandle();

        private void OnEnable()
        {
            // Create an instance of the MaterialEditor
            m_DecalProjectorComponent = (DecalProjectorComponent)target;
            m_MaterialEditor = (MaterialEditor)CreateEditor(m_DecalProjectorComponent.Mat);
            m_DecalProjectorComponent.OnMaterialChange += OnMaterialChange;
            m_MaterialProperty = serializedObject.FindProperty("m_Material");
            m_DrawDistanceProperty = serializedObject.FindProperty("m_DrawDistance");
            m_FadeScaleProperty = serializedObject.FindProperty("m_FadeScale");
            m_UVScaleProperty = serializedObject.FindProperty("m_UVScale");
            m_UVBiasProperty = serializedObject.FindProperty("m_UVBias");
            m_AffectsTransparencyProperty = serializedObject.FindProperty("m_AffectsTransparency");
            m_Size = serializedObject.FindProperty("m_Size");
            m_IsCropModeEnabledProperty = serializedObject.FindProperty("m_IsCropModeEnabled");
        }

        private void OnDisable()
        {
            m_DecalProjectorComponent.OnMaterialChange -= OnMaterialChange;
        }

        private void OnDestroy()
        {
            DestroyImmediate(m_MaterialEditor);
        }

        public void OnMaterialChange()
        {
            // Update material editor with the new material
            m_MaterialEditor = (MaterialEditor)CreateEditor(m_DecalProjectorComponent.Mat);
        }

        void OnSceneGUI()
        {
            var mat = Handles.matrix;
            var col = Handles.color;

            Handles.color = Color.white;
            Handles.matrix = m_DecalProjectorComponent.transform.localToWorldMatrix;
            m_Handle.center = m_DecalProjectorComponent.m_Offset;
            m_Handle.size = m_DecalProjectorComponent.m_Size;

            Vector3 boundsSizePreviousOS = m_Handle.size;
            Vector3 boundsMinPreviousOS = m_Handle.size * -0.5f + m_Handle.center;

            EditorGUI.BeginChangeCheck();
            m_Handle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                // Adjust decal transform if handle changed.
                Undo.RecordObject(m_DecalProjectorComponent, "Decal Projector Change");

                m_DecalProjectorComponent.m_Size = m_Handle.size;
                m_DecalProjectorComponent.m_Offset = m_Handle.center;

                Vector3 boundsSizeCurrentOS = m_Handle.size;
                Vector3 boundsMinCurrentOS = m_Handle.size * -0.5f + m_Handle.center;

                if (m_DecalProjectorComponent.m_IsCropModeEnabled)
                {
                    // Treat decal projector bounds as a crop tool, rather than a scale tool.
                    // Compute a new uv scale and bias terms to pin decal projection pixels in world space, irrespective of projector bounds.
                    m_DecalProjectorComponent.m_UVScale.x *= Mathf.Max(1e-5f, boundsSizeCurrentOS.x) / Mathf.Max(1e-5f, boundsSizePreviousOS.x);
                    m_DecalProjectorComponent.m_UVScale.y *= Mathf.Max(1e-5f, boundsSizeCurrentOS.z) / Mathf.Max(1e-5f, boundsSizePreviousOS.z);

                    m_DecalProjectorComponent.m_UVBias.x += (boundsMinCurrentOS.x - boundsMinPreviousOS.x) / Mathf.Max(1e-5f, boundsSizeCurrentOS.x) * m_DecalProjectorComponent.m_UVScale.x;
                    m_DecalProjectorComponent.m_UVBias.y += (boundsMinCurrentOS.z - boundsMinPreviousOS.z) / Mathf.Max(1e-5f, boundsSizeCurrentOS.z) * m_DecalProjectorComponent.m_UVScale.y;
                }
            }

            // Automatically recenter our transform component if necessary.
            // In order to correctly handle world-space snapping, we only perform this recentering when the user is no longer interacting with the gizmo.
            if ((GUIUtility.hotControl == 0) && (m_DecalProjectorComponent.m_Offset != Vector3.zero))
            {
                // Both the DecalProjectorComponent, and the transform will be modified.
                // The undo system will automatically group all RecordObject() calls here into a single action.
                Undo.RecordObject(m_DecalProjectorComponent.transform, "Decal Projector Change");

                // Re-center the transform to the center of the decal projector bounds,
                // while maintaining the world-space coordinates of the decal projector boundings vertices.
                m_DecalProjectorComponent.transform.Translate(
                    Vector3.Scale(m_DecalProjectorComponent.m_Offset, m_DecalProjectorComponent.transform.localScale),
                    Space.Self
                );

                m_DecalProjectorComponent.m_Offset = Vector3.zero;
            }

            Handles.matrix = mat;
            Handles.color = col;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_IsCropModeEnabledProperty, new GUIContent("Crop Decal with Gizmo"));

            EditorGUILayout.PropertyField(m_Size);
            EditorGUILayout.PropertyField(m_MaterialProperty);
            EditorGUILayout.PropertyField(m_DrawDistanceProperty);
            EditorGUILayout.Slider(m_FadeScaleProperty, 0.0f, 1.0f, new GUIContent("Fade scale"));
            EditorGUILayout.PropertyField(m_UVScaleProperty);
            EditorGUILayout.PropertyField(m_UVBiasProperty);
            EditorGUILayout.PropertyField(m_AffectsTransparencyProperty);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (m_MaterialEditor != null)
            {
                // Draw the material's foldout and the material shader field
                // Required to call m_MaterialEditor.OnInspectorGUI ();
                m_MaterialEditor.DrawHeader();

                // We need to prevent the user to edit default decal materials
                bool isDefaultMaterial = false;
                var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                if (hdrp != null)
                {
                    isDefaultMaterial = m_DecalProjectorComponent.Mat == hdrp.GetDefaultDecalMaterial();
                }
                using (new EditorGUI.DisabledGroupScope(isDefaultMaterial))
                {
                    // Draw the material properties
                    // Works only if the foldout of m_MaterialEditor.DrawHeader () is open
                    m_MaterialEditor.OnInspectorGUI();
                }
            }
        }
    }
}
