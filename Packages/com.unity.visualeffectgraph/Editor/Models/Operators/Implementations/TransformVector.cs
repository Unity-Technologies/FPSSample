using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class TransformVector : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The transform.")]
            public Transform transform = Transform.defaultValue;
            [Tooltip("The vector to be transformed.")]
            public Vector vector = Vector.defaultValue;
        }

        public class OutputProperties
        {
            public Vector3 tVec = Vector3.zero;
        }

        override public string name { get { return "Transform (Vector)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformVector(inputExpression[0], inputExpression[1]) };
        }
    }
}
