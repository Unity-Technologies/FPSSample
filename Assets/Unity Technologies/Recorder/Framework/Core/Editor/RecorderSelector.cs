using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Recorder;

namespace UnityEditor.Recorder
{
    class RecorderSelector
    {
        string m_Category;
        string[] m_RecorderNames;
        string[] m_Categories;
        List<RecorderInfo> m_Recorders;
        //bool m_SettingsAreAssets;
        bool m_CategoryIsReadonly = false;

        public Type selectedRecorder { get; private set; }

        Action m_SetRecorderCallback;

        public string category {
            get { return m_Category;}
        }

        public RecorderSelector(Action setRecorderCallback, bool categoryIsReadonly)
        {
            m_CategoryIsReadonly = categoryIsReadonly;
            m_Categories = RecordersInventory.availableCategories;
            m_SetRecorderCallback = setRecorderCallback;
        }

        public void Init( RecorderSettings settings, string startingCategory = "" )
        {
            // Pre existing settings obj?
            if( settings != null )
            {
                var recInfo = RecordersInventory.GetRecorderInfo(settings.recorderType);

                // category value overrides existing settings.
                if (!string.IsNullOrEmpty(startingCategory))
                {
                    if (string.Compare(recInfo.category, startingCategory, StringComparison.InvariantCultureIgnoreCase) != 0)
                    {
                        // forced another category, flush existing settings obj.
                        SetCategory(startingCategory);
                        SelectRecorder( GetRecorderFromIndex(0) );
                    }
                }

                // Not invalidated by category, so set and we are done
                if( settings != null )
                {
                    SetCategory(recInfo.category);
                    SelectRecorder(settings.recorderType);
                    return;
                }
            }
            else
                SetCategory(startingCategory);
        }

        int GetCategoryIndex()
        {
            for (int i = 0; i < m_Categories.Length; i++)
                if (m_Categories[i] == m_Category)
                    return i;

            if (m_Categories.Length > 0)
                return 0;
            else
                return -1;
        }

        void SetCategory(string category)
        {
            m_Category = category;
            if (string.IsNullOrEmpty(m_Category) && m_Categories.Length > 0)
                m_Category = "Video"; // default

            if (string.IsNullOrEmpty(m_Category))
            {
                m_Category = string.Empty;
                m_RecorderNames = new string[0];                
            }
            else
            {
                m_Recorders = RecordersInventory.recordersByCategory[m_Category];
                m_RecorderNames = RecordersInventory.recordersByCategory[m_Category]
                    .Select(x => x.displayName)
                    .ToArray();
            }
        }

        void SetCategoryFromIndex(int index)
        {
            if (index >= 0)
            {
                var newCategory = RecordersInventory.availableCategories[index];
                if (string.Compare(m_Category, newCategory, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return;
                SetCategory(newCategory);
            }
            else
            {
                SetCategory(string.Empty);
            }
        }

        int GetRecorderIndex()
        {
            if (m_Recorders.Count == 0)
                return -1;
            
            for (int i = 0; i < m_Recorders.Count; i++)
                if (m_Recorders[i].recorderType == selectedRecorder)
                    return i;

            if (m_Recorders.Count > 0)
                return 0;
            else
                return -1;
        }

        Type GetRecorderFromIndex(int index)
        {
            if (index >= 0)
                return RecordersInventory.recordersByCategory[m_Category][index].recorderType;

            return null;
        }

        public void OnGui()
        {
            // Group selection
            if (!m_CategoryIsReadonly)
            {
                EditorGUILayout.BeginHorizontal();
                SetCategoryFromIndex(EditorGUILayout.Popup("Recorder category:", GetCategoryIndex(), m_Categories));
                EditorGUILayout.EndHorizontal();
            }

            // Recorder in group selection
            EditorGUILayout.BeginHorizontal();
            var oldIndex = GetRecorderIndex();
            var newIndex = EditorGUILayout.Popup("Selected recorder:", oldIndex, m_RecorderNames);
            SelectRecorder(GetRecorderFromIndex(newIndex));

            EditorGUILayout.EndHorizontal();
        }

        void SelectRecorder( Type newSelection )
        {
            if (selectedRecorder == newSelection)
                return;

            var recorderAttribs = newSelection.GetCustomAttributes(typeof(ObsoleteAttribute), false);
            if (recorderAttribs.Length > 0 )
                Debug.LogWarning( "Recorder " + ((ObsoleteAttribute)recorderAttribs[0]).Message);

            selectedRecorder = newSelection;
            m_SetRecorderCallback();
        }
    }
}
