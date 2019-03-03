using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Attribute/Derived")]
    class AttributeMassFromVolume : VFXBlock
    {
        public override string name { get { return "Calculate Mass from Volume"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);
            }
        }

        public class InputProperties
        {
            [Tooltip("Particle density, measured in kg/dm^3")]
            public float Density = 1.0f;
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                yield return new VFXNamedExpression(inputSlots[0].GetExpression() * VFXValue.Constant(1000.0f), "Density");
            }
        }

        public override string source
        {
            get
            {
                return @"
float3 radius = size * float3(scaleX, scaleY, scaleZ);
float radiusCubed = radius.x * radius.y * radius.z * 0.125f;
mass = (4.0f / 3.0f) * UNITY_PI * radiusCubed * Density;
";
            }
        }
    }
}
