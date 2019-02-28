using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Collision")]
    class CollisionCylinder : CollisionBase
    {
        public override string name { get { return "Collider (Cylinder)"; } }

        public class InputProperties
        {
            [Tooltip("The collision cylinder.")]
            public Cylinder Cylinder = new Cylinder() { height = 1.0f, radius = 0.5f };
        }

        private string collisionTestSource
        {
            get
            {
                if (mode == Mode.Solid)
                    return @"
bool collision = abs(dir.y) < halfHeight && sqrLength < cylinderRadius * cylinderRadius;
";
                else
                    return @"
bool collision = abs(dir.y) > halfHeight || sqrLength > cylinderRadius * cylinderRadius;
";
            }
        }

        private string normalAndPushSource
        {
            get
            {
                if (mode == Mode.Solid)
                    return @"
    n *= distToSide < distToCap ? float3(1,0,1) : float3(0,1,0);
    position += n * min(distToSide,distToCap);
";
                else
                    return @"
    position += n * float3(max(0,distToSide).xx,max(0,distToCap)).xzy;
    n *= distToSide > distToCap ? float3(1,0,1) : float3(0,1,0);
";
            }
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 nextPos = position + velocity * deltaTime;
float3 dir = nextPos - Cylinder_center;
const float halfHeight = Cylinder_height * 0.5f + radius * colliderSign;
const float cylinderRadius = Cylinder_radius + radius * colliderSign;
float sqrLength = dot(dir.xz, dir.xz);
";

                Source += collisionTestSource;
                Source += @"
if (collision)
{
    float dist = sqrt(sqrLength);
    float distToCap = colliderSign * (halfHeight - abs(dir.y));
    float distToSide = colliderSign * (cylinderRadius - dist);

    float3 n = colliderSign * float3(dir.xz / dist, sign(dir.y)).xzy;
";

                Source += normalAndPushSource;
                Source += collisionResponseSource;
                Source += @"
}";
                return Source;
            }
        }
    }
}
