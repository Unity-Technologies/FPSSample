using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionTorus : PositionBase
    {
        public override string name { get { return "Position (Torus)"; } }
        protected override float thicknessDimensions { get { return 2.0f; } }

        public class InputProperties
        {
            [Tooltip("The torus used for positioning particles.")]
            public ArcTorus Torus = new ArcTorus() { majorRadius = 1.0f, minorRadius = 0.2f, arc = Mathf.PI * 2.0f };
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("When using customized emission, control the position around the arc to emit particles from.")]
            public float ArcSequencer = 0.0f;
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this).Where(e => e.name != "Thickness"))
                    yield return p;

                yield return new VFXNamedExpression(CalculateVolumeFactor(positionMode, 0, 1), "volumeFactor");
                yield return new VFXNamedExpression(VFXOperatorUtility.Saturate(inputSlots[0][2].GetExpression() / inputSlots[0][1].GetExpression()), "r"); // Saturate can be removed once degenerated torus are correctly handled
            }
        }

        protected override bool needDirectionWrite
        {
            get
            {
                return true;
            }
        }

        public override string source
        {
            get
            {
                string outSource = @"";
                if (spawnMode == SpawnMode.Randomized)
                {
                    outSource += @"float3 u = RAND3;";
                    outSource += @"float arc = Torus_arc;";
                }
                else
                {
                    outSource += @"float3 u = float3(RAND, 1.0f, RAND);";
                    outSource += @"float arc = Torus_arc * ArcSequencer;";
                }

                outSource += @"
float R = sqrt(volumeFactor + (1.0f - volumeFactor) * u.z);

float sinTheta,cosTheta;
sincos(u.x * UNITY_TWO_PI,sinTheta,cosTheta);

float2 s1_1 = R * r * float2(cosTheta, sinTheta) + float2(1,0);
float2 s1_2 = R * r * float2(-cosTheta, sinTheta) + float2(1,0);
float w = s1_1.x / (s1_1.x + s1_2.x);

float3 t;
float phi;
if (u.y < w)
{
    phi = arc * u.y / w;
    t = float3(s1_1.x, 0, s1_1.y);
}
else
{
    phi = arc * (u.y - w) / (1.0f - w);
    t = float3(s1_2.x, 0, s1_2.y);
}

float s,c;
sincos(phi,c,s);
float3 t2 = float3(c * t.x - s * t.y,c * t.y + s * t.x,t.z);

position += Torus_center + Torus_majorRadius * t2;
direction = t2;
";

                return outSource;
            }
        }
    }
}
