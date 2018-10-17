using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Recorder;

namespace UnityEditor.Recorder
{
    public enum EFieldDisplayState
    {
        Enabled,
        Disabled,
        Hidden
    }

    public abstract class RecorderEditor : Editor
    {

        protected class InputEditorState
        {
            InputEditor.IsFieldAvailableDelegate m_Validator;
            public bool visible;
            public InputEditor editor { get; private set; }

            RecorderInputSetting m_SettingsObj;

            public RecorderInputSetting settingsObj
            {
                get { return m_SettingsObj; }
                set
                {
                    m_SettingsObj = value;
                    if (editor != null)
                        UnityHelpers.Destroy(editor);

                    editor = Editor.CreateEditor(m_SettingsObj) as InputEditor;
                    if (editor is InputEditor)
                        (editor as InputEditor).isFieldAvailableForHost = m_Validator;
                }
            }

            public InputEditorState(InputEditor.IsFieldAvailableDelegate validator, RecorderInputSetting settings)
            {
                m_Validator = validator;
                settingsObj = settings;
            }
        }

        protected List<InputEditorState> m_InputEditors;
        protected List<string> m_SettingsErrors = new List<string>();
        RTInputSelector m_RTInputSelector;

        SerializedProperty m_FrameRateMode;
        SerializedProperty m_FrameRate;
        SerializedProperty m_DurationMode;
        SerializedProperty m_StartFrame;
        SerializedProperty m_EndFrame;
        SerializedProperty m_StartTime;
        SerializedProperty m_EndTime;
        SerializedProperty m_SynchFrameRate;
        SerializedProperty m_CaptureEveryNthFrame;
        SerializedProperty m_FrameRateExact;
        SerializedProperty m_DestinationPath;
        SerializedProperty m_BaseFileName;


        string[] m_FrameRateLabels;

        protected virtual void OnEnable()
        {
            if (target != null)
            {
                m_InputEditors = new List<InputEditorState>();
                m_FrameRateLabels = EnumHelper.MaskOutEnumNames<EFrameRate>(0xFFFF, (x) => FrameRateHelper.ToLable((EFrameRate)x));

                var pf = new PropertyFinder<RecorderSettings>(serializedObject);
                m_FrameRateMode = pf.Find(x => x.m_FrameRateMode);
                m_FrameRate = pf.Find(x => x.m_FrameRate);
                m_DurationMode = pf.Find(x => x.m_DurationMode);
                m_StartFrame = pf.Find(x => x.m_StartFrame);
                m_EndFrame = pf.Find(x => x.m_EndFrame);
                m_StartTime = pf.Find(x => x.m_StartTime);
                m_EndTime = pf.Find(x => x.m_EndTime);
                m_SynchFrameRate = pf.Find(x => x.m_SynchFrameRate);
                m_CaptureEveryNthFrame = pf.Find(x => x.m_CaptureEveryNthFrame);
                m_FrameRateExact = pf.Find(x => x.m_FrameRateExact);
                m_DestinationPath = pf.Find(w => w.m_DestinationPath);
                m_BaseFileName = pf.Find(w => w.m_BaseFileName);

                m_RTInputSelector = new RTInputSelector(target as RecorderSettings);

                BuildInputEditors();
            }
        }

        void BuildInputEditors()
        {
            var rs = target as RecorderSettings;
            if (!rs.inputsSettings.hasBrokenBindings && rs.inputsSettings.Count == m_InputEditors.Count)
                return;

            if (rs.inputsSettings.hasBrokenBindings)
                rs.BindSceneInputSettings();

            foreach (var editor in m_InputEditors)
                UnityHelpers.Destroy(editor.editor);
            m_InputEditors.Clear();

            foreach (var input in rs.inputsSettings)
                m_InputEditors.Add(new InputEditorState(GetFieldDisplayState, input) { visible = true });
        }

        protected virtual void OnDisable() {}

        protected virtual void Awake() {}

        public bool ValidityCheck(List<string> errors)
        {
            return (target as RecorderSettings).ValidityCheck(errors)
                && (target as RecorderSettings).isPlatformSupported;
        }

        public bool showBounds { get; set; }

        bool m_FoldoutInput = true;
        bool m_FoldoutEncoder = true;
        bool m_FoldoutTime = true;
        bool m_FoldoutBounds = true;
        bool m_FoldoutOutput = true;

