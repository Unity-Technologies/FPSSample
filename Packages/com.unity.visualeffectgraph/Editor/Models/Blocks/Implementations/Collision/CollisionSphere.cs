using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Collision")]
    class CollisionSphere : CollisionBase
    {
        public override string name { get { return "Collider (Sphere)"; } }

        public class InputProperties
        {
            [Tooltip("The collision sphere.")]
            public Sphere Sphere = new Sphere() { radius = 1.0f };
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 nextPos = position + velocity * deltaTime;
float3 dir = nextPos - Sphere_center;
float sqrLength = dot(dir, dir);
float totalRadius = Sphere_radius + colliderSign * radius;
if (colliderSign * sqrLength <= colliderSign * totalRadius * totalRadius)
{
    float dist = sqrt(sqrLength);
    float3 n = colliderSign * dir / dist;
    position -= n * (dist - totalRadius) * colliderSign;
";

                Source += collisionResponseSource;
                Source += @"
}";
                return Source;
            }
        }
    }
}
