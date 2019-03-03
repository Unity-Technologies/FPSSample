using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Spawn")]
    class VFXSpawnerConstantRate : VFXAbstractSpawner
    {
        public override string name { get { return "Constant Spawn Rate"; } }
        public override VFXTaskType spawnerType { get { return VFXTaskType.ConstantRateSpawner; } }
        public class InputProperties
        {
            [Min(0), Tooltip("Spawn Rate (in number per seconds)")]
            public float Rate = 10;
        }
    }
}
