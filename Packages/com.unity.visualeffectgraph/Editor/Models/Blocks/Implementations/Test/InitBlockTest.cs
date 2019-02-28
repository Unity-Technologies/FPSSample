using System;
using UnityEngine;

namespace UnityEditor.VFX.Block.Test
{
    class InitBlockTest : VFXBlock
    {
        public override string name                         { get { return "Init Block"; }}
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kInit; } }
        public override VFXDataType compatibleData          { get { return VFXDataType.kParticle; } }
    }
}
