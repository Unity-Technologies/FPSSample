using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Velocity")]
    class VelocitySpherical : VelocityBase
    {
        public override string name { get { return string.Format(base.name, "Spherical"); } }
        protected override bool altersDirection {  get { return true; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                foreach (var attribute in base.attributes)
                    yield return attribute;

                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
            }
        }

        public class InputProperties
        {
            [Tooltip("The center of the spherical direction.")]
            public Vector3 center;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var property in PropertiesFromType("InputProperties"))
                    yield return property;

                foreach (var property in base.inputProperties)
                    yield return property;
            }
        }

        public override string source
        {
            get
            {
                string outSource = "float3 sphereDirection = VFXSafeNormalize(position - center);\n";
                outSource += speedComputeString + "\n";
                outSource += string.Format(directionFormatBlendSource, "sphereDirection") + "\n";
                outSource += string.Format(velocityComposeFormatString, "direction * speed");
                return outSource;
            }
        }
    }
}
