using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractSpawner : VFXBlock
    {
        public override VFXContextType compatibleContexts { get { return VFXContextType.kSpawner; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kSpawnEvent; } }
        public abstract VFXTaskType spawnerType { get; }
        public virtual Type customBehavior { get { return null; } }
    }
}
