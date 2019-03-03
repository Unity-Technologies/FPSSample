using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Experimental.VFX;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.VFX;
using System.Collections.Generic;

namespace UnityEditor.VFX.Utils
{
    [CustomEditor(typeof(VisualEffectActivationClip))]
    public class VisualEffectActivationClipEditor : Editor
    {
        private SerializedProperty onClipEnterProperty;
        private SerializedProperty onClipExitProperty;

        private ReorderableList clipEnterAttributesPropertyList;
        private ReorderableList clipExitAttributesPropertyList;

        private void OnEnable()
        {
            Action<ReorderableList, SerializedProperty> fnAssetDropDown = delegate(ReorderableList list, SerializedProperty property)
            {
                var existingAttribute = new List<string>();
                for (int i = 0; i < property.arraySize; ++i)
                {
                    existingAttribute.Add(property.GetArrayElementAtIndex(i).FindPropertyRelative("attribute.m_Name").stringValue);
                }

                var menu = new GenericMenu();
                foreach (var attributeName in VFXAttribute.AllIncludingVariadicReadWritable.Except(existingAttribute).OrderBy(o => o))
                {
                    var attribute = VFXAttribute.Find(attributeName);
                    menu.AddItem(new GUIContent(attribute.name), false, () =>
                    {
                        serializedObject.Update();
                        property.arraySize++;

                        var newElement = property.GetArrayElementAtIndex(property.arraySize - 1);
                        newElement.FindPropertyRelative("attribute.m_Name").stringValue = attribute.name;
                        newElement.FindPropertyRelative("type").intValue = (int)attribute.type;

                        var size = VFXExpression.TypeToSize(attribute.type);
                        var values = newElement.FindPropertyRelative("values");
                        values.arraySize = size;

                        var initialValues = new float[size];
                        if (attribute.type == VFXValueType.Float)
                        {
                            initialValues[0] = attribute.value.Get<float>();
                        }
                        else if (attribute.type == VFXValueType.Float2)
                        {
                            var v = attribute.value.Get<Vector2>();
                            initialValues[0] = v.x;
                            initialValues[1] = v.y;
                        }
                        else if (attribute.type == VFXValueType.Float3)
                        {
                            var v = attribute.value.Get<Vector3>();
                            initialValues[0] = v.x;
                            initialValues[1] = v.y;
                            initialValues[2] = v.z;
                        }
                        else if (attribute.type == VFXValueType.Float4)
                        {
                            var v = attribute.value.Get<Vector4>();
                            initialValues[0] = v.x;
                            initialValues[1] = v.y;
                            initialValues[2] = v.z;
                            initialValues[3] = v.w;
                        }
                        else if (attribute.type == VFXValueType.Int32)
                        {
                            initialValues[0] = attribute.value.Get<int>();
                        }
                        else if (attribute.type == VFXValueType.Uint32)
                        {
                            initialValues[0] = attribute.value.Get<uint>();
                        }
                        else if (attribute.type == VFXValueType.Boolean)
                        {
                            initialValues[0] = attribute.value.Get<bool>() ? 1.0f : 0.0f;
                        }
                        for (int i = 0; i < size; ++i)
                        {
                            values.GetArrayElementAtIndex(i).floatValue = initialValues[i];
                        }
                        serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.ShowAsContext();
            };

            Action<Rect, SerializedProperty, int> fnDrawElement = delegate(Rect r, SerializedProperty property, int index)
            {
                var element = property.GetArrayElementAtIndex(index);

                var label = element.FindPropertyRelative("attribute.m_Name").stringValue;
                var labelWidth = 110;//GUI.skin.label.CalcSize(new GUIContent(label)); //Should be maximized among all existing property, for now, angularVelocity is considered as maximum

                EditorGUI.LabelField(new Rect(r.x, r.y, labelWidth, EditorGUIUtility.singleLineHeight), label);
                var valueType = (VFXValueType)element.FindPropertyRelative("type").intValue;
                var valueSize = VFXExpression.TypeToSize(valueType);
                var fieldWidth = (r.width - labelWidth) / valueSize;
                var emptyGUIContent = new GUIContent(string.Empty);
                var valuesProperty = element.FindPropertyRelative("values");
                if (valueType == VFXValueType.Float
                    || valueType == VFXValueType.Float2
                    || valueType == VFXValueType.Float3
                    || valueType == VFXValueType.Float4)
                {
                    if (label.Contains("color") && valueType == VFXValueType.Float3)
                    {
                        var oldColor = new Color(valuesProperty.GetArrayElementAtIndex(0).floatValue,
                            valuesProperty.GetArrayElementAtIndex(1).floatValue,
                            valuesProperty.GetArrayElementAtIndex(2).floatValue);

                        EditorGUI.BeginChangeCheck();
                        var newColor = EditorGUI.ColorField(new Rect(r.x + labelWidth, r.y, fieldWidth * 3, EditorGUIUtility.singleLineHeight), oldColor);
                        if (EditorGUI.EndChangeCheck())
                        {
                            valuesProperty.GetArrayElementAtIndex(0).floatValue = newColor.r;
                            valuesProperty.GetArrayElementAtIndex(1).floatValue = newColor.g;
                            valuesProperty.GetArrayElementAtIndex(2).floatValue = newColor.b;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < valueSize; ++i)
                        {
                            EditorGUI.PropertyField(new Rect(r.x + labelWidth + fieldWidth * i, r.y, fieldWidth, EditorGUIUtility.singleLineHeight), valuesProperty.GetArrayElementAtIndex(i), emptyGUIContent);
                        }
                    }
                }
                else if (valueType == VFXValueType.Int32
                         || valueType == VFXValueType.Uint32
                         || valueType == VFXValueType.Boolean)
                {
                    var oldValue = valuesProperty.GetArrayElementAtIndex(0).floatValue;
                    float newValue;
                    var currentRect = new Rect(r.x + labelWidth, r.y, fieldWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.BeginChangeCheck();
                    if (valueType == VFXValueType.Boolean)
                    {
                        newValue = EditorGUI.Toggle(currentRect, emptyGUIContent, oldValue != 0.0f) ? 1.0f : 0.0f;
                    }
                    else
                    {
                        newValue = (float)EditorGUI.LongField(currentRect, emptyGUIContent, (long)oldValue);
                        newValue = newValue < 0.0f ? 0.0f : newValue;
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        valuesProperty.GetArrayElementAtIndex(0).floatValue = newValue;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            };

            onClipEnterProperty = serializedObject.FindProperty("activationBehavior.onClipEnter.m_Name");
            onClipExitProperty = serializedObject.FindProperty("activationBehavior.onClipExit.m_Name");

            var clipEnterAttributesProperty = serializedObject.FindProperty("activationBehavior.clipEnterEventAttributes");
            var clipExitAttributesProperty = serializedObject.FindProperty("activationBehavior.clipExitEventAttributes");

            clipEnterAttributesPropertyList = new ReorderableList(serializedObject, clipEnterAttributesProperty, true, true, true, true);
            clipExitAttributesPropertyList = new ReorderableList(serializedObject, clipExitAttributesProperty, true, true, true, true);

            clipEnterAttributesPropertyList.drawHeaderCallback = (Rect r) => { EditorGUI.LabelField(r, "Enter Event Attributes"); };
            clipExitAttributesPropertyList.drawHeaderCallback = (Rect r) => { EditorGUI.LabelField(r, "Exit Event Attributes"); };

            clipEnterAttributesPropertyList.onAddDropdownCallback += (Rect buttonRect, ReorderableList list) => fnAssetDropDown(list, clipEnterAttributesProperty);
            clipExitAttributesPropertyList.onAddDropdownCallback += (Rect buttonRect, ReorderableList list) => fnAssetDropDown(list, clipExitAttributesProperty);

            clipEnterAttributesPropertyList.drawElementCallback = (Rect r, int index, bool active, bool focused) => fnDrawElement(r, clipEnterAttributesProperty, index);
            clipExitAttributesPropertyList.drawElementCallback = (Rect r, int index, bool active, bool focused) => fnDrawElement(r, clipExitAttributesProperty, index);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (serializedObject.isEditingMultipleObjects)
                return; //TODO

            EditorGUILayout.PropertyField(onClipEnterProperty);
            clipEnterAttributesPropertyList.DoLayoutList();

            EditorGUILayout.PropertyField(onClipExitProperty);
            clipExitAttributesPropertyList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
