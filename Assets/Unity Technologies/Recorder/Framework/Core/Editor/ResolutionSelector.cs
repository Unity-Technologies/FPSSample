using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Input;

namespace UnityEditor.Recorder
{
    public class ResolutionSelector
    {
        string[] m_MaskedNames;
        EImageDimension m_MaxRes = EImageDimension.Window;

        public void OnInspectorGUI(EImageDimension max, SerializedProperty size )
        {
            if (m_MaskedNames == null || max != m_MaxRes)
            {
                m_MaskedNames = EnumHelper.ClipOutEnumNames<EImageDimension>((int)EImageDimension.Window, (int)max);
                m_MaxRes = max;
            }

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var index = EnumHelper.GetClippedIndexFromEnumValue<EImageDimension>(size.intValue, (int)EImageDimension.Window, (int)m_MaxRes);
                index = EditorGUILayout.Popup("Output Resolution", index, m_MaskedNames);

                if (check.changed)
                    size.intValue = EnumHelper.GetEnumValueFromClippedIndex<EImageDimension>(index, (int)EImageDimension.Window, (int)m_MaxRes);

                if (size.intValue > (int)m_MaxRes)
                    size.intValue = (int)m_MaxRes;
            }
        }
    }
}