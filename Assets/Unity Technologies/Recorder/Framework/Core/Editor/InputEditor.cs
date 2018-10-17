using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Recorder;

namespace UnityEditor.Recorder
{
    public abstract class InputEditor : Editor
    {
        protected List<string> m_SettingsErrors = new List<string>();

        public delegate EFieldDisplayState IsFieldAvailableDelegate(SerializedProperty property);

        public IsFieldAvailableDelegate isFieldAvailableForHost { get; set; }

        protected virtual void AddProperty(SerializedProperty prop, Action action)
        {
            var state = isFieldAvailableForHost == null ? EFieldDisplayState.Disabled : isFieldAvailableForHost(prop);

            if (state == EFieldDisplayState.Enabled)
                state = IsFieldAvailable(prop);
            if (state != EFieldDisplayState.Hidden)
            {
                using (new EditorGUI.DisabledScope(state == EFieldDisplayState.Disabled))
                    action();
            }
        }

        protected virtual EFieldDisplayState IsFieldAvailable(SerializedProperty property)
        {
            return EFieldDisplayState.Enabled;
        }

        public virtual void OnValidateSettingsGUI()
        {
            m_SettingsErrors.Clear();
            if (!(target as RecorderInputSetting).ValidityCheck(m_SettingsErrors))
            {
                for (int i = 0; i < m_SettingsErrors.Count; i++)
                {
                    EditorGUILayout.HelpBox(m_SettingsErrors[i], MessageType.Warning);
                }
            }
        }
    }
}
