using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class ScreenSpaceReflection : VolumeComponent
    {

        public ClampedFloatParameter depthBufferThickness = new ClampedFloatParameter(0.01f, 0, 1);
        public ClampedFloatParameter screenFadeDistance = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);
        public ClampedFloatParameter minSmoothness = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);
        public ClampedFloatParameter smoothnessFadeStart = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);
        public BoolParameter         reflectSky          = new BoolParameter(true);

        public IntParameter rayMaxIterations = new IntParameter(32);


        static ScreenSpaceReflection s_Default = null;
        public static ScreenSpaceReflection @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<ScreenSpaceReflection>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }

    }
}
