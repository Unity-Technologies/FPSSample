using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionRGBtoHSV : VFXExpression
    {
        public VFXExpressionRGBtoHSV() : this(VFXValue<Vector3>.Default)
        {
        }

        public VFXExpressionRGBtoHSV(VFXExpression parent) : base(VFXExpression.Flags.None, new[] { parent })
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.RGBtoHSV;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var rgbReduce = constParents[0];
            var rgb = rgbReduce.Get<Vector3>();
            Color color = new Color(rgb.x, rgb.y, rgb.z, 1.0f);

            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);

            return VFXValue.Constant(new Vector3(h, s, v));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("RGBtoHSV({0})", parents[0]);
        }
    }

    class VFXExpressionHSVtoRGB : VFXExpression
    {
        public VFXExpressionHSVtoRGB() : this(VFXValue<Vector3>.Default)
        {
        }

        public VFXExpressionHSVtoRGB(VFXExpression parent) : base(VFXExpression.Flags.None, new VFXExpression[] { parent })
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.HSVtoRGB;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var hsvReduce = constParents[0];
            var hsv = hsvReduce.Get<Vector3>();

            var rgb = Color.HSVToRGB(hsv.x, hsv.y, hsv.z, true);

            return VFXValue.Constant<Vector3>(new Vector3(rgb.r, rgb.g, rgb.b));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("HSVtoRGB({0})", parents[0]);
        }
    }
}
