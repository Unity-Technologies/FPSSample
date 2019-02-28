using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Multiply : VFXOperatorNumericCascadedUnified
    {
        protected override sealed string operatorName { get { return "Multiply"; } }

        protected override sealed double defaultValueDouble { get { return 1.0; } }

        protected override sealed VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a * b;
        }
    }
}
