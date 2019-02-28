using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    public class VFXCustomSpawnerTest : VFXSpawnerCallbacks
    {
        static public float s_SpawnCount = 101.0f;
        static public float s_LifeTime = 17.0f;

        public class InputProperties
        {
            public float totalTime = 8;
        }

        public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            state.spawnCount = s_SpawnCount;
            state.totalTime = vfxValues.GetFloat("totalTime");
            state.vfxEventAttribute.SetFloat("lifetime", s_LifeTime);
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }
    }
}
