using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

using Object = UnityEngine.Object;
using UnityEditorInternal;
using System.Reflection;

[CustomEditor(typeof(VFXBlock), true)]
[CanEditMultipleObjects]
public class VFXBlockEditor : VFXSlotContainerEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (VFXViewPreference.displayExtraDebugInfo && !serializedObject.isEditingMultipleObjects)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Attributes", EditorStyles.boldLabel);

            VFXBlock block = serializedObject.targetObject as VFXBlock;

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(Contents.name, Styles.header);
                GUILayout.Label(Contents.type, Styles.header, GUILayout.Width(80));
                GUILayout.Label(Contents.mode, Styles.header, GUILayout.Width(80));
            }

            foreach (var attribute in block.attributes)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(attribute.attrib.name, Styles.cell);
                    Styles.DataTypeLabel(attribute.attrib.type.ToString(), attribute.attrib.type, Styles.cell, GUILayout.Width(80));
                    Styles.AttributeModeLabel(attribute.mode.ToString(), attribute.mode, Styles.cell, GUILayout.Width(80));
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(Contents.name, Styles.header);
                GUILayout.Label(Contents.type, Styles.header, GUILayout.Width(160));
            }

            foreach (var param in block.parameters)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(param.name, Styles.cell);
                    Styles.DataTypeLabel(param.exp.valueType.ToString(), param.exp.valueType, Styles.cell, GUILayout.Width(160));
                }
            }

            if (!string.IsNullOrEmpty(block.source))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Computed Source Code", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(block.source);
            }
        }
    }
}
