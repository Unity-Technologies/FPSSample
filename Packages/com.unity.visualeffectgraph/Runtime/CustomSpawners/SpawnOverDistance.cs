using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [UnityEngine.Scripting.Preserve]
    public class SpawnOverDistance : VFXSpawnerCallbacks
    {
        public class InputProperties
        {
            public Vector3 Position;
            public float RatePerUnit = 10.0f;
            public float VelocityThreshold = 50.0f;
        }

        private Vector3 m_OldPosition;

        static private readonly int positionPropertyId = Shader.PropertyToID("Position");
        static private readonly int ratePerUnitPropertyId = Shader.PropertyToID("RatePerUnit");
        static private readonly int velocityThresholdPropertyId = Shader.PropertyToID("VelocityThreshold");

        static private readonly int positionAttributeId = Shader.PropertyToID("position");
        static private readonly int oldPositionAttributeId = Shader.PropertyToID("oldPosition");

        public sealed override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            m_OldPosition = vfxValues.GetVector3(positionPropertyId);
        }

        private float cachedSqrThreshold;
        private float cachedRatePerSqrUnit;

        public sealed override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            cachedSqrThreshold = vfxValues.GetFloat(velocityThresholdPropertyId);
            cachedSqrThreshold *= cachedSqrThreshold;

            cachedRatePerSqrUnit = vfxValues.GetFloat(ratePerUnitPropertyId);
            cachedRatePerSqrUnit *= cachedRatePerSqrUnit;

            if (!state.playing || state.deltaTime == 0) return;

            Vector3 pos = vfxValues.GetVector3(positionPropertyId);
            float sqrDistance = Vector3.SqrMagnitude(m_OldPosition - pos);
            if (sqrDistance < cachedSqrThreshold * state.deltaTime)
            {
                state.spawnCount += sqrDistance * cachedRatePerSqrUnit;

                state.vfxEventAttribute.SetVector3(oldPositionAttributeId, m_OldPosition);
                state.vfxEventAttribute.SetVector3(positionAttributeId, pos);
            }
            m_OldPosition = pos;
        }

        public sealed override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }
    }
}
