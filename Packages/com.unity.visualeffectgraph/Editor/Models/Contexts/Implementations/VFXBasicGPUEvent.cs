using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXType]
    public struct GPUEvent
    {
        /* expected emptiness */
    };

    [VFXInfo(experimental = true)]
    class VFXBasicGPUEvent : VFXContext
    {
        public VFXBasicGPUEvent() : base(VFXContextType.kSpawnerGPU, VFXDataType.kNone, VFXDataType.kSpawnEvent) {}
        public override string name { get { return "GPUEvent"; } }

        public class InputProperties
        {
            public GPUEvent evt = new GPUEvent();
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            return new VFXExpressionMapper();
        }

        public override bool CanBeCompiled()
        {
            return outputContexts.Any(c => c.CanBeCompiled());
        }
    }
}
