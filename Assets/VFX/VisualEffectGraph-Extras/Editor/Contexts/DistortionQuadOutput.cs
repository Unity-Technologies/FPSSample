using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor.VFX;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX.Block;

[VFXInfo]
class DistortionQuadOutput : VFXAbstractParticleOutput
{
    public class InputProperties
    {
        public Texture2D DistortionMap;
        public Vector2 Intensity;
        public float Roughness;
    }

    public class InputPropertiesFlipbook
    {
        public Texture2D DistortionMap;
        public Vector2 Intensity;
        public float Roughness;
        public Vector2 flipBookSize = new Vector2(5, 5);
    }

    public override string name { get { return "Distortion Quad"; } }

    public override string codeGeneratorTemplate { get { return "Assets/VFX/VisualEffectGraph-Extras/Editor/Templates/DistortionQuad/DistortionQuad"; } }

    public override bool supportsUV { get { return true; } }

    public override VFXTaskType taskType { get { return  VFXTaskType.ParticleQuadOutput; } }

    public override IEnumerable<VFXAttributeInfo> attributes
    {
        get
        {
            yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.PivotX, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.PivotZ, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);

            if (uvMode == UVMode.Flipbook || uvMode == UVMode.FlipbookBlend)
                yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.Read);
        }
    }

    protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
    {
        foreach (var exp in base.CollectGPUExpressions(slotExpressions))
            yield return exp;

        yield return slotExpressions.First(o => o.name == "DistortionMap");
        yield return slotExpressions.First(o => o.name == "Intensity");
        yield return slotExpressions.First(o => o.name == "Roughness");
    }

    protected override IEnumerable<string> filteredOutSettings
    {
        get
        {
            foreach (var setting in base.filteredOutSettings)
                yield return setting;

            yield return "blendMode";
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        blendMode = BlendMode.Additive;
    }

}
