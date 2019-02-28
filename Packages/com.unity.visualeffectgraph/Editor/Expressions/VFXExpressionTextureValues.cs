using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    class VFXTexture2DValue : VFXValue<Texture>
    {
        public VFXTexture2DValue(Texture content = null, Mode mode = Mode.FoldableVariable) : base(content, mode)
        {
        }

        sealed protected override int[] additionnalOperands
        {
            get
            {
                return new int[] { (int)VFXValueType.Texture2D };
            }
        }

        sealed public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXTexture2DValue(Get(), mode);
            return copy;
        }
    }

    class VFXTexture3DValue : VFXValue<Texture>
    {
        public VFXTexture3DValue(Texture content = null, Mode mode = Mode.FoldableVariable) : base(content, mode)
        {
        }

        sealed protected override int[] additionnalOperands
        {
            get
            {
                return new int[] { (int)VFXValueType.Texture3D };
            }
        }

        sealed public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXTexture3DValue(Get(), mode);
            return copy;
        }
    }

    class VFXTextureCubeValue : VFXValue<Texture>
    {
        public VFXTextureCubeValue(Texture content = null, Mode mode = Mode.FoldableVariable) : base(content, mode)
        {
        }

        sealed protected override int[] additionnalOperands
        {
            get
            {
                return new int[] { (int)VFXValueType.TextureCube };
            }
        }

        sealed public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXTextureCubeValue(Get(), mode);
            return copy;
        }
    }

    class VFXTexture2DArrayValue : VFXValue<Texture>
    {
        public VFXTexture2DArrayValue(Texture content = null, Mode mode = Mode.FoldableVariable) : base(content, mode)
        {
        }

        sealed protected override int[] additionnalOperands
        {
            get
            {
                return new int[] { (int)VFXValueType.Texture2DArray };
            }
        }

        sealed public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXTexture2DArrayValue(Get(), mode);
            return copy;
        }
    }

    class VFXTextureCubeArrayValue : VFXValue<Texture>
    {
        public VFXTextureCubeArrayValue(Texture content = null, Mode mode = Mode.FoldableVariable) : base(content, mode)
        {
        }

        sealed protected override int[] additionnalOperands
        {
            get
            {
                return new int[] { (int)VFXValueType.TextureCubeArray };
            }
        }

        sealed public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXTextureCubeArrayValue(Get(), mode);
            return copy;
        }
    }
}
