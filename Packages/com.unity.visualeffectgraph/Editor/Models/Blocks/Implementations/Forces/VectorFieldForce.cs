using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Force")]
    class VectorFieldForce : VFXBlock
    {
        public class InputProperties
        {
            [Tooltip("The vector field used as a force for particles")]
            public Texture3D VectorField = VFXResources.defaultResources.vectorField;
            [Tooltip("The position, rotation and scale of the field")]
            public OrientedBox FieldTransform = OrientedBox.defaultValue;
            [Tooltip("Intensity of the field. Vectors are multiplied by the intensity")]
            public float Intensity = 1.0f;
        }

        [VFXSetting, SerializeField, Tooltip("Signed: Field data is used as is (typically for float formats)\nUnsigned Normalized: Field data are centered on gray and scaled/biased (typically for 8 bits per component formats)")]
        TextureDataEncoding DataEncoding = TextureDataEncoding.UnsignedNormalized;

        [VFXSetting, SerializeField]
        ForceMode Mode = ForceMode.Relative;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("True to consider the field to be closed. Particles outside the box will not be affected by the vector field, else wrap mode of the texture is used.")]
        bool ClosedField = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("True to conserve the magnitude of the field when the size of its box is changed.")]
        bool ConserveMagnitude = false;

        public override string name { get { return "Vector Field Force"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                foreach (var a in ForceHelper.attributes)
                    yield return a;

                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = PropertiesFromType(GetInputPropertiesTypeName());
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
                    if (input.name == "FieldTransform")
                        yield return new VFXNamedExpression(new VFXExpressionInverseMatrix(input.exp), "InvFieldTransform");
                    yield return input;
                }

                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }

        public override string source
        {
            get
            {
                string Source = "float3 vectorFieldCoord = mul(InvFieldTransform, float4(position,1.0f)).xyz;";

                if (ClosedField)
                    Source += @"
if (abs(vectorFieldCoord.x) > 0.5f || abs(vectorFieldCoord.y) > 0.5f || abs(vectorFieldCoord.z) > 0.5f)
    return;";

                Source += string.Format(@"

float3 value = SampleTexture(VectorField, vectorFieldCoord + 0.5f).xyz {0};"
                    , DataEncoding == TextureDataEncoding.UnsignedNormalized ? "* 2.0f - 1.0f" : "");

                if (ConserveMagnitude)
                    Source += @"
float sqrValueLength = dot(value,value);";

                Source += @"
value = mul(FieldTransform,float4(value,0.0f)).xyz;";

                if (ConserveMagnitude)
                    Source += @"
value *= sqrt(sqrValueLength / max(VFX_EPSILON,dot(value,value)));";

                Source += string.Format(@"

velocity += {0};", ForceHelper.ApplyForceString(Mode, "(value * Intensity)"));

                return Source;
            }
        }
    }
}
