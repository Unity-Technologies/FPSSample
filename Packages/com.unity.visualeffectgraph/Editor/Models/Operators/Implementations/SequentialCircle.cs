using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class SequentialCircle : VFXOperator
    {
        public class InputProperties
        {
            //TODO : Circle has to be reworks ?
            [Tooltip("Center of the circle")]
            public Position Center = Position.defaultValue;
            [Tooltip("Radius of the circle")]
            public float Radius = 1.0f;
            [Tooltip("Rotation Axis")]
            public DirectionType Normal = new DirectionType() { direction = Vector3.forward };
            [Tooltip("Start Angle (Midnight direction)")]
            public DirectionType Up = new DirectionType() { direction = Vector3.up };
            [Tooltip("Element index used to loop over the sequence")]
            public uint Index = 0u;
            [Tooltip("Element count used to loop over the sequence")]
            public uint Count = 64u;
        }

        public class OutputProperties
        {
            public Position r = Position.defaultValue;
        }

        public override string name
        {
            get
            {
                return "Sequential Circle";
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var center = inputExpression[0];
            var radius = inputExpression[1];
            var normal = inputExpression[2];
            var up = inputExpression[3];
            var index = inputExpression[4];
            var count = inputExpression[5];

            return new[] { VFXOperatorUtility.SequentialCircle(center, radius, normal, up, index, count) };
        }
    }
}
