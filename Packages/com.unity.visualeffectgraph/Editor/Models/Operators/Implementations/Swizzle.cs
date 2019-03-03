using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
    class Swizzle : VFXOperatorNumericUniform
    {
        public override sealed string libraryName { get { return "Swizzle"; } }
        protected override sealed string operatorName { get { return "Swizzle." + mask; } }

        public class InputProperties
        {
            public Vector4 x = Vector4.zero;
        }

        [VFXSetting, Regex("[^w-zW-Z]", 4), Delayed]
        public string mask = "xyzw";

        protected override sealed Type GetExpectedOutputTypeOfOperation(IEnumerable<Type> inputTypes)
        {
            Type slotType = null;
            switch (mask.Length)
            {
                case 1: slotType = typeof(float); break;
                case 2: slotType = typeof(Vector2); break;
                case 3: slotType = typeof(Vector3); break;
                case 4: slotType = typeof(Vector4); break;
                default: break;
            }
            return slotType;
        }

        private static int CharToComponentIndex(char componentChar)
        {
            switch (componentChar)
            {
                default:
                case 'x': return 0;
                case 'y': return 1;
                case 'z': return 2;
                case 'w': return 3;
            }
        }

        protected override ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptInteger;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            if (mask.Length == 0)
                return new VFXExpression[] {};

            var inputComponents = (inputExpression.Length > 0) ? VFXOperatorUtility.ExtractComponents(inputExpression[0]).ToArray() : new VFXExpression[0];

            var componentStack = new Stack<VFXExpression>();
            int outputSize = mask.Length;
            for (int iComponent = 0; iComponent < outputSize; iComponent++)
            {
                char componentChar = char.ToLower(mask[iComponent]);
                int currentComponent = Math.Min(CharToComponentIndex(componentChar), inputComponents.Length - 1);
                componentStack.Push(inputComponents[currentComponent]);
            }

            VFXExpression finalExpression = null;
            if (componentStack.Count == 1)
            {
                finalExpression = componentStack.Pop();
            }
            else
            {
                finalExpression = new VFXExpressionCombine(componentStack.Reverse().ToArray());
            }
            return new[] { finalExpression };
        }
    }
}
