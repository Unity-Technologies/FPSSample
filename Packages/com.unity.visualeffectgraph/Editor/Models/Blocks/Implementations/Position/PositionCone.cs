using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionCone : PositionBase
    {
        public enum HeightMode
        {
            Base,
            Volume
        }

        [VFXSetting, Tooltip("Controls whether particles are spawned on the base of the cone, or throughout the entire volume.")]
        public HeightMode heightMode;

        public override string name { get { return "Position (Cone)"; } }
        protected override float thicknessDimensions { get { return 2.0f; } }

        public class InputProperties
        {
            [Tooltip("The cone used for positioning particles.")]
            public ArcCone Cone = new ArcCone() { radius0 = 0.0f, radius1 = 1.0f, height = 0.5f, arc = Mathf.PI * 2.0f };
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("When using customized emission, control the position along the height to emit particles from.")]
            public float HeightSequencer = 0.0f;
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

                VFXExpression radius0 = inputSlots[0][1].GetExpression();
                VFXExpression radius1 = inputSlots[0][2].GetExpression();
                VFXExpression height = inputSlots[0][3].GetExpression();
                VFXExpression tanSlope = (radius1 - radius0) / height;
                VFXExpression slope = new VFXExpressionATan(tanSlope);
                if (spawnMode == SpawnMode.Randomized)
                    yield return new VFXNamedExpression(radius1 / tanSlope, "fullConeHeight");
                yield return new VFXNamedExpression(new VFXExpressionCombine(new VFXExpression[] { new VFXExpressionSin(slope), new VFXExpressionCos(slope) }), "sincosSlope");
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
                string outSource = "";

                if (spawnMode == SpawnMode.Randomized)
                    outSource += @"float theta = Cone_arc * RAND;";
                else
                    outSource += @"float theta = Cone_arc * ArcSequencer;";

                outSource += @"
float rNorm = sqrt(volumeFactor + (1 - volumeFactor) * RAND);

float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);

float2 pos = (sincosTheta * rNorm);

";

                if (heightMode == HeightMode.Base)
                {
                    outSource += @"
float hNorm = 0.0f;
float3 base = float3(pos * Cone_radius0, 0.0f);
";
                }
                else if (spawnMode == SpawnMode.Randomized)
                {
                    outSource += @"
float heightFactor = pow(Cone_radius0 / Cone_radius1, 3.0f);
float hNorm = pow(heightFactor + (1 - heightFactor) * RAND, 1.0f / 3.0f);
float3 base = float3(0.0f, 0.0f, Cone_height - fullConeHeight);
";
                }
                else
                {
                    outSource += @"
float hNorm = HeightSequencer;
float3 base = float3(0.0f, 0.0f, 0.0f);
";
                }

                outSource += @"
direction.xzy = normalize(float3(pos * sincosSlope.x, sincosSlope.y));
position.xzy += lerp(base, float3(pos * Cone_radius1, Cone_height), hNorm) + Cone_center.xzy;
";

                return outSource;
            }
        }
    }
}
