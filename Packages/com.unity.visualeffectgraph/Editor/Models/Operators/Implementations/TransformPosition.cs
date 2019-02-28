using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class TransformPosition : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The transform.")]
            public Transform transform = Transform.defaultValue;
            [Tooltip("The position to be transformed.")]
            public Position position = Position.defaultValue;
        }

        public class OutputProperties
        {
            public Vector3 tPos = Vector3.zero;
        }

        override public string name { get { return "Transform (Position)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformPosition(inputExpression[0], inputExpression[1]) };
        }
    }
}
