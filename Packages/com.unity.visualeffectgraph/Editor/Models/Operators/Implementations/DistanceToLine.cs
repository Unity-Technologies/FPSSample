using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class DistanceToLine : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The line used for the distance calculation.")]
            public Line line = new Line();
            [Tooltip("The position used for the distance calculation.")]
            public Position position = new Position();
        }

        public class OutputProperties
        {
            [Tooltip("The closest point on the line to the supplied position.")]
            public Vector3 closestPosition;
            [Tooltip("The unsigned distance from the line.")]
            public float distance;
        }

        override public string name { get { return "Distance (Line)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression lineDelta = (inputExpression[1] - inputExpression[0]);
            VFXExpression lineLength = new VFXExpressionMax(VFXOperatorUtility.Dot(lineDelta, lineDelta), VFXValue.Constant(Mathf.Epsilon));
            VFXExpression t = VFXOperatorUtility.Dot(inputExpression[2] - inputExpression[0], lineDelta);

            t = VFXOperatorUtility.Clamp(t / lineLength, VFXValue.Constant(0.0f), VFXValue.Constant(1.0f));

            VFXExpression pointOnLine = (inputExpression[0] + VFXOperatorUtility.CastFloat(t, lineDelta.valueType) * lineDelta);
            VFXExpression lineDistance = VFXOperatorUtility.Distance(inputExpression[2], pointOnLine);
            return new VFXExpression[] { pointOnLine, lineDistance };
        }
    }
}
