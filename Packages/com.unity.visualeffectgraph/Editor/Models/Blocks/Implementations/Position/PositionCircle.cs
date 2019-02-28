using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionCircle : PositionBase
    {
        public override string name { get { return "Position (Circle)"; } }
        protected override float thicknessDimensions { get { return 2.0f; } }

        public class InputProperties
        {
            [Tooltip("The circle used for positioning particles.")]
            public ArcCircle ArcCircle = ArcCircle.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("When using customized emission, control the position around the arc to emit particles from.")]
            public float ArcSequencer = 0.0f;
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
                    outSource += @"float theta = ArcCircle_arc * RAND;";
                else
                    outSource += @"float theta = ArcCircle_arc * ArcSequencer;";

                outSource += @"
float rNorm = sqrt(volumeFactor + (1 - volumeFactor) * RAND);

float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);

direction = float3(sincosTheta, 0.0f);
position.xy += sincosTheta * rNorm * ArcCircle_circle_radius + ArcCircle_circle_center;
";

                return outSource;
            }
        }
    }
}
