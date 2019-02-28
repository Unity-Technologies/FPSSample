using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Collision")]
    class CollisionSDF : CollisionBase
    {
        public override string name { get { return "Collider (Signed Distance Field)"; } }

        public class InputProperties
        {
            public Texture3D DistanceField = VFXResources.defaultResources.signedDistanceField;
            public OrientedBox FieldTransform = OrientedBox.defaultValue;
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var input in base.parameters)
                    yield return input;

                foreach (var input in GetExpressionsFromSlots(this))
                {
                    if (input.name == "FieldTransform")
                        yield return new VFXNamedExpression(new VFXExpressionInverseMatrix(input.exp), "InvFieldTransform");
                }
            }
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 nextPos = position + velocity * deltaTime;

float3 tPos = mul(InvFieldTransform, float4(nextPos,1.0f)).xyz;
float tRadius = radius * length(InvFieldTransform[0]); // Only uniform scale for SDF transform
float3 coord = saturate(tPos + 0.5f);
float dist = SampleSDF(DistanceField, coord) - colliderSign * tRadius;

if (colliderSign * dist <= 0.0f) // collision
{
    float3 n = SampleSDFDerivatives(DistanceField, coord);

    // back in system space
    float3 delta = colliderSign * mul(FieldTransform,float4(normalize(n) * abs(dist),0)).xyz;
    n = normalize(delta);
";

                Source += collisionResponseSource;

                if (mode == Mode.Inverted)
                {
                    Source += @"
    float3 absPos = abs(tPos);
    float outsideDist = max(absPos.x,max(absPos.y,absPos.z));
    if (outsideDist > 0.5f) // Check wether point is outside the box
        position = mul(FieldTransform,float4(coord - 0.5f,1)).xyz;
";
                }

                Source += @"
    position += delta;
}";
                return Source;
            }
        }
    }
}
