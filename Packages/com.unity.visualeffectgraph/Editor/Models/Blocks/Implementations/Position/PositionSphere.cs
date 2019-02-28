using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionSphere : PositionBase
    {
        public override string name { get { return "Position (Sphere)"; } }

        public class InputProperties
        {
            [Tooltip("The sphere used for positioning particles.")]
            public ArcSphere ArcSphere = ArcSphere.defaultValue;
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
                string outSource = @"float cosPhi = 2.0f * RAND - 1.0f;";
                if (spawnMode == SpawnMode.Randomized)
                    outSource += @"float theta = ArcSphere_arc * RAND;";
                else
                    outSource += @"float theta = ArcSphere_arc * ArcSequencer;";

                outSource += @"
float rNorm = pow(volumeFactor + (1 - volumeFactor) * RAND, 1.0f / 3.0f);

float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);
sincosTheta *= sqrt(1.0f - cosPhi * cosPhi);

direction = float3(sincosTheta, cosPhi);
position += direction * (rNorm * ArcSphere_sphere_radius) + ArcSphere_sphere_center;
";

                return outSource;
            }
        }
    }
}
