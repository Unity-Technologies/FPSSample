using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Coordinates")]
    class PolarToRectangular : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The angular coordinate (Polar angle).")]
            public float angle = 45.0f;
            [Tooltip("The radial coordinate (Radius).")]
            public float distance = 1.0f;
        }

        public class OutputProperties
        {
            public Vector2 coord = Vector2.zero;
        }

        override public string name { get { return "Polar to Rectangular"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.PolarToRectangular(VFXOperatorUtility.DegToRad(inputExpression[0]), inputExpression[1]) };
        }
    }
}
