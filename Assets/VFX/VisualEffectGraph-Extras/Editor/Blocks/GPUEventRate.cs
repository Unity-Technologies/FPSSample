using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "GPUEvent", experimental = true)]
    class GPUEventRate : VFXBlock
    {
        public enum Mode
        {
            OverTime,
            OverDistance
        }

        [VFXSetting]
        public Mode mode = Mode.OverTime;

        public override string name { get { return string.Format("Trigger GPUEvent Rate({0})", ObjectNames.NicifyVariableName(mode.ToString())); } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(new VFXAttribute("rateCount", VFXValueType.Float), VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.EventCount, VFXAttributeMode.Write);

                if (mode == Mode.OverDistance)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);
                }
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var parameter in base.parameters)
                    yield return parameter;

                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");

            }
        }

        public class InputProperties
        {
            public float Rate = 10.0f;
        }

        public class OutputProperties
        {
            public GPUEvent evt = new GPUEvent();
        }

        public override string source
        {
            get
            {
                string outSource = "";

                switch(mode)
                {
                    case Mode.OverDistance:
                        outSource += "rateCount += length(velocity) * deltaTime * Rate;";
                        break;
                    case Mode.OverTime:
                        outSource += "rateCount += deltaTime * Rate;";
                        break;
                }

                outSource += @"
uint count = floor(rateCount);
rateCount = frac(rateCount);
eventCount = count;";
                return outSource;
            }
        }
    }
}

