using System;
using System.Collections.Generic;

namespace UnityEngine.Recorder
{
    [Flags]
    public enum EImageSource
    {
        ActiveCameras = 1,
        SceneView = 2,
        MainCamera = 4,
        TaggedCamera = 8,
        RenderTexture = 16,
    }

    public enum FrameRateMode
    {
        Variable,
        Constant,
    }

    public enum DurationMode
    {
        Manual,
        SingleFrame,
        FrameInterval,
        TimeInterval
    }

    public class InputFilter
    {
        public InputFilter(string title, Type type)
        {
            this.title = title;
            this.type = type;
        }
        public string title { get; private set; }
        public Type type { get; private set; }

    }


    public class TInputFilter<T> : InputFilter
    {
        public TInputFilter(string title) : base( title, typeof(T))
        {
        }
    }

    public struct InputGroupFilter
    {
        public string title;
        public List<InputFilter> typesFilter;
    }

    /// <summary>
    /// What is this: Base settings class for all Recorders.
    /// Motivation  : All recorders share a basic common set of settings and some of them are known to the 
    ///                 recording framework, so we need a base class to allow the framework to access these settings.
    /// Notes: 
    /// - Some of the fields in this class actually don't apply to ALL recorders but are so common that they are included 
    ///   here for convenience.
    /// </summary>    
    [ExecuteInEditMode]
    public abstract class RecorderSettings : ScriptableObject
    {
        [SerializeField]
        string m_AssetID;
        public int m_CaptureEveryNthFrame = 1;
        public FrameRateMode m_FrameRateMode = FrameRateMode.Constant;
        [Range(1, 120)]
        public double m_FrameRate = 30.0;
        public EFrameRate m_FrameRateExact = EFrameRate.FR_CUSTOM;
        public int m_StartFrame;
        public int m_EndFrame = 10;
        public float m_StartTime = 0.0f;
        public float m_EndTime = 1.0f;
        public DurationMode m_DurationMode;
        public bool m_SynchFrameRate = true;
        public FileNameGenerator m_BaseFileName;
        public OutputPath m_DestinationPath;

        [SerializeField]
        private InputSettingsList m_InputsSettings = new InputSettingsList();

        public InputSettingsList inputsSettings
        {
            get { return m_InputsSettings; }
        }


        [SerializeField]
        string m_RecorderTypeName;

        public string assetID
        {
            get { return m_AssetID; }
            set
            {
                m_AssetID = value;
                m_InputsSettings.ownerRecorderSettingsAssetId = value;
            }
        }

        public RecorderSettings()
        {
            m_DestinationPath.root = OutputPath.ERoot.Current;
            m_DestinationPath.leaf = "Recordings";
        }

        public Type recorderType
        {
            get
            {
                if (string.IsNullOrEmpty(m_RecorderTypeName))
                    return null;
                return Type.GetType(m_RecorderTypeName);
            }
            set { m_RecorderTypeName = value == null ? string.Empty : value.AssemblyQualifiedName; }
        }

        public bool fixedDuration
        {
            get { return m_DurationMode != DurationMode.Manual; }
        }

        public virtual bool ValidityCheck( List<string> errors )
        {
            bool ok = true;

            if (m_InputsSettings != null)
            {
                var inputErrors = new List<string>();
                if (!m_InputsSettings.ValidityCheck(inputErrors))
                {
                    errors.Add("Input settings are incorrect.");
                    ok = false;
                }
            }

            if (Math.Abs(m_FrameRate) <= float.Epsilon)
            {
                ok = false;
                errors.Add("Invalid frame rate.");
            }

            if (m_CaptureEveryNthFrame <= 0)
            {
                ok = false;
                errors.Add("Invalid frame skip value");
            }

            if (!isPlatformSupported)
            {
                errors.Add("Current platform is not supported");
                ok  = false;
            }

            return ok;
        }

        public virtual bool isPlatformSupported
        {
            get { return true; }
        }

        public virtual void OnEnable()
        {
            m_InputsSettings.OnEnable(m_AssetID);
            BindSceneInputSettings();
        }

        public void BindSceneInputSettings()
        {
            if (!m_InputsSettings.hasBrokenBindings)
                return;

            m_InputsSettings.Rebuild();

#if UNITY_EDITOR
            if (m_InputsSettings.hasBrokenBindings)
            {
                // only supported case is scene stored input settings are missing (for example: new scene loaded that does not contain the scene stored inputs.)
                m_InputsSettings.RepareMissingBindings();
            }
#endif

            if (m_InputsSettings.hasBrokenBindings)
                Debug.LogError("Recorder: missing input settings");
        }

        public virtual void OnDestroy()
        {
            if (m_InputsSettings != null)
                m_InputsSettings.OnDestroy();
        }

        public abstract List<RecorderInputSetting> GetDefaultInputSettings();

        public T NewInputSettingsObj<T>(string title) where T : class
        {
            return NewInputSettingsObj(typeof(T), title) as T;
        }

        public virtual RecorderInputSetting NewInputSettingsObj(Type type, string title)
        {
            var obj = (RecorderInputSetting)ScriptableObject.CreateInstance(type);
            obj.m_DisplayName = title;
            obj.name = Guid.NewGuid().ToString();
            return obj;
        }

        public abstract List<InputGroupFilter> GetInputGroups();

        /// <summary>
        /// Allows for recorder specific settings logic to correct/adjust settings that might be missed by it's editor.
        /// </summary>
        /// <returns>true if setting where changed</returns>
        public virtual bool SelfAdjustSettings()
        {
            return false;
        }

    }
}
