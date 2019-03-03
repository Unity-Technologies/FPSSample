using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicInitialize : VFXContext
    {
        [VFXSetting, Delayed]
        private uint capacity = 0; // not serialized here but in VFXDataParticle

        public VFXBasicInitialize() : base(VFXContextType.kInit, VFXDataType.kSpawnEvent, VFXDataType.kParticle) {}
        public override string name { get { return "Initialize"; } }
        public override string codeGeneratorTemplate { get { return VisualEffectGraphPackageInfo.assetPackagePath + "/Shaders/VFXInit"; } }
        public override bool codeGeneratorCompute { get { return true; } }
        public override VFXTaskType taskType { get { return VFXTaskType.Initialize; } }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                if (inputContexts.Any(o => o.contextType == VFXContextType.kSpawnerGPU))
                {
                    yield return "VFX_USE_SPAWNER_FROM_GPU";
                }
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            capacity = ((VFXDataParticle)GetData()).capacity;
            GetData().onModified += DataModified;
        }

        protected void OnDisable()
        {
            GetData().onModified -= DataModified;
        }

        void DataModified(VFXObject o)
        {
            capacity = ((VFXDataParticle)o).capacity;
        }

        public override void OnDataChanges(VFXData oldData, VFXData newData)
        {
            if(oldData != null)
                oldData.onModified -= DataModified;
            base.OnDataChanges(oldData, newData);
            if( newData != null)
                newData.onModified += DataModified;
            DataModified(newData);
        }


        protected override void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (model == this && cause == VFXModel.InvalidationCause.kSettingChanged)
                ((VFXDataParticle)GetData()).capacity = capacity;

            base.OnInvalidate(model, cause);
        }

        public class InputProperties
        {
            public AABox bounds = new AABox() { size = Vector3.one };
        }

        public sealed override VFXCoordinateSpace GetOutputSpaceFromSlot(VFXSlot slot)
        {
            if (slot.name == "bounds")
                return VFXCoordinateSpace.Local;
            return base.GetOutputSpaceFromSlot(slot);
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            // GPU
            if (target == VFXDeviceTarget.GPU)
                return VFXExpressionMapper.FromBlocks(activeChildrenWithImplicit);

            // CPU
            var cpuMapper = new VFXExpressionMapper();
            cpuMapper.AddExpressionFromSlotContainer(this, -1);
            return cpuMapper;
        }
    }
}
