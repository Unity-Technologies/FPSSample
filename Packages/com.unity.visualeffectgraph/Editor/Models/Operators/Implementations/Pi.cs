using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Constants")]
    class Pi : VFXOperator
    {
        override public string libraryName { get { return "Pi (π)"; } }
        override public string name
        {
            get
            {
                int nbLinkedSlots = outputSlots.Count(o => o.HasLink());

                if (nbLinkedSlots == 1)
                {
                    if (GetOutputSlot(0).HasLink())
                        return "Pi (π)";
                    else if (GetOutputSlot(1).HasLink())
                        return "Pi (2π)";
                    else if (GetOutputSlot(2).HasLink())
                        return "Pi (π/2)";
                    else
                        return "Pi (π/3)";
                }

                return "Pi (π)";
            }
        }

        public class OutputProperties
        {
            public float Pi = Mathf.PI;
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "π"));
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "2π"));
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "π/2"));
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "π/3"));
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[]
            {
                VFXValue.Constant(Mathf.PI),
                VFXValue.Constant(2 * Mathf.PI),
                VFXValue.Constant(Mathf.PI / 2.0f),
                VFXValue.Constant(Mathf.PI / 3.0f)
            };
        }
    }
}
