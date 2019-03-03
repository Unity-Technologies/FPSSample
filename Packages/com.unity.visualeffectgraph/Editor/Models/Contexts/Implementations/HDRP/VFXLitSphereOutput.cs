using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXLitSphereOutput : VFXAbstractParticleHDRPLitOutput
    {
        public override string name { get { return "Lit Sphere Output"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleSphere"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleQuadOutput; } }

        protected override bool allowTextures { get { return false; } }

        public override void OnEnable()
        {
            blendMode = BlendMode.Opaque; // TODO use masked
            doubleSided = false;
            base.OnEnable();
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                if (colorMode != ColorMode.None)
                    yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);

                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXBlock> implicitPostBlock
        {
            get
            {
                var orient = VFXBlock.CreateImplicitBlock<Orient>(GetData());
                orient.mode = Orient.Mode.FaceCameraPosition;
                yield return orient;
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                yield return "cullMode";
                yield return "blendMode";
                yield return "doubleSided";
            }
        }
    }
}
