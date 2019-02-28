using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Attribute")]
    class AgeOverLifetime : VFXOperator
    {
        public class OutputProperties
        {
            public float t = 0;
        }

        public override string name
        {
            get
            {
                return "Age Over Lifetime [0..1]";
            }
        }
        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] output = new VFXExpression[] { new VFXAttributeExpression(VFXAttribute.Age) / new VFXAttributeExpression(VFXAttribute.Lifetime) };
            return output;
        }
    }
}
