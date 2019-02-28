using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    //[VFXInfo(category = "Implicit")] //There's no way the user can meaningfully interact with them.
    class Reap : VFXBlock
    {
        public override string name { get { return "Reap"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.ReadWrite);
            }
        }

        public override string source
        {
            get
            {
                return "if(age > lifetime) { alive = false; }";
            }
        }
    }
}
