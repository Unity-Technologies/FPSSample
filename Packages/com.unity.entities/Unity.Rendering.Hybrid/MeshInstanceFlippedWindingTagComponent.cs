using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace Unity.Rendering
{
    public struct MeshInstanceFlippedWindingTag : IComponentData
    {
    }

	public class MeshInstanceFlippedWindingTagComponent : ComponentDataWrapper<MeshInstanceFlippedWindingTag> { }
}
