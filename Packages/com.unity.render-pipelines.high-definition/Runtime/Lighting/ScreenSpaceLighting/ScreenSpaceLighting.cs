using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public abstract class ScreenSpaceLighting : VolumeComponent
    {
        public enum RefractionModel
        {
            None = 0,
            Plane = 1,
            Sphere = 2
        };

        [GenerateHLSL]
        public enum HiZIntersectionKind
        {
            None,
            Cell,
            Depth
        }

        int m_RayLevelID;
        int m_RayMinLevelID;
        int m_RayMaxLevelID;
        int m_RayMaxIterationsID;
        int m_DepthBufferThicknessID;
        int m_InvScreenWeightDistanceID;
        int m_RayMaxScreenDistanceID;
        int m_RayBlendScreenDistanceID;
        int m_RayMarchBehindObjectsID;

        public IntParameter                 rayLevel = new IntParameter(2);
        public IntParameter                 rayMinLevel = new IntParameter(2);
        public IntParameter                 rayMaxLevel = new IntParameter(6);
        public IntParameter                 rayMaxIterations = new IntParameter(32);
        public ClampedFloatParameter        depthBufferThickness = new ClampedFloatParameter(0.01f, 0, 1);
        public ClampedFloatParameter        screenWeightDistance = new ClampedFloatParameter(0.1f, 0, 1);
        public ClampedFloatParameter        rayMaxScreenDistance = new ClampedFloatParameter(0.3f, 0, 1);
        public ClampedFloatParameter        rayBlendScreenDistance = new ClampedFloatParameter(0.1f, 0, 1);
        public BoolParameter                rayMarchBehindObjects = new BoolParameter(true);

        public virtual void PushShaderParameters(CommandBuffer cmd)
        {
            cmd.SetGlobalInt(m_RayLevelID, rayLevel.value);
            cmd.SetGlobalInt(m_RayMinLevelID, rayMinLevel.value);
            cmd.SetGlobalInt(m_RayMaxLevelID, rayMaxLevel.value);
            cmd.SetGlobalInt(m_RayMaxIterationsID, rayMaxIterations.value);
            cmd.SetGlobalFloat(m_DepthBufferThicknessID, depthBufferThickness.value);
            cmd.SetGlobalFloat(m_InvScreenWeightDistanceID, 1f / screenWeightDistance.value);
            cmd.SetGlobalFloat(m_RayMaxScreenDistanceID, rayMaxScreenDistance.value);
            cmd.SetGlobalFloat(m_RayBlendScreenDistanceID, rayBlendScreenDistance.value);
            cmd.SetGlobalInt(m_RayMarchBehindObjectsID, rayMarchBehindObjects.value ? 1 : 0);
        }

        protected abstract void FetchIDs(
            out int rayLevelID,
            out int rayMinLevelID,
            out int rayMaxLevelID,
            out int rayMaxIterationsID,
            out int DepthBufferThicknessID,
            out int invScreenWeightDistanceID,
            out int rayMaxScreenDistanceID,
            out int rayBlendScreenDistanceID,
            out int rayMarchBehindObjectsID
            );

        void Awake()
        {
            FetchIDs(
                out m_RayLevelID,
                out m_RayMinLevelID,
                out m_RayMaxLevelID,
                out m_RayMaxIterationsID,
                out m_DepthBufferThicknessID,
                out m_InvScreenWeightDistanceID,
                out m_RayMaxScreenDistanceID,
                out m_RayBlendScreenDistanceID,
                out m_RayMarchBehindObjectsID
                );
        }
    }
}
