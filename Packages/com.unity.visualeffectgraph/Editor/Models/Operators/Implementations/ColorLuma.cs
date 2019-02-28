using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Color")]
    class ColorLuma : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The color used for the luminance calculation.")]
            public Color color = Color.white;
        }

        public class OutputProperties
        {
            [Tooltip("The luminance of the color.")]
            public float luma;
        }

        override public string name { get { return "Color Luma"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.ColorLuma(inputExpression[0]) };
        }
    }
}
