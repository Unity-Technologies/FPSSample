using UnityEngine.Experimental.Rendering.UI;

namespace UnityEditor.Experimental.Rendering.UI
{
    [CustomEditor(typeof(UIFoldout), true)]
    sealed class UIFoldoutEditor : Editor
    {
        SerializedProperty m_IsOn;
        SerializedProperty m_Content;
        SerializedProperty m_ArrowClosed;
        SerializedProperty m_ArrowOpened;

        void OnEnable()
        {
            var o = new PropertyFetcher<UIFoldout>(serializedObject);
            m_IsOn = o.Find("m_IsOn");
            m_Content = o.Find(x => x.content);
            m_ArrowClosed = o.Find(x => x.arrowClosed);
            m_ArrowOpened = o.Find(x => x.arrowOpened);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_IsOn);
            EditorGUILayout.PropertyField(m_Content);
            EditorGUILayout.PropertyField(m_ArrowClosed);
            EditorGUILayout.PropertyField(m_ArrowOpened);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
