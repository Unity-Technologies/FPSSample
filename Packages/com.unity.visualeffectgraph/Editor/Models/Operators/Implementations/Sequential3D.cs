using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class Sequential3D : VFXOperator
    {
        public class InputProperties
        {
            public Position Origin = Position.defaultValue;
            public Vector AxisX = Vector3.right;
            public Vector AxisY = Vector3.up;
            public Vector AxisZ = Vector3.forward;

            [Tooltip("Element index used to loop over the sequence")]
            public uint Index = 0u;
            [Tooltip("Element X count used to loop over the sequence")]
            public uint CountX = 8u;
            [Tooltip("Element Y count used to loop over the sequence")]
            public uint CountY = 8u;
            [Tooltip("Element Z count used to loop over the sequence")]
            public uint CountZ = 8u;
        }

        public class OutputProperties
        {
            public Position r = Position.defaultValue;
        }

        public override string name
        {
            get
            {
                return "Sequential 3D";
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var origin = inputExpression[0];
            var axisX = inputExpression[1];
            var axisY = inputExpression[2];
            var axisZ = inputExpression[3];
            var index = inputExpression[4];
            var countX = inputExpression[5];
            var countY = inputExpression[6];
            var countZ = inputExpression[7];

            return new[] { VFXOperatorUtility.Sequential3D(origin, axisX, axisY, axisZ, index, countX, countY, countZ) };
        }
    }
}
