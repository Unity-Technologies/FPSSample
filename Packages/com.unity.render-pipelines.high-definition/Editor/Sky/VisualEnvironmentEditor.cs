using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(VisualEnvironment))]
    public class VisualEnvironmentEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_SkyType;
        SerializedDataParameter m_FogType;

        List<GUIContent> m_SkyClassNames = null;
        List<int> m_SkyUniqueIDs = null;

        public override void OnEnable()
        {
            base.OnEnable();
            var o = new PropertyFetcher<VisualEnvironment>(serializedObject);

            m_SkyType = Unpack(o.Find(x => x.skyType));
            m_FogType = Unpack(o.Find(x => x.fogType));
        }

        void UpdateSkyIntPopupData()
        {
            if (m_SkyClassNames == null)
            {
                m_SkyClassNames = new List<GUIContent>();
                m_SkyUniqueIDs = new List<int>();

                // Add special "None" case.
                m_SkyClassNames.Add(new GUIContent("None"));
                m_SkyUniqueIDs.Add(0);

                var skyTypesDict = SkyManager.skyTypesDict;

                foreach (KeyValuePair<int, Type> kvp in skyTypesDict)
                {
                    m_SkyClassNames.Add(new GUIContent(kvp.Value.Name.ToString()));
                    m_SkyUniqueIDs.Add(kvp.Key);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            UpdateSkyIntPopupData();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawOverrideCheckbox(m_SkyType);
                using (new EditorGUI.DisabledScope(!m_SkyType.overrideState.boolValue))
                {
                    EditorGUILayout.IntPopup(m_SkyType.value, m_SkyClassNames.ToArray(), m_SkyUniqueIDs.ToArray(), new GUIContent("Sky Type"));
                }
            }
            PropertyField(m_FogType);
        }
    }
}
