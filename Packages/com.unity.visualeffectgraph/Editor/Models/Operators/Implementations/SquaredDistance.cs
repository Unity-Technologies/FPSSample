using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
    class SquaredDistance : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            [Tooltip("The first operand.")]
            public Vector3 a = Vector3.zero;
            [Tooltip("The second operand.")]
            public Vector3 b = Vector3.zero;
        }

        protected override sealed Type GetExpectedOutputTypeOfOperation(IEnumerable<Type> inputTypes)
        {
            var type = inputTypes.First(); //derive from VFXOperatorNumericUniform, First is suitable
            return VFXExpression.GetMatchingScalar(type);
        }

        protected sealed override string expectedOutputName
        {
            get
            {
                return "d";
            }
        }

        protected override sealed VFXPropertyAttribute[] expectedOutputAttributes
        {
            get
            {
                return VFXPropertyAttribute.Create(new TooltipAttribute("The squared distance between a and b."));
            }
        }

        protected override sealed string operatorName { get { return "Squared Distance"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.SqrDistance(inputExpression[0], inputExpression[1]) };
        }
    }
}
