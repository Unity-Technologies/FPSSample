using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class TransformMatrix : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The transform.")]
            public Transform transform = Transform.defaultValue;
            [Tooltip("The Matrix4x4 to be transformed.")]
            public Matrix4x4 matrix = Matrix4x4.identity;
        }

        public class OutputProperties
        {
            public Matrix4x4 o = Matrix4x4.identity;
        }

        override public string name { get { return "Transform (Matrix)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformMatrix(inputExpression[0], inputExpression[1]) };
        }
    }
}
