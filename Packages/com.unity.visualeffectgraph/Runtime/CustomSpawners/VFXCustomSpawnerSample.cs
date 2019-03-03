using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    public class VFXCustomSpawnerSample : VFXSpawnerCallbacks
    {
        public class InputProperties
        {
            public float dummyX = 2;
            public float dummyY = 1;
            public Gradient dummyZ = new Gradient();
        }

        public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            var a = vfxValues.GetGradient("dummyZ");
            if (a != null)
            {
                state.spawnCount = 123.0f;
            }
            else
            {
                state.spawnCount = 456.0f;
            }
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }
    }
}
