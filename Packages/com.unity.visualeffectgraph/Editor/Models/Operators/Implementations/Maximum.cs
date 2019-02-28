using System;
using UnityEditor.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp")]
    class Maximum : VFXOperatorNumericCascadedUnified
    {
        protected override sealed string operatorName { get { return "Maximum"; } }

        protected override sealed double defaultValueDouble { get { return 0.0; } }
        protected override sealed float identityValueFloat { get { return float.MinValue; } }
        protected override sealed int identityValueInt { get { return int.MinValue; } }
        protected override sealed uint identityValueUint { get { return uint.MinValue; } }

        protected override sealed VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return new VFXExpressionMax(a, b);
        }
    }
}
