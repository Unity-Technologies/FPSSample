using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Logic")]
    class LogicalNand : VFXOperator
    {
        override public string name { get { return "Nand"; } }

        public class InputProperties
        {
            static public bool FallbackValue = false;
            [Tooltip("The first operand.")]
            public bool a = FallbackValue;
            [Tooltip("The second operand.")]
            public bool b = FallbackValue;
        }

        public class OutputProperties
        {
            public bool o = false;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionLogicalNot(new VFXExpressionLogicalAnd(inputExpression[0], inputExpression[1])) };
        }
    }
}
