using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class SerializedInfluenceVolume
    {
        internal SerializedProperty root;

        internal SerializedProperty shape;
        internal SerializedProperty offset;
        internal SerializedProperty boxSize;
        internal SerializedProperty boxBlendDistancePositive;
        internal SerializedProperty boxBlendDistanceNegative;
        internal SerializedProperty boxBlendNormalDistancePositive;
        internal SerializedProperty boxBlendNormalDistanceNegative;
        internal SerializedProperty boxSideFadePositive;
        internal SerializedProperty boxSideFadeNegative;
        internal SerializedProperty sphereRadius;
        internal SerializedProperty sphereBlendDistance;
        internal SerializedProperty sphereBlendNormalDistance;

        internal SerializedProperty editorAdvancedModeBlendDistancePositive;
        internal SerializedProperty editorAdvancedModeBlendDistanceNegative;
        internal SerializedProperty editorSimplifiedModeBlendDistance;
        internal SerializedProperty editorAdvancedModeBlendNormalDistancePositive;
        internal SerializedProperty editorAdvancedModeBlendNormalDistanceNegative;
        internal SerializedProperty editorSimplifiedModeBlendNormalDistance;
        internal SerializedProperty editorAdvancedModeEnabled;
        internal SerializedProperty editorAdvancedModeFaceFadePositive;
        internal SerializedProperty editorAdvancedModeFaceFadeNegative;

        public SerializedInfluenceVolume(SerializedProperty root)
        {
            this.root = root;

            shape = root.Find((InfluenceVolume i) => i.shape);
            offset = root.Find((InfluenceVolume i) => i.offset);
            boxSize = root.Find((InfluenceVolume i) => i.boxSize);
            boxBlendDistancePositive = root.Find((InfluenceVolume i) => i.boxBlendDistancePositive);
            boxBlendDistanceNegative = root.Find((InfluenceVolume i) => i.boxBlendDistanceNegative);
            boxBlendNormalDistancePositive = root.Find((InfluenceVolume i) => i.boxBlendNormalDistancePositive);
            boxBlendNormalDistanceNegative = root.Find((InfluenceVolume i) => i.boxBlendNormalDistanceNegative);
            boxSideFadePositive = root.Find((InfluenceVolume i) => i.boxSideFadePositive);
            boxSideFadeNegative = root.Find((InfluenceVolume i) => i.boxSideFadeNegative);
            sphereRadius = root.Find((InfluenceVolume i) => i.sphereRadius);
            sphereBlendDistance = root.Find((InfluenceVolume i) => i.sphereBlendDistance);
            sphereBlendNormalDistance = root.Find((InfluenceVolume i) => i.sphereBlendNormalDistance);

            editorAdvancedModeBlendDistancePositive = root.FindPropertyRelative("m_EditorAdvancedModeBlendDistancePositive");
            editorAdvancedModeBlendDistanceNegative = root.FindPropertyRelative("m_EditorAdvancedModeBlendDistanceNegative");
            editorSimplifiedModeBlendDistance = root.FindPropertyRelative("m_EditorSimplifiedModeBlendDistance");
            editorAdvancedModeBlendNormalDistancePositive = root.FindPropertyRelative("m_EditorAdvancedModeBlendNormalDistancePositive");
            editorAdvancedModeBlendNormalDistanceNegative = root.FindPropertyRelative("m_EditorAdvancedModeBlendNormalDistanceNegative");
            editorSimplifiedModeBlendNormalDistance = root.FindPropertyRelative("m_EditorSimplifiedModeBlendNormalDistance");
            editorAdvancedModeEnabled = root.FindPropertyRelative("m_EditorAdvancedModeEnabled");
            editorAdvancedModeFaceFadePositive = root.FindPropertyRelative("m_EditorAdvancedModeFaceFadePositive");
            editorAdvancedModeFaceFadeNegative = root.FindPropertyRelative("m_EditorAdvancedModeFaceFadeNegative");

            //handle data migration from before editor value were saved
            if (editorAdvancedModeBlendDistancePositive.vector3Value == Vector3.zero
                && editorAdvancedModeBlendDistanceNegative.vector3Value == Vector3.zero
                && editorSimplifiedModeBlendDistance.floatValue == 0f
                && editorAdvancedModeBlendNormalDistancePositive.vector3Value == Vector3.zero
                && editorAdvancedModeBlendNormalDistanceNegative.vector3Value == Vector3.zero
                && editorSimplifiedModeBlendNormalDistance.floatValue == 0f
                && (boxBlendDistancePositive.vector3Value != Vector3.zero
                    || boxBlendDistanceNegative.vector3Value != Vector3.zero
                    || boxBlendNormalDistancePositive.vector3Value != Vector3.zero
                    || boxBlendNormalDistanceNegative.vector3Value != Vector3.zero))
            {
                Vector3 positive = boxBlendDistancePositive.vector3Value;
                Vector3 negative = boxBlendDistanceNegative.vector3Value;
                //exact advanced
                editorAdvancedModeBlendDistancePositive.vector3Value = positive;
                editorAdvancedModeBlendDistanceNegative.vector3Value = negative;
                //approximated simplified, exact if it was simplified
                editorSimplifiedModeBlendDistance.floatValue = Mathf.Max(positive.x, positive.y, positive.z, negative.x, negative.y, negative.z);

                positive = boxBlendNormalDistancePositive.vector3Value;
                negative = boxBlendNormalDistanceNegative.vector3Value;
                //exact advanced
                editorAdvancedModeBlendNormalDistancePositive.vector3Value = positive;
                editorAdvancedModeBlendNormalDistanceNegative.vector3Value = negative;
                //approximated simplified, exact if it was simplified
                editorSimplifiedModeBlendNormalDistance.floatValue = Mathf.Max(positive.x, positive.y, positive.z, negative.x, negative.y, negative.z);

                //display old data
                editorAdvancedModeEnabled.boolValue =
                       positive.x != positive.y
                    || positive.x != positive.z
                    || negative.x != negative.y
                    || negative.x != negative.z
                    || positive.x != negative.x;
                Apply();
            }
            if(editorAdvancedModeFaceFadePositive.vector3Value == Vector3.one
                && editorAdvancedModeFaceFadeNegative.vector3Value == Vector3.one
                && (boxSideFadePositive.vector3Value != Vector3.one
                    || boxSideFadeNegative.vector3Value != Vector3.one))
            {
                editorAdvancedModeFaceFadePositive.vector3Value = boxSideFadePositive.vector3Value;
                editorAdvancedModeFaceFadeNegative.vector3Value = boxSideFadeNegative.vector3Value;
                if(!editorAdvancedModeEnabled.boolValue)
                {
                    boxSideFadePositive.vector3Value = Vector3.one;
                    boxSideFadeNegative.vector3Value = Vector3.one;
                }
                Apply();
            }
        }

        public void Apply()
        {
            root.serializedObject.ApplyModifiedProperties();
        }
    }
}
