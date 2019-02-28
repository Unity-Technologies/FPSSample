using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class TransformDirection : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The transform.")]
            public Transform transform = Transform.defaultValue;
            [Tooltip("The normalized vector to be transformed.")]
            public DirectionType direction = DirectionType.defaultValue;
        }

        public class OutputProperties
        {
            public Vector3 tDir = Vector3.zero;
        }

        override public string name { get { return "Transform (Direction)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformDirection(inputExpression[0], inputExpression[1]) };
        }
    }
}
