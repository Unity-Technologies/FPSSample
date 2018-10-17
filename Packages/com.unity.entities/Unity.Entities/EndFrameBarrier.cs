using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Scripting;

namespace Unity.Entities
{
    [UpdateBefore(typeof(Initialization))]
    [Preserve]
    public class EndFrameBarrier : BarrierSystem
    {
    }
}
