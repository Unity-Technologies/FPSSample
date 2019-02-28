using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Logic")]
    class LogicalNot : VFXOperator
    {
        override public string name { get { return "Not"; } }

        public class InputProperties
        {
            static public bool FallbackValue = false;
            [Tooltip("The operand.")]
            public bool a = FallbackValue;
        }

        public class OutputProperties
        {
            public bool o = false;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionLogicalNot(inputExpression[0]) };
        }
    }
}
