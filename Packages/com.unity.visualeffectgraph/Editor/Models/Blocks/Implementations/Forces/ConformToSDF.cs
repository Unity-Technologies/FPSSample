using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Force")]
    class ConformToSDF : VFXBlock
    {
        public override string name { get { return "Conform to Signed Distance Field"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

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

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            }
        }

        public class InputProperties
        {
            public Texture3D DistanceField = VFXResources.defaultResources.signedDistanceField;
            public OrientedBox FieldTransform = OrientedBox.defaultValue;
            public float attractionSpeed = 5.0f;
            public float attractionForce = 20.0f;
            public float stickDistance = 0.1f;
            public float stickForce = 50.0f;
        }

        public override string source
        {
            get
            {
                return @"
float3 tPos = mul(InvFieldTransform, float4(position,1.0f)).xyz;
float3 coord = saturate(tPos + 0.5f);
float dist = SampleSDF(DistanceField, coord);

float3 absPos = abs(tPos);
float outsideDist = max(absPos.x,max(absPos.y,absPos.z));
float3 dir;
if (outsideDist > 0.5f) // Check wether point is outside the box
{
    // in that case just move towards center
    dist += outsideDist - 0.5f;
    dir = normalize(float3(FieldTransform[0][3],FieldTransform[1][3],FieldTransform[2][3]) - position);
}
else
{
    // compute normal
    dir = SampleSDFDerivativesFast(DistanceField, coord, dist);
    if (dist > 0)
        dir = -dir;
    dir = normalize(mul(FieldTransform,float4(dir,0)));
}

float distToSurface = abs(dist);

float spdNormal = dot(dir,velocity);
float ratio = smoothstep(0.0,stickDistance * 2.0,abs(distToSurface));
float tgtSpeed = sign(distToSurface) * attractionSpeed * ratio;
float deltaSpeed = tgtSpeed - spdNormal;
velocity += sign(deltaSpeed) * min(abs(deltaSpeed),deltaTime * lerp(stickForce,attractionForce,ratio)) * dir / mass ;";
            }
        }
    }
}
