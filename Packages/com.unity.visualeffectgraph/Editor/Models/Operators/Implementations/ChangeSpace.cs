using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class ChangeSpace : VFXOperatorNumericUniform
    {
        [VFXSetting, SerializeField]
        VFXCoordinateSpace m_targetSpace;

        public class InputProperties
        {
            public Position x = Position.defaultValue;
        }

        protected override double defaultValueDouble
        {
            get
            {
                return 0.0;
            }
        }

        public override string libraryName { get { return "Change Space"; } }
        public override string name { get { return "Change Space"; } }

        protected override ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowSpaceable;
            }
        }

        public sealed override VFXCoordinateSpace GetOutputSpaceFromSlot(VFXSlot slot)
        {
            return m_targetSpace;
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            /* Actually, it's automatic because actualOutputSpace return target space
             * See SetOutExpression which use masterSlot.owner.GetOutputSpaceFromSlot
            var currentSpace = inputSlots[0].space;
            if (currentSpace == m_targetSpace)
            {
                return new[] { inputExpression[0] };
            }
            return new[] { ConvertSpace(inputExpression[0], inputSlots[0].GetSpaceTransformationType(), m_targetSpace) };
            */
            return inputExpression.ToArray();
        }
    }
}
