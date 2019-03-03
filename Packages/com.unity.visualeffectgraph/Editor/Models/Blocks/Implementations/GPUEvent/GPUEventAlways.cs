using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block.Test
{
    [VFXInfo(category = "GPUEvent", experimental = true)]
    class GPUEventAlways : VFXBlock
    {
        public override string name { get { return "Trigger Event Always"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.EventCount, VFXAttributeMode.Write);
            }
        }

        public class InputProperties
        {
            public uint count = 1u;
        }

        public class OutputProperties
        {
            public GPUEvent evt = new GPUEvent();
        }

        public override string source
        {
            get
            {
                return "eventCount = count;";
            }
        }
    }
}
