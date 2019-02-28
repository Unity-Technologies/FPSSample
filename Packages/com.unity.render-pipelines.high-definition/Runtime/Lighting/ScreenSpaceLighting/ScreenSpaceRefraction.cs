using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{

    [Serializable]
    public class ScreenSpaceRefraction : VolumeComponent
    {
        public enum RefractionModel
        {
            None = 0,
            Box = 1,
            Sphere = 2
        };

        int m_InvScreenFadeDistanceID;

        public ClampedFloatParameter screenFadeDistance = new ClampedFloatParameter(0.1f, 0.001f, 1.0f);

        static ScreenSpaceRefraction s_Default = null;

        public static ScreenSpaceRefraction @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<ScreenSpaceRefraction>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }

        public virtual void PushShaderParameters(CommandBuffer cmd)
        {
            cmd.SetGlobalFloat(m_InvScreenFadeDistanceID, 1.0f / screenFadeDistance.value);
        }

        protected void FetchIDs(
            out int invScreenWeightDistanceID)
        {
            invScreenWeightDistanceID = HDShaderIDs._SSRefractionInvScreenWeightDistance;
        }

        void Awake()
        {
            FetchIDs(
                out m_InvScreenFadeDistanceID
                );
        }

    }
}
