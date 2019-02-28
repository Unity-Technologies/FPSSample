using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Collision")]
    class CollisionAABox : CollisionBase
    {
        public override string name { get { return "Collider (AABox)"; } }

        public class InputProperties
        {
            [Tooltip("The collision bounding box.")]
            public AABox box = new AABox() { size = Vector3.one };
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 nextPos = position + velocity * deltaTime;
float3 dir = nextPos - box_center;
float3 absDir = abs(dir);
float3 halfBoxSize = box_size * 0.5f + radius * colliderSign;
";

                if (mode == Mode.Solid)
                    Source += @"bool collision = all(absDir < halfBoxSize);";
                else
                    Source += @"bool collision = any(absDir > halfBoxSize);";

                Source += @"
if (collision)
{
    float3 distanceToEdge = (absDir - halfBoxSize);
    float3 absDistanceToEdge = abs(distanceToEdge);

    float3 n;

    if (absDistanceToEdge.x < absDistanceToEdge.y && absDistanceToEdge.x < absDistanceToEdge.z)
        n = float3(colliderSign * sign(dir.x), 0.0f, 0.0f);
    else if (absDistanceToEdge.y < absDistanceToEdge.z)
        n = float3(0.0f, colliderSign * sign(dir.y), 0.0f);
    else
        n = float3(0.0f, 0.0f, colliderSign * sign(dir.z));
    ";
                if (mode == Mode.Solid)
                    Source += @"position -= n * distanceToEdge;";
                else
                    Source += @"position -= sign(dir) * max(0, distanceToEdge);";

                Source += collisionResponseSource;
                Source += @"
}";
                return Source;
            }
        }
    }
}
