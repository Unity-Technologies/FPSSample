using UnityEditor.AnimatedValues;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDCameraUI : BaseUI<SerializedHDCamera>
    {
        SerializedHDCamera m_SerializedHdCamera;

        public AnimBool isSectionExpandedOrthoOptions { get { return m_AnimBools[0]; } }
        public AnimBool isSectionExpandedPhysicalSettings { get { return m_AnimBools[1]; } }
        public AnimBool isSectionExpandedGeneralSettings { get { return m_AnimBools[2]; } }
        public AnimBool isSectionExpandedOutputSettings { get { return m_AnimBools[3]; } }
        public AnimBool isSectionAvailableRenderLoopSettings { get { return m_AnimBools[4]; } }
        public AnimBool isSectionExpandedXRSettings { get { return m_AnimBools[5]; } }
        public AnimBool isSectionAvailableXRSettings { get { return m_AnimBools[6]; } }

        public bool canOverrideRenderLoopSettings { get; set; }

        public FrameSettingsUI frameSettingsUI = new FrameSettingsUI();

        public HDCameraUI()
            : base(7)
        {
            canOverrideRenderLoopSettings = false;
            isSectionExpandedGeneralSettings.value = true;
        }

        public override void Reset(SerializedHDCamera data, UnityAction repaint)
        {
            m_SerializedHdCamera = data;
            frameSettingsUI.Reset(data.frameSettings, repaint);

            for (var i = 0; i < m_AnimBools.Length; ++i)
            {
                m_AnimBools[i].valueChanged.RemoveAllListeners();
                m_AnimBools[i].valueChanged.AddListener(repaint);
            }

            Update();
        }

        public override void Update()
        {
            base.Update();

            var renderingPath = (HDAdditionalCameraData.RenderingPath)m_SerializedHdCamera.renderingPath.intValue;
            canOverrideRenderLoopSettings = renderingPath == HDAdditionalCameraData.RenderingPath.Custom;

            isSectionExpandedOrthoOptions.target = !m_SerializedHdCamera.baseCameraSettings.orthographic.hasMultipleDifferentValues && m_SerializedHdCamera.baseCameraSettings.orthographic.boolValue;
            isSectionAvailableXRSettings.target = PlayerSettings.virtualRealitySupported;
            // SRP settings are available only if the rendering path is not the Default one (configured by the SRP asset)
            isSectionAvailableRenderLoopSettings.target = canOverrideRenderLoopSettings;

            frameSettingsUI.Update();
        }
    }
}
