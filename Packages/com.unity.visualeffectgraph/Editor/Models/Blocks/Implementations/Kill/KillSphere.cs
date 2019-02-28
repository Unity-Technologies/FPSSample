using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Kill")]
    class KillSphere : VFXBlock
    {
        [VFXSetting]
        public CollisionBase.Mode mode = CollisionBase.Mode.Solid;

        public override string name { get { return "Kill (Sphere)"; } }

        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Write);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this))
                    yield return p;

                if (mode == CollisionBase.Mode.Solid)
                    yield return new VFXNamedExpression(VFXValue.Constant(1.0f), "colliderSign");
                else
                    yield return new VFXNamedExpression(VFXValue.Constant(-1.0f), "colliderSign");
            }
        }

        public class InputProperties
        {
            [Tooltip("The killing sphere.")]
            public Sphere Sphere = new Sphere() { radius = 1.0f };
        }

        public override string source
        {
            get
            {
                return @"
float3 dir = position - Sphere_center;
float sqrLength = dot(dir, dir);
if (colliderSign * sqrLength <= colliderSign * Sphere_radius * Sphere_radius)
    alive = false;";
            }
        }
    }
}
