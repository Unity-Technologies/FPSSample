using UnityEngine;
using System.Collections;

public class PhysicsUtils
{
    public static Vector3 GetClosestPointOnCollider(Collider c, Vector3 p)
    {
        if (c is SphereCollider)
        {
            var csc = c as SphereCollider;

            var scale = csc.transform.localScale;
            return c.transform.position + (p - c.transform.position).normalized * csc.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }
        else if (c is BoxCollider)
        {
            var cbc = c as BoxCollider;
            var local_p = cbc.transform.InverseTransformPoint(p);

            local_p -= cbc.center;

            var minsize = -0.5f * cbc.size;
            var maxsize = 0.5f * cbc.size;

            local_p.x = Mathf.Clamp(local_p.x, minsize.x, maxsize.x);
            local_p.y = Mathf.Clamp(local_p.y, minsize.y, maxsize.y);
            local_p.z = Mathf.Clamp(local_p.z, minsize.z, maxsize.z);

            local_p += cbc.center;

            return cbc.transform.TransformPoint(local_p);
        }
        else if (c is CapsuleCollider)
        {
            // TODO: Only supports Y axis based capsules now
            var ccc = c as CapsuleCollider;
            var local_p = ccc.transform.InverseTransformPoint(p);
            local_p -= ccc.center;

            // Clamp inside outer cylinder top/bot
            local_p.y = Mathf.Clamp(local_p.y, -ccc.height * 0.5f, ccc.height * 0.5f);

            // Clamp to cylinder edge
            Vector2 h = new Vector2(local_p.x, local_p.z);
            h = h.normalized * ccc.radius;
            local_p.x = h.x;
            local_p.z = h.y;

            // Capsule ends
            float dist_to_top = ccc.height * 0.5f - Mathf.Abs(local_p.y);
            if (dist_to_top < ccc.radius)
            {
                float f = (ccc.radius - dist_to_top) / ccc.radius;
                float scaledown = Mathf.Sqrt(1.0f - f * f);
                local_p.x *= scaledown;
                local_p.z *= scaledown;
            }

            local_p += ccc.center;
            return ccc.transform.TransformPoint(local_p);
        }
        else
        {
            return c.ClosestPointOnBounds(p);
        }
    }
}
