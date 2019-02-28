using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXLineOutput : VFXAbstractParticleOutput
    {
        public override string name { get { return "Line Output"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate(useNativeLines ? "VFXParticleLinesHW" : "VFXParticleLinesSW"); } }
        public override VFXTaskType taskType { get { return useNativeLines ? VFXTaskType.ParticleLineOutput : VFXTaskType.ParticleQuadOutput; } }

        [VFXSetting, SerializeField]
        protected bool targetFromAttributes = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool useNativeLines = false;

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
                yield return new VFXAttributeInfo(VFXAttribute.Color,           VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha,           VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive,           VFXAttributeMode.Read);

                if (targetFromAttributes)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.PivotX, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.PivotZ, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);

                    yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);

                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                    yield return new VFXAttributeInfo(VFXAttribute.TargetPosition, VFXAttributeMode.Write);
                }
                else
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.TargetPosition, VFXAttributeMode.Read);
                }
            }
        }

        public class TargetFromAttributesProperties
        {
            public Vector3 targetOffset = Vector3.up;
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (targetFromAttributes)
                yield return slotExpressions.First(o => o.name == "targetOffset");
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;
                if (targetFromAttributes)
                    properties = PropertiesFromType("TargetFromAttributesProperties").Concat(properties);

                return properties;
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                if (targetFromAttributes)
                    yield return "TARGET_FROM_ATTRIBUTES";
            }
        }
    }
}
