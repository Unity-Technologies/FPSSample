using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Bitwise")]
    class BitwiseComplement : VFXOperator
    {
        override public string name { get { return "Complement"; } }

        public class InputProperties
        {
            [Tooltip("The operand.")]
            public uint x = 0;
        }

        public class OutputProperties
        {
            public uint o = 0;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBitwiseComplement(inputExpression[0]) };
        }
    }
}
