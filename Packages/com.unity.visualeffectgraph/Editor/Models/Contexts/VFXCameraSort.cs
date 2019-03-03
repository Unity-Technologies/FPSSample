using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX.Block;

namespace UnityEditor.VFX
{
    class VFXCameraSort : VFXContext
    {
        public VFXCameraSort() : base(VFXContextType.kUpdate, VFXDataType.kParticle, VFXDataType.kParticle) {}
        public override string name { get { return "CameraSort"; } }
        public override string codeGeneratorTemplate { get { return VisualEffectGraphPackageInfo.assetPackagePath + "/Shaders/VFXCameraSort"; } }
        public override bool codeGeneratorCompute { get { return true; } }
        public override VFXTaskType taskType { get { return VFXTaskType.CameraSort; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
            }
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            var localSpace = ((VFXDataParticle)GetData()).space == VFXCoordinateSpace.Local;
            if (localSpace && target == VFXDeviceTarget.GPU) // Needs to add locaToWorld matrix
            {
                var gpuMapper = new VFXExpressionMapper();
                gpuMapper.AddExpression(VFXBuiltInExpression.LocalToWorld, "localToWorld", -1);
                return gpuMapper;
            }

            return null; // cpu
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                if (GetData().IsAttributeStored(VFXAttribute.Alive))
                    yield return "USE_DEAD_LIST_COUNT";
            }
        }
    }
}