        protected virtual void OnGroupGui()
        {
            OnInputGroupGui();
            OnOutputGroupGui();
            OnEncodingGroupGui();
            OnFrameRateGroupGui();
            OnBoundsGroupGui();
            OnExtraGroupsGui();
        }

        public override void OnInspectorGUI()
        {
            if (target == null)
                return;

            BuildInputEditors();

            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            OnGroupGui();

            serializedObject.ApplyModifiedProperties();

            EditorGUI.EndChangeCheck();

            (target as RecorderSettings).SelfAdjustSettings();

            OnValidateSettingsGUI();
        }

        public virtual void OnValidateSettingsGUI()
        {
            m_SettingsErrors.Clear();
            if (!(target as RecorderSettings).ValidityCheck(m_SettingsErrors))
            {
                for (int i = 0; i < m_SettingsErrors.Count; i++)
                {
                    EditorGUILayout.HelpBox(m_SettingsErrors[i], MessageType.Warning);
                }
            }
        }

        protected void AddInputSettings(RecorderInputSetting inputSettings)
        {
            var inputs = (target as RecorderSettings).inputsSettings;
            inputs.Add(inputSettings);
            m_InputEditors.Add(new InputEditorState(GetFieldDisplayState, inputSettings) { visible = true });
        }

        public void ChangeInputSettings(int atIndex, RecorderInputSetting newSettings)
        {
            if (newSettings != null)
            {
                var inputs = (target as RecorderSettings).inputsSettings;
                inputs.ReplaceAt(atIndex, newSettings);
                m_InputEditors[atIndex].settingsObj = newSettings;
            }
            else if (m_InputEditors.Count == 0)
            {
                throw new Exception("Source removal not implemented");
            }
        }

        protected virtual void OnInputGui()
        {
            var inputs = (target as RecorderSettings).inputsSettings;

            bool multiInputs = inputs.Count > 1;
            for (int i = 0; i < inputs.Count; i++)
            {
                if (multiInputs)
                {
                    EditorGUI.indentLevel++;
                    m_InputEditors[i].visible = EditorGUILayout.Foldout(m_InputEditors[i].visible, m_InputEditors[i].settingsObj.m_DisplayName ?? "Input " + (i + 1));
                }

                if (m_InputEditors[i].visible)
                    OnInputGui(i);

                if (multiInputs)
                    EditorGUI.indentLevel--;
            }
        }

        protected virtual void OnInputGui(int inputIndex)
        {
            var inputs = (target as RecorderSettings).inputsSettings;
            var input = inputs[inputIndex];
            if (m_RTInputSelector.OnInputGui(inputIndex, ref input))
                ChangeInputSettings(inputIndex, input);

            m_InputEditors[inputIndex].editor.OnInspectorGUI();
            m_InputEditors[inputIndex].editor.OnValidateSettingsGUI();
        }

        protected virtual void OnOutputGui()
        {
            AddProperty(m_DestinationPath, () => { EditorGUILayout.PropertyField(m_DestinationPath, new GUIContent("Output path")); });
            AddProperty(m_BaseFileName, () => { EditorGUILayout.PropertyField(m_BaseFileName, new GUIContent("File name")); });
            AddProperty(m_CaptureEveryNthFrame, () => EditorGUILayout.PropertyField(m_CaptureEveryNthFrame, new GUIContent("Every n'th frame")));
        }

        protected virtual void OnEncodingGui()
        {
            // place holder
        }

        protected virtual void OnFrameRateGui()
        {

            AddProperty(m_FrameRateMode, () => EditorGUILayout.PropertyField(m_FrameRateMode, new GUIContent("Constraint Type")));

            AddProperty(m_FrameRateExact, () =>
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var label = m_FrameRateMode.intValue == (int)FrameRateMode.Constant ? "Target fps" : "Max fps";
                    var index = EnumHelper.GetMaskedIndexFromEnumValue<EFrameRate>(m_FrameRateExact.intValue, 0xFFFF);
                    index = EditorGUILayout.Popup(label, index, m_FrameRateLabels);

                    if (check.changed)
                    {
                        m_FrameRateExact.intValue = EnumHelper.GetEnumValueFromMaskedIndex<EFrameRate>(index, 0xFFFF);
                        if (m_FrameRateExact.intValue != (int)EFrameRate.FR_CUSTOM)
                            m_FrameRate.floatValue = FrameRateHelper.ToFloat((EFrameRate)m_FrameRateExact.intValue, m_FrameRate.floatValue);
                    }
                }
            });

