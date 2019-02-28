using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXCubeTestOutput : VFXAbstractParticleOutput
    {
        public override string name { get { return "Cube test Output"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleCube"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleHexahedronOutput; } }

        [VFXSetting, SerializeField]
        bool useRimLight = false;

        [VFXSetting, SerializeField]
        bool useNormalMap = false;

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
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
                yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (useRimLight)
            {
                yield return slotExpressions.First(o => o.name == "rimColor");
                yield return slotExpressions.First(o => o.name == "rimCoef");

                if (useNormalMap)
                    yield return slotExpressions.First(o => o.name == "normalMap");
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;
                if (useRimLight)
                {
                    properties = properties.Concat(PropertiesFromType("RimLightInputProperties"));
                    if (useNormalMap)
                        properties = properties.Concat(PropertiesFromType("NormalInputProperties"));
                }
                return properties;
            }
        }

        public class NormalInputProperties
        {
            public Texture2D normalMap = null;
        }

        public class RimLightInputProperties
        {
            public Color rimColor = Color.clear;
            public float rimCoef = 0;
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                if (useRimLight)
                {
                    yield return "VFX_USE_RIM_LIGHT";
                    if (useNormalMap)
                        yield return "VFX_USE_NORMAL_MAP";
                }
            }
        }


        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                if (!useRimLight)
                    yield return "useNormalMap";
            }
        }
    }
}
