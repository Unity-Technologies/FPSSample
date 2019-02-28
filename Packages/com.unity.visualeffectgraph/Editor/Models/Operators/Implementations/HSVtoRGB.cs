using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Color")]
    class HSVtoRGB : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The Hue, Saturation and Value parameters.")]
            public Vector3 hsv = new Vector3(1.0f, 0.5f, 0.5f);
        }

        public class OutputProperties
        {
            public Vector4 rgb = Vector4.zero;
        }

        override public string name { get { return "HSV to RGB"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] rgb = VFXOperatorUtility.ExtractComponents(new VFXExpressionHSVtoRGB(inputExpression[0])).Take(3).ToArray();
            return new[] { new VFXExpressionCombine(new[] { rgb[0], rgb[1], rgb[2], VFXValue.Constant(1.0f) }) };
        }
    }
}
