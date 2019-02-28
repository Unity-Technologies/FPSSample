using System;
using UnityEditor.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp")]
    class Minimum : VFXOperatorNumericCascadedUnified
    {
        protected override sealed string operatorName { get { return "Minimum"; } }

        protected override sealed double defaultValueDouble { get { return 0.0; } }
        protected override sealed float identityValueFloat { get { return float.MaxValue; } }
        protected override sealed int identityValueInt { get { return int.MaxValue; } }
        protected override sealed uint identityValueUint { get { return uint.MaxValue; } }

        protected override sealed VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return new VFXExpressionMin(a, b);
        }
    }
}
