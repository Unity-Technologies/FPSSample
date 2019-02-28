using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class CircleArea : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The circle used for the area calculation.")]
            public Circle circle = new Circle();
        }

        public class OutputProperties
        {
            [Tooltip("The area of the circle.")]
            public float area;
        }

        override public string name { get { return "Area (Circle)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.CircleArea(inputExpression[1]) };
        }
    }
}
