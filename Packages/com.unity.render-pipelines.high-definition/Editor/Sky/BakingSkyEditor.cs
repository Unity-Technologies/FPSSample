using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(BakingSky))]
    [DisallowMultipleComponent]
    public class BakingSkyEditor : Editor
    {
        SerializedProperty m_VolumeProfile;
        SerializedProperty m_SkyUniqueID;

        List<GUIContent> m_SkyClassNames = null;
        List<int> m_SkyUniqueIDs = null;

        void InitializeProperties()
        {
            m_VolumeProfile = serializedObject.FindProperty("m_Profile");
            m_SkyUniqueID = serializedObject.FindProperty("m_BakingSkyUniqueID");
        }

        void UpdateSkyIntPopupData(bool reset = false)
        {
            if (m_SkyClassNames == null)
            {
                m_SkyClassNames = new List<GUIContent>();
                m_SkyUniqueIDs = new List<int>();
            }

            // We always reinit because the content can change depending on the volume and we are not always notified when this happens (like for undo/redo for example)
            m_SkyClassNames.Clear();
            m_SkyUniqueIDs.Clear();

            // Add special "None" case.
            m_SkyClassNames.Add(new GUIContent("None"));
            m_SkyUniqueIDs.Add(0);

            VolumeProfile profile = m_VolumeProfile.objectReferenceValue as VolumeProfile;
            if (profile != null)
            {
                var skyTypesDict = SkyManager.skyTypesDict;

                foreach (KeyValuePair<int, Type> kvp in skyTypesDict)
                {
                    if (profile.Has(kvp.Value))
                    {
                        m_SkyClassNames.Add(new GUIContent(kvp.Value.Name.ToString()));
                        m_SkyUniqueIDs.Add(kvp.Key);
                    }
                }
            }
        }

        protected void OnEnable()
        {
            InitializeProperties();

            if (m_VolumeProfile.objectReferenceValue == null)
            {
                BakingSky bakingSky = (BakingSky)target;
                Volume volume = bakingSky.GetComponent<Volume>();
                if (volume != null)
                {
                    bakingSky.profile = volume.sharedProfile;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Lazy init because domain reload, undo/redo, etc...
            UpdateSkyIntPopupData();

            EditorGUILayout.PropertyField(m_VolumeProfile);
            using (new EditorGUI.DisabledScope(m_SkyClassNames.Count == 1)) // Only "None"
            {
                EditorGUILayout.IntPopup(m_SkyUniqueID, m_SkyClassNames.ToArray(), m_SkyUniqueIDs.ToArray(), CoreEditorUtils.GetContent("Baking Sky|Specify which kind of sky you want to use for baking in the referenced profile."));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
