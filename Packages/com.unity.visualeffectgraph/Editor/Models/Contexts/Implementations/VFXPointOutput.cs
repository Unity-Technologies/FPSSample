using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXPointOutput : VFXAbstractParticleOutput
    {
        public override string name { get { return "Point Output"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticlePoints"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticlePointOutput; } }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                yield return "cullMode";
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            cullMode = CullMode.Off;
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);

                var asset = GetResource();
                if (asset != null && asset.rendererSettings.motionVectorGenerationMode == MotionVectorGenerationMode.Object)
                    yield return new VFXAttributeInfo(VFXAttribute.OldPosition, VFXAttributeMode.Read);
            }
        }
    }
}
