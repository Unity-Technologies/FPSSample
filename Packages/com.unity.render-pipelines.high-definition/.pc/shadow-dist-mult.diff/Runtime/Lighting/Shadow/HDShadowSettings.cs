using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class HDShadowSettings : VolumeComponent
    {
        float[] m_CascadeShadowSplits = new float[3];
        float[] m_CascadeShadowBorders = new float[4];

        public float[] cascadeShadowSplits
        {
            get
            {
                m_CascadeShadowSplits[0] = cascadeShadowSplit0;
                m_CascadeShadowSplits[1] = cascadeShadowSplit1;
                m_CascadeShadowSplits[2] = cascadeShadowSplit2;
                return m_CascadeShadowSplits;
            }
        }

        public float[] cascadeShadowBorders
        {
            get
            {
                m_CascadeShadowBorders[0] = cascadeShadowBorder0;
                m_CascadeShadowBorders[1] = cascadeShadowBorder1;
                m_CascadeShadowBorders[2] = cascadeShadowBorder2;
                m_CascadeShadowBorders[3] = cascadeShadowBorder3;

                // For now we don't use shadow cascade borders but we still want to have the last split fading out.
                if (!LightLoop.s_UseCascadeBorders)
                {
                    m_CascadeShadowBorders[cascadeShadowSplitCount - 1] = 0.2f;
                }
                return m_CascadeShadowBorders;
            }
        }

        [Tooltip("Maximum shadow distance for all light types.")]
        public NoInterpMinFloatParameter        maxShadowDistance = new NoInterpMinFloatParameter(500.0f, 0.0f);

        [Tooltip("Number of splits for cascaded shadow maps.")]
        public NoInterpClampedIntParameter      cascadeShadowSplitCount = new NoInterpClampedIntParameter(4, 1, 4);
        [Tooltip("Ratio of the first split against max shadow distance.")]
        public NoInterpClampedFloatParameter    cascadeShadowSplit0 = new NoInterpClampedFloatParameter(0.05f, 0.0f, 1.0f);
        [Tooltip("Ratio of the second split against max shadow distance.")]
        public NoInterpClampedFloatParameter    cascadeShadowSplit1 = new NoInterpClampedFloatParameter(0.15f, 0.0f, 1.0f);
        [Tooltip("Ratio of the third split against max shadow distance.")]
        public NoInterpClampedFloatParameter    cascadeShadowSplit2 = new NoInterpClampedFloatParameter(0.3f, 0.0f, 1.0f);
        [Tooltip("Border size between first and second split.")]
        public NoInterpMinFloatParameter        cascadeShadowBorder0 = new NoInterpMinFloatParameter(0.0f, 0.0f);
        [Tooltip("Border size between second and third split.")]
        public NoInterpMinFloatParameter        cascadeShadowBorder1 = new NoInterpMinFloatParameter(0.0f, 0.0f);
        [Tooltip("Border size between third and last split.")]
        public NoInterpMinFloatParameter        cascadeShadowBorder2 = new NoInterpMinFloatParameter(0.0f, 0.0f);
        [Tooltip("Border size at the end of last split.")]
        public NoInterpMinFloatParameter        cascadeShadowBorder3 = new NoInterpMinFloatParameter(0.0f, 0.0f);
    }
}
