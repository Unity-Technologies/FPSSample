using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using UnityEditorInternal;

namespace UnityEngine.Experimental.Rendering.UI
{
    [CustomEditor(typeof(DebugUIHandlerCanvas))]
    public sealed class DebugUIHandlerCanvasEditor : Editor
    {
        SerializedProperty m_PanelPrefab;
        SerializedProperty m_Prefabs;
        ReorderableList m_PrefabList;

        static string[] s_Types; // Assembly qualified names
        static string[] s_DisplayTypes; // Pretty names

        static DebugUIHandlerCanvasEditor()
        {
            s_Types = CoreUtils.GetAllAssemblyTypes()
                .Where(t => t.IsSubclassOf(typeof(DebugUI.Widget)) && !t.IsAbstract)
                .Select(t => t.AssemblyQualifiedName)
                .ToArray();

            s_DisplayTypes = new string[s_Types.Length];
            for (int i = 0; i < s_Types.Length; i++)
                s_DisplayTypes[i] = Type.GetType(s_Types[i]).Name;
        }

        void OnEnable()
        {
            var o = new PropertyFetcher<DebugUIHandlerCanvas>(serializedObject);
            m_PanelPrefab = o.Find(x => x.panelPrefab);
            m_Prefabs = o.Find(x => x.prefabs);

            m_PrefabList = new ReorderableList(serializedObject, m_Prefabs, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Widget Prefabs"),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var element = m_PrefabList.serializedProperty.GetArrayElementAtIndex(index);
                        rect.y += 2f;
                        const float kTypeWidth = 100f;

                        // Type selector
                        var typeProp = element.FindPropertyRelative("type");
                        int typeIndex = ArrayUtility.IndexOf(s_Types, typeProp.stringValue);
                        typeIndex = Mathf.Max(typeIndex, 0);
                        typeIndex = EditorGUI.Popup(new Rect(rect.x, rect.y, kTypeWidth, EditorGUIUtility.singleLineHeight), typeIndex, s_DisplayTypes);
                        typeProp.stringValue = s_Types[typeIndex];

                        // Prefab
                        EditorGUI.PropertyField(
                            new Rect(rect.x + kTypeWidth + 2f, rect.y, rect.width - kTypeWidth - 2f, EditorGUIUtility.singleLineHeight),
                            element.FindPropertyRelative("prefab"), GUIContent.none);
                    },
                onSelectCallback = list =>
                    {
                        var prefab = list.serializedProperty.GetArrayElementAtIndex(list.index).FindPropertyRelative("prefab").objectReferenceValue as GameObject;
                        if (prefab)
                            EditorGUIUtility.PingObject(prefab.gameObject);
                    }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_PanelPrefab);
            EditorGUILayout.Space();
            m_PrefabList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
