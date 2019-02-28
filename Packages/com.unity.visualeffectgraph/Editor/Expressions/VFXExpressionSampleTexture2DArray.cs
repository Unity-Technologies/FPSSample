using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleTexture2DArray : VFXExpression
    {
        public VFXExpressionSampleTexture2DArray() : this(VFXTexture2DArrayValue.Default, VFXValue<Vector2>.Default, VFXValue<float>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionSampleTexture2DArray(VFXExpression texture, VFXExpression uv, VFXExpression slice, VFXExpression mipLevel)
            : base(Flags.InvalidOnCPU, new VFXExpression[4] { texture, uv, slice, mipLevel })
        {}

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float4; } }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleTexture(VFX_SAMPLER({0}),{1},{2},{3})", parents[0], parents[1], parents[2], parents[3]);
        }
    }
}
