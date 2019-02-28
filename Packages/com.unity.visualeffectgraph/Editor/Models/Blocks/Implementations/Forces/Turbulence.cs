using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Force")]
    class Turbulence : VFXBlock
    {
        public class InputProperties
        {
            [Tooltip("The position, rotation and scale of the turbulence field")]
            public Transform FieldTransform = Transform.defaultValue;
            [Tooltip("Number of Octaves of the noise (Max 10)")]
            public uint NumOctaves = 3;
            [Range(0.0f, 1.0f), Tooltip("The roughness of the turbulence")]
            public float Roughness = 0.5f;
            [Tooltip("Intensity of the motion vectors")]
            public float Intensity = 1.0f;
        }

        [VFXSetting, SerializeField]
        ForceMode Mode = ForceMode.Relative;

        public override string name { get { return "Turbulence"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;
                if (Mode == ForceMode.Relative)
                    properties = properties.Concat(PropertiesFromType(typeof(ForceHelper.DragProperties)));
                return properties;
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var input in GetExpressionsFromSlots(this))
                {
                    if (input.name == "NumOctaves") continue;

                    if (input.name == "FieldTransform")
                        yield return new VFXNamedExpression(new VFXExpressionInverseMatrix(input.exp), "InvFieldTransform");
                    yield return input;
                }

                // Clamp (1..10) for octaves (TODO: Add a Range attribute that works with int instead of doing that
                yield return new VFXNamedExpression(new VFXExpressionCastFloatToUint(VFXOperatorUtility.Clamp(new VFXExpressionCastUintToFloat(inputSlots[1].GetExpression()), VFXValue.Constant(1.0f), VFXValue.Constant(10.0f))), "octaves");
                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }

        public override string source
        {
            get
            {
                return string.Format(
 @"float3 vectorFieldCoord = mul(InvFieldTransform, float4(position,1.0f)).xyz;

float3 value = Noise3D(vectorFieldCoord + 0.5f, octaves, Roughness);
value = mul(FieldTransform,float4(value,0.0f)).xyz * Intensity;

velocity += {0};", ForceHelper.ApplyForceString(Mode, "value"));
            }
        }
    }
}
