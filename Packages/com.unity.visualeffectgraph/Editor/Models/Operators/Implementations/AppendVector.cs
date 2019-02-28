using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
    class AppendVector : VFXOperatorNumericCascadedUnified
    {
        protected override sealed string operatorName { get { return "AppendVector"; } }

        protected override sealed double defaultValueDouble { get { return 0.0f; } }

        protected override sealed ValidTypeRule typeFilter { get { return ValidTypeRule.allowEverythingExceptIntegerAndVector4; } }

        protected override Type GetExpectedOutputTypeOfOperation(IEnumerable<Type> inputTypes)
        {
            var outputComponentCount = inputTypes.Select(o => VFXExpression.TypeToSize(VFXExpression.GetVFXValueTypeFromType(o))).Sum();
            outputComponentCount = Mathf.Min(Mathf.Max(outputComponentCount, 1), 4);
            switch (outputComponentCount)
            {
                case 2: return typeof(Vector2);
                case 3: return typeof(Vector3);
                case 4: return typeof(Vector4);
                default: return typeof(float);
            }
        }

        protected override sealed IEnumerable<VFXExpression> ApplyPatchInputExpression(IEnumerable<VFXExpression> inputExpression)
        {
            return inputExpression; //remove explicitly unified behavior
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var allComponent = inputExpression.SelectMany(e => VFXOperatorUtility.ExtractComponents(e))
                .Take(4)
                .ToArray();

            if (allComponent.Length == 0)
            {
                return new VFXExpression[] {};
            }
            else if (allComponent.Length == 1)
            {
                return allComponent;
            }
            return new[] { new VFXExpressionCombine(allComponent) };
        }

        protected override VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            throw new NotImplementedException();
        }
    }
}
