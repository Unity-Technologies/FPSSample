using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Velocity")]
    class VelocityDirection : VelocityBase
    {
        public override string name { get { return string.Format(base.name, "Direction"); } }
        protected override bool altersDirection { get { return true; } }

        public class InputProperties
        {
            [Tooltip("The direction of the velocity to add to the particles.")]
            public DirectionType Direction = new DirectionType() { direction = Vector3.forward };
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
                string outSource = speedComputeString + "\n";
                outSource += string.Format(directionFormatBlendSource, "Direction") + "\n";
                outSource += string.Format(velocityComposeFormatString, "direction * speed");
                return outSource;
            }
        }
    }
}
