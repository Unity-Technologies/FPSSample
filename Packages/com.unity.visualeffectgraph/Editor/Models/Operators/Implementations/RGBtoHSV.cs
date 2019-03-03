using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Color")]
    class RGBtoHSV : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The color to be converted to HSV.")]
            public Color color = Color.white;
        }

        public class OutputProperties
        {
            public Vector3 hsv = Vector3.zero;
        }

        override public string name { get { return "RGB to HSV"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var components = VFXOperatorUtility.ExtractComponents(inputExpression[0]);
            VFXExpression rgb = new VFXExpressionCombine(components.Take(3).ToArray());

            return new[] { new VFXExpressionRGBtoHSV(rgb) };
        }
    }
}
