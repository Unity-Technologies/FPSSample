using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Velocity")]
    class VelocityRandomize : VelocityBase
    {
        public override string name { get { return string.Format(base.name, "Random Direction"); } }
        protected override bool altersDirection { get { return true; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                foreach (var attribute in base.attributes)
                    yield return attribute;

                // we need to add seed only if it's not already present
                if (speedMode == SpeedMode.Constant)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
            }
        }

        public override string source
        {
            get
            {
                string outSource = "float3 randomDirection = normalize(RAND3 * 2.0f - 1.0f);\n";
                outSource += speedComputeString + "\n";
                outSource += string.Format(directionFormatBlendSource, "randomDirection") + "\n";
                outSource += string.Format(velocityComposeFormatString, "direction * speed");
                return outSource;
            }
        }
    }
}
