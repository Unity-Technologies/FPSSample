using System;
using UnityEditor.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Add : VFXOperatorNumericCascadedUnified
    {
        protected override sealed string operatorName { get { return "Add"; } }
        protected override sealed double defaultValueDouble { get { return 0.0; } }
        protected override sealed VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a + b;
        }
    }
}
