using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class SequentialLine : VFXOperator
    {
        public class InputProperties
        {
            public Line line = Line.defaultValue;
            [Tooltip("Element index used to loop over the sequence")]
            public uint Index = 0u;
            [Tooltip("Element count used to loop over the sequence")]
            public uint Count = 64;
        }

        public class OutputProperties
        {
            public Position r = Position.defaultValue;
        }

        public override string name
        {
            get
            {
                return "Sequential Line";
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var start = inputExpression[0];
            var end = inputExpression[1];
            var index = inputExpression[2];
            var count = inputExpression[3];

            return new[] { VFXOperatorUtility.SequentialLine(start, end, index, count) };
        }
    }
}
