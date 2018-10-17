using System;
using System.Collections.Generic;

namespace UnityEngine.Recorder.Input
{
    /// <summary>
    /// What is it: 
    /// Motivation: 
    /// </summary>
    public enum ESuperSamplingCount
    {
        x1 = 1,
        x2 = 2,
        x4 = 4,
        x8 = 8,
        x16 = 16,
    }

    public class RenderTextureSamplerSettings : ImageInputSettings
    {
        public EImageSource source = EImageSource.ActiveCameras;
        public EImageDimension m_RenderSize = EImageDimension.x720p_HD;
        public ESuperSamplingCount m_SuperSampling = ESuperSamplingCount.x1;
        public float m_SuperKernelPower = 16f;
        public float m_SuperKernelScale = 1f;
        public string m_CameraTag;
        public ColorSpace m_ColorSpace = ColorSpace.Gamma;
        public bool m_FlipFinalOutput = false;

        public override Type inputType
        {
            get { return typeof(RenderTextureSampler); }
        }

        public override bool ValidityCheck( List<string> errors )
        {
            return true;
        }
    }
}