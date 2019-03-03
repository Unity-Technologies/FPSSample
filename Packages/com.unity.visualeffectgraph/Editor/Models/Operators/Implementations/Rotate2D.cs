using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
    class Rotate2D : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Position to Rotate")]
            public Vector2 Position = Vector2.one;
            [Tooltip("Rotation Center")]
            public Vector2 RotationCenter = Vector2.zero;
            [Tooltip("Angle in Radians")]
            public float Angle;
        }

        public class OutputProperties
        {
            [Tooltip("Rotated Position")]
            public Vector2 Position;
        }

        override public string name { get { return "Rotate 2D"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var posX = inputExpression[0].x - inputExpression[1].x;
            var posY = inputExpression[0].y - inputExpression[1].y;
            var centerX = inputExpression[1].x;
            var centerY = inputExpression[1].y;

            var sinAngle = new VFXExpressionSin(inputExpression[2]);
            var cosAngle = new VFXExpressionCos(inputExpression[2]);

            var outPosX = centerX + ((posX * cosAngle) - (posY * sinAngle));
            var outPosY = centerY + ((posX * sinAngle) + (posY * cosAngle));

            return new[] { new VFXExpressionCombine(outPosX, outPosY) };
        }
    }
}