            AddProperty(m_FrameRate, () =>
            {
                if (m_FrameRateExact.intValue == (int)EFrameRate.FR_CUSTOM)
                {
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(m_FrameRate, new GUIContent("Value"));
                    --EditorGUI.indentLevel;
                }
            });

            AddProperty(m_FrameRateMode, () =>
            {
                if (m_FrameRateMode.intValue == (int)FrameRateMode.Constant)
                    EditorGUILayout.PropertyField(m_SynchFrameRate, new GUIContent("Sync. framerate"));
            });
        }

        protected virtual void OnBoundsGui()
        {
            EditorGUILayout.PropertyField(m_DurationMode, new GUIContent("Mode"));

            ++EditorGUI.indentLevel;
            switch ((DurationMode)m_DurationMode.intValue)
            {
                case DurationMode.Manual:
                {
                    break;
                }
                case DurationMode.SingleFrame:
                {
                    AddProperty(m_StartFrame, () =>
                    {
                        EditorGUILayout.PropertyField(m_StartFrame, new GUIContent("Frame #"));
                        m_EndFrame.intValue = m_StartFrame.intValue;
                    });
                    break;
                }
                case DurationMode.FrameInterval:
                {
                    AddProperty(m_StartFrame, () => EditorGUILayout.PropertyField(m_StartFrame, new GUIContent("First frame")));
                    AddProperty(m_EndFrame, () => EditorGUILayout.PropertyField(m_EndFrame, new GUIContent("Last frame")));
                    break;
                }
                case DurationMode.TimeInterval:
                {
                    AddProperty(m_StartTime, () => EditorGUILayout.PropertyField(m_StartTime, new GUIContent("Start (sec)")));
                    AddProperty(m_EndFrame, () => EditorGUILayout.PropertyField(m_EndTime, new GUIContent("End (sec)")));
                    break;
                }
            }
            --EditorGUI.indentLevel;
        }

        protected virtual void OnInputGroupGui()
        {
            m_FoldoutInput = EditorGUILayout.Foldout(m_FoldoutInput, "Input(s)");
            if (m_FoldoutInput)
            {
                ++EditorGUI.indentLevel;
                OnInputGui();
                --EditorGUI.indentLevel;
            }
        }

        protected virtual void OnOutputGroupGui()
        {
            m_FoldoutOutput = EditorGUILayout.Foldout(m_FoldoutOutput, "Output(s)");
            if (m_FoldoutOutput)
            {
                ++EditorGUI.indentLevel;
                OnOutputGui();
                --EditorGUI.indentLevel;
            }
        }

        protected virtual void OnEncodingGroupGui()
        {
            m_FoldoutEncoder = EditorGUILayout.Foldout(m_FoldoutEncoder, "Encoding");
            if (m_FoldoutEncoder)
            {
                ++EditorGUI.indentLevel;
                OnEncodingGui();
                --EditorGUI.indentLevel;
            }
        }

        protected virtual void OnFrameRateGroupGui()
        {
            m_FoldoutTime = EditorGUILayout.Foldout(m_FoldoutTime, "Frame rate");
            if (m_FoldoutTime)
            {
                ++EditorGUI.indentLevel;
                OnFrameRateGui();
                --EditorGUI.indentLevel;
            }
        }

        protected virtual void OnBoundsGroupGui()
        {
            if (showBounds)
            {
                m_FoldoutBounds = EditorGUILayout.Foldout(m_FoldoutBounds, "Bounds / Limits");
                if (m_FoldoutBounds)
                {
                    ++EditorGUI.indentLevel;
                    OnBoundsGui();
                    --EditorGUI.indentLevel;
                }
            }
        }

        protected virtual void OnExtraGroupsGui()
        {
            // nothing. this is for sub classes...
        }

        protected virtual EFieldDisplayState GetFieldDisplayState(SerializedProperty property)
        {
            return EFieldDisplayState.Enabled;
        }

        protected void AddProperty(SerializedProperty prop, Action action)
        {
            var state = GetFieldDisplayState(prop);
            if (state != EFieldDisplayState.Hidden)
            {
                using (new EditorGUI.DisabledScope(state == EFieldDisplayState.Disabled))
                    action();
            }
        }


    }
}

