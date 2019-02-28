using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class CrossProductDeprecated : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The first operand.")]
            public Vector3 a = Vector3.right;
            [Tooltip("The second operand.")]
            public Vector3 b = Vector3.up;
        }

        public class OutputProperties
        {
            public Vector3 o = Vector3.zero;
        }

        override public string name { get { return "Cross Product (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Cross(inputExpression[0], inputExpression[1]) };
        }

        public override sealed void Sanitize(int version)
        {
            var crossProduct = ScriptableObject.CreateInstance(typeof(CrossProduct)) as VFXOperatorNumericUniform;
            crossProduct.SetOperandType(typeof(Vector3));
            VFXSlot.CopyLinksAndValue(crossProduct.inputSlots[0], inputSlots[0], true);
            VFXSlot.CopyLinksAndValue(crossProduct.inputSlots[1], inputSlots[1], true);
            VFXSlot.CopyLinks(crossProduct.outputSlots[0], outputSlots[0], true);
            VFXModel.ReplaceModel(crossProduct, this);
        }
    }
}
