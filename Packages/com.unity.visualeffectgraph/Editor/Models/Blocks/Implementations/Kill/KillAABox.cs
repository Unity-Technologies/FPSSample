using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Kill")]
    class KillAABox : VFXBlock
    {
        [VFXSetting]
        public CollisionBase.Mode mode = CollisionBase.Mode.Solid;

        public override string name { get { return "Kill (AABox)"; } }

        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public class InputProperties
        {
            [Tooltip("The kill bounding box.")]
            public AABox box = new AABox() { size = Vector3.one };
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Write);
            }
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 dir = position - box_center;
float3 absDir = abs(dir);
float3 size = box_size * 0.5f;
";

                if (mode == CollisionBase.Mode.Solid)
                    Source += @"bool collision = all(absDir <= size);";
                else
                    Source += @"bool collision = any(absDir >= size);";

                Source += @"
if (collision)
    alive = false;";

                return Source;
            }
        }
    }
}
