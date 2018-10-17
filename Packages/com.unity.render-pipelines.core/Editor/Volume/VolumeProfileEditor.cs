using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    [CustomEditor(typeof(VolumeProfile))]
    sealed class VolumeProfileEditor : Editor
    {
        VolumeComponentListEditor m_ComponentList;

        void OnEnable()
        {
            m_ComponentList = new VolumeComponentListEditor(this);
            m_ComponentList.Init(target as VolumeProfile, serializedObject);
        }

        void OnDisable()
        {
            if (m_ComponentList != null)
                m_ComponentList.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_ComponentList.OnGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
