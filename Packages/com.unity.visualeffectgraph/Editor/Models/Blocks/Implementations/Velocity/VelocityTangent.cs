using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Velocity")]
    class VelocityTangent : VelocityBase
    {
        public override string name { get { return string.Format(base.name, "Tangent"); } }
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
            [Tooltip("The axis of rotation.")]
            public Line axis = Line.defaultValue;
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

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var param in base.parameters)
                {
                    if (param.name == "axis_end")
                        continue;

                    yield return param;
                }

                var start = base.parameters.First(o => o.name == "axis_start").exp;
                var end = base.parameters.First(o => o.name == "axis_end").exp;

                yield return new VFXNamedExpression(VFXOperatorUtility.Normalize(end - start), "normalizedDir");
            }
        }

        public override string source
        {
            get
            {
                string outSource = @"float3 dir = normalizedDir; // normalize(axis_end - axis_start);
float3 projPos = (dot(dir, position - axis_start) * dir) + axis_start;
float3 tangentDirection = cross(normalize(position - projPos), dir);
";
                outSource += speedComputeString + "\n";
                outSource += string.Format(directionFormatBlendSource, "tangentDirection") + "\n";
                outSource += string.Format(velocityComposeFormatString, "direction * speed");
                return outSource;
            }
        }
    }
}
