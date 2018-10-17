using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Object = UnityEngine.Object;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI
    {
        public static void DrawHandles_EditBase(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, Matrix4x4 matrix, Object sourceAsset)
        {
            using (new Handles.DrawingScope(k_GizmoThemeColorBase, matrix))
            {
                switch ((InfluenceShape)d.shape.intValue)
                {
                    case InfluenceShape.Box:
                        DrawBoxHandle(s, d, o, sourceAsset, s.boxBaseHandle);
                        break;
                    case InfluenceShape.Sphere:
                        DrawSphereHandle(s, d, o, sourceAsset, s.sphereBaseHandle);
                        break;
                }
            }
        }

        public static void DrawHandles_EditInfluence(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, Matrix4x4 matrix, Object sourceAsset)
        {
            using (new Handles.DrawingScope(k_GizmoThemeColorInfluence, matrix))
            {
                switch ((InfluenceShape)d.shape.intValue)
                {
                    case InfluenceShape.Box:
                        EditorGUI.BeginChangeCheck();
                        DrawBoxFadeHandle(s, d, o, sourceAsset, s.boxInfluenceHandle, d.boxBlendDistancePositive, d.boxBlendDistanceNegative);
                        if (EditorGUI.EndChangeCheck())
                        {
                            //save advanced/simplified saved data
                            if (d.editorAdvancedModeEnabled.boolValue)
                            {
                                d.editorAdvancedModeBlendDistancePositive.vector3Value = d.boxBlendDistancePositive.vector3Value;
                                d.editorAdvancedModeBlendDistanceNegative.vector3Value = d.boxBlendDistanceNegative.vector3Value;
                            }
                            else
                            {
                                d.editorSimplifiedModeBlendDistance.floatValue = d.boxBlendDistancePositive.vector3Value.x;
                            }
                            d.Apply();
                        }
                        break;
                    case InfluenceShape.Sphere:
                        DrawSphereFadeHandle(s, d, o, sourceAsset, s.sphereInfluenceHandle, d.sphereBlendDistance);
                        break;
                }
            }
        }

        public static void DrawHandles_EditInfluenceNormal(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, Matrix4x4 matrix, Object sourceAsset)
        {
            using (new Handles.DrawingScope(k_GizmoThemeColorInfluenceNormal, matrix))
            {
                switch ((InfluenceShape)d.shape.intValue)
                {
                    case InfluenceShape.Box:
                        EditorGUI.BeginChangeCheck();
                        DrawBoxFadeHandle(s, d, o, sourceAsset, s.boxInfluenceNormalHandle, d.boxBlendNormalDistancePositive, d.boxBlendNormalDistanceNegative);
                        if (EditorGUI.EndChangeCheck())
                        {
                            //save advanced/simplified saved data
                            if (d.editorAdvancedModeEnabled.boolValue)
                            {
                                d.editorAdvancedModeBlendNormalDistancePositive.vector3Value = d.boxBlendNormalDistancePositive.vector3Value;
                                d.editorAdvancedModeBlendNormalDistanceNegative.vector3Value = d.boxBlendNormalDistanceNegative.vector3Value;
                            }
                            else
                            {
                                d.editorSimplifiedModeBlendNormalDistance.floatValue = d.boxBlendNormalDistancePositive.vector3Value.x;
                            }
                            d.Apply();
                        }
                        break;
                    case InfluenceShape.Sphere:
                        DrawSphereFadeHandle(s, d, o, sourceAsset, s.sphereInfluenceNormalHandle, d.sphereBlendNormalDistance);
                        break;
                }
            }
        }

        static void DrawBoxHandle(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, Object sourceAsset, Gizmo6FacesBox box)
        {
            box.center = d.offset.vector3Value;
            box.size = d.boxSize.vector3Value;

            EditorGUI.BeginChangeCheck();
            box.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Base Volume AABB");

                d.offset.vector3Value = box.center;
                var size = box.size;
                
                Vector3 blendPositive = d.boxBlendDistancePositive.vector3Value;
                Vector3 blendNegative = d.boxBlendDistanceNegative.vector3Value;
                Vector3 blendNormalPositive = d.boxBlendNormalDistancePositive.vector3Value;
                Vector3 blendNormalNegative = d.boxBlendNormalDistanceNegative.vector3Value;
                for (int i = 0; i < 3; ++i)
                {
                    size[i] = Mathf.Max(0f, size[i]);
                }
                d.boxSize.vector3Value = size;
                Vector3 halfSize = size * .5f;
                for (int i = 0; i < 3; ++i)
                {
                    blendPositive[i] = Mathf.Clamp(blendPositive[i], 0f, halfSize[i]);
                    blendNegative[i] = Mathf.Clamp(blendNegative[i], 0f, halfSize[i]);
                    blendNormalPositive[i] = Mathf.Clamp(blendNormalPositive[i], 0f, halfSize[i]);
                    blendNormalNegative[i] = Mathf.Clamp(blendNormalNegative[i], 0f, halfSize[i]);
                }
                d.boxBlendDistancePositive.vector3Value = blendPositive;
                d.boxBlendDistanceNegative.vector3Value = blendNegative;
                d.boxBlendNormalDistancePositive.vector3Value = blendNormalPositive;
                d.boxBlendNormalDistanceNegative.vector3Value = blendNormalNegative;

                if (d.editorAdvancedModeEnabled.boolValue)
                {
                    d.editorAdvancedModeBlendDistancePositive.vector3Value = d.boxBlendDistancePositive.vector3Value;
                    d.editorAdvancedModeBlendDistanceNegative.vector3Value = d.boxBlendDistanceNegative.vector3Value;
                    d.editorAdvancedModeBlendNormalDistancePositive.vector3Value = d.boxBlendNormalDistancePositive.vector3Value;
                    d.editorAdvancedModeBlendNormalDistanceNegative.vector3Value = d.boxBlendNormalDistanceNegative.vector3Value;
                }
                else
                {
                    d.editorSimplifiedModeBlendDistance.floatValue = Mathf.Min(
                        d.boxBlendDistancePositive.vector3Value.x,
                        d.boxBlendDistancePositive.vector3Value.y,
                        d.boxBlendDistancePositive.vector3Value.z,
                        d.boxBlendDistanceNegative.vector3Value.x,
                        d.boxBlendDistanceNegative.vector3Value.y,
                        d.boxBlendDistanceNegative.vector3Value.z);
                    d.boxBlendDistancePositive.vector3Value = d.boxBlendDistanceNegative.vector3Value = Vector3.one * d.editorSimplifiedModeBlendDistance.floatValue;
                    d.editorSimplifiedModeBlendNormalDistance.floatValue = Mathf.Min(
                        d.boxBlendNormalDistancePositive.vector3Value.x,
                        d.boxBlendNormalDistancePositive.vector3Value.y,
                        d.boxBlendNormalDistancePositive.vector3Value.z,
                        d.boxBlendNormalDistanceNegative.vector3Value.x,
                        d.boxBlendNormalDistanceNegative.vector3Value.y,
                        d.boxBlendNormalDistanceNegative.vector3Value.z);
                    d.boxBlendNormalDistancePositive.vector3Value = d.boxBlendNormalDistanceNegative.vector3Value = Vector3.one * d.editorSimplifiedModeBlendNormalDistance.floatValue;
                }

                d.Apply();
            }
        }

        static void DrawBoxFadeHandle(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, Object sourceAsset, Gizmo6FacesBox box, SerializedProperty positive, SerializedProperty negative)
        {
            box.center = d.offset.vector3Value - (positive.vector3Value - negative.vector3Value) * 0.5f;
            box.size = d.boxSize.vector3Value - positive.vector3Value - negative.vector3Value;
            box.allHandleControledByOne = !d.editorAdvancedModeEnabled.boolValue;

            EditorGUI.BeginChangeCheck();
            box.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Influence Volume");

                var influenceCenter = d.offset.vector3Value;
                var halfInfluenceSize = d.boxSize.vector3Value * .5f;

                var centerDiff = box.center - influenceCenter;
                var halfSizeDiff = halfInfluenceSize - box.size * .5f;
                var positiveNew = halfSizeDiff - centerDiff;
                var negativeNew = halfSizeDiff + centerDiff;
                var blendDistancePositive = Vector3.Max(Vector3.zero, Vector3.Min(positiveNew, halfInfluenceSize));
                var blendDistanceNegative = Vector3.Max(Vector3.zero, Vector3.Min(negativeNew, halfInfluenceSize));

                positive.vector3Value = blendDistancePositive;
                negative.vector3Value = blendDistanceNegative;

                d.Apply();
            }
        }

        static void DrawSphereHandle(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, Object sourceAsset,  SphereBoundsHandle sphere)
        {
            sphere.center = d.offset.vector3Value;
            sphere.radius = d.sphereRadius.floatValue;

            EditorGUI.BeginChangeCheck();
            sphere.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Base Volume AABB");

                float radius = sphere.radius;
                d.sphereRadius.floatValue = radius;
                d.sphereBlendDistance.floatValue = Mathf.Clamp(s.data.sphereBlendDistance.floatValue, 0, radius);
                d.sphereBlendNormalDistance.floatValue = Mathf.Clamp(s.data.sphereBlendNormalDistance.floatValue, 0, radius);
                d.Apply();
            }
        }

        static void DrawSphereFadeHandle(InfluenceVolumeUI s, SerializedInfluenceVolume d, Editor o, Object sourceAsset, SphereBoundsHandle sphere, SerializedProperty radius)
        {
            sphere.center = d.offset.vector3Value;
            sphere.radius = radius.floatValue;

            EditorGUI.BeginChangeCheck();
            sphere.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Influence volume");

                radius.floatValue = Mathf.Clamp(d.sphereRadius.floatValue - sphere.radius, 0, d.sphereRadius.floatValue);
                d.Apply();
            }
        }
    }
}
