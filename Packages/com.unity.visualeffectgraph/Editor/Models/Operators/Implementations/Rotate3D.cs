using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
    class Rotate3D : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Position to Rotate")]
            public Position Position = new Position() { position = Vector3.forward };
            [Tooltip("Rotation Center")]
            public Position RotationCenter = new Position() { position = Vector3.zero };
            [Tooltip("Rotation Axis")]
            public DirectionType RotationAxis = DirectionType.defaultValue;

            [Tooltip("Angle in Radians")]
            public float Angle;
        }

        public class OutputProperties
        {
            [Tooltip("Rotated Position")]
            public Position Position;
        }

        override public string name { get { return "Rotate 3D"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var pos = inputExpression[0];
            var center = inputExpression[1];
            var wAxis = inputExpression[2];
            var angle = inputExpression[3];

            var projPoint = center + (wAxis * VFXOperatorUtility.CastFloat(VFXOperatorUtility.Dot(wAxis, pos - center), VFXValueType.Float3));

            var uAxis = pos - projPoint;
            var vAxis = VFXOperatorUtility.Cross(uAxis, wAxis);

            var sinAngle = VFXOperatorUtility.CastFloat(new VFXExpressionSin(angle), VFXValueType.Float3);
            var cosAngle = VFXOperatorUtility.CastFloat(new VFXExpressionCos(angle), VFXValueType.Float3);

            return new[] { projPoint + (uAxis * cosAngle) + (vAxis * sinAngle) };
        }
    }
}
