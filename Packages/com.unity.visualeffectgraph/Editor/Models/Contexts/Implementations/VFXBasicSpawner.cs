using System.Linq;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicSpawner : VFXContext
    {
        public VFXBasicSpawner() : base(VFXContextType.kSpawner, VFXDataType.kSpawnEvent, VFXDataType.kSpawnEvent) {}
        public override string name { get { return "Spawn"; } }

        protected override int inputFlowCount
        {
            get
            {
                return 2;
            }
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            if (target == VFXDeviceTarget.CPU)
                return VFXExpressionMapper.FromContext(this);

            return null;
        }

        public override bool CanBeCompiled()
        {
            return outputContexts.Any(c => c.CanBeCompiled());
        }
    }
}
