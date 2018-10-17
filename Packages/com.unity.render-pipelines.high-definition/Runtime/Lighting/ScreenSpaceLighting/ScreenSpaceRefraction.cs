using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class ScreenSpaceRefraction : ScreenSpaceLighting
    {
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

        protected override void FetchIDs(
            out int rayLevelID,
            out int rayMinLevelID,
            out int rayMaxLevelID,
            out int rayMaxIterationsID,
            out int depthBufferThicknessID,
            out int invScreenWeightDistanceID,
            out int rayMaxScreenDistanceID,
            out int rayBlendScreenDistanceID,
            out int rayMarchBehindObjectsID
            )
        {
            rayLevelID = HDShaderIDs._SSRefractionRayLevel;
            rayMinLevelID = HDShaderIDs._SSRefractionRayMinLevel;
            rayMaxLevelID = HDShaderIDs._SSRefractionRayMaxLevel;
            rayMaxIterationsID = HDShaderIDs._SSRefractionRayMaxIterations;
            depthBufferThicknessID = HDShaderIDs._SSRefractionDepthBufferThickness;
            invScreenWeightDistanceID = HDShaderIDs._SSRefractionInvScreenWeightDistance;
            rayMaxScreenDistanceID = HDShaderIDs._SSRefractionRayMaxScreenDistance;
            rayBlendScreenDistanceID = HDShaderIDs._SSRefractionRayBlendScreenDistance;
            rayMarchBehindObjectsID = HDShaderIDs._SSRefractionRayMarchBehindObjects;
        }
    }
}
