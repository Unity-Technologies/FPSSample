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
        List<GUIContent> m_FogNames = null;
        List<int> m_SkyUniqueIDs = null;

        public static readonly string[] fogNames = Enum.GetNames(typeof(FogType));
        public static readonly int[] fogValues = Enum.GetValues(typeof(FogType)) as int[];

        public override void OnEnable()
        {
            base.OnEnable();
            var o = new PropertyFetcher<VisualEnvironment>(serializedObject);

            m_SkyType = Unpack(o.Find(x => x.skyType));
            m_FogType = Unpack(o.Find(x => x.fogType));
        }

        void UpdateSkyAndFogIntPopupData()
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
                    m_SkyClassNames.Add(new GUIContent(ObjectNames.NicifyVariableName(kvp.Value.Name.ToString())));
                    m_SkyUniqueIDs.Add(kvp.Key);
                }
            }

            if (m_FogNames == null)
            {
                m_FogNames = new List<GUIContent>();

                foreach (string fogStr in fogNames)
                {
                    m_FogNames.Add(new GUIContent(fogStr + " Fog"));
                }
            }
        }

        public override void OnInspectorGUI()
        {
            UpdateSkyAndFogIntPopupData();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawOverrideCheckbox(m_SkyType);
                using (new EditorGUI.DisabledScope(!m_SkyType.overrideState.boolValue))
                {
                    EditorGUILayout.IntPopup(m_SkyType.value, m_SkyClassNames.ToArray(), m_SkyUniqueIDs.ToArray(), new GUIContent("Sky Type"));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawOverrideCheckbox(m_FogType);
                using (new EditorGUI.DisabledScope(!m_FogType.overrideState.boolValue))
                {
                    EditorGUILayout.IntPopup(m_FogType.value, m_FogNames.ToArray(), fogValues, new GUIContent("Fog Type"));
                }
            }
        }
    }
}
