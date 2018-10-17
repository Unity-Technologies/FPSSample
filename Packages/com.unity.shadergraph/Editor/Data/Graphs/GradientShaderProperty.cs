using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public static class GradientUtils
    {
        public static void GetGradientDeclaration(Gradient gradient, ref ShaderStringBuilder s)
        {
            string[] colors = new string[8];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = string.Format("g.colors[{0}] = float4(0, 0, 0, 0);", i.ToString());
            for (int i = 0; i < gradient.colorKeys.Length; i++)
                colors[i] = GetColorKey(i, gradient.colorKeys[i].color, gradient.colorKeys[i].time);

            string[] alphas = new string[8];
            for (int i = 0; i < colors.Length; i++)
                alphas[i] = string.Format("g.alphas[{0}] = float2(0, 0);", i.ToString());
            for (int i = 0; i < gradient.alphaKeys.Length; i++)
                alphas[i] = GetAlphaKey(i, gradient.alphaKeys[i].alpha, gradient.alphaKeys[i].time);

            s.AppendLine("Gradient g;");
            s.AppendLine("g.type = {0};",
                (int)gradient.mode);
            s.AppendLine("g.colorsLength = {0};",
                gradient.colorKeys.Length);
            s.AppendLine("g.alphasLength = {0};",
                gradient.alphaKeys.Length);

            for (int i = 0; i < colors.Length; i++)
                s.AppendLine(colors[i]);

            for (int i = 0; i < alphas.Length; i++)
                s.AppendLine(alphas[i]);
        }

        public static bool CheckEquivalency(Gradient A, Gradient B)
        {
            var currentMode = A.mode;
            var currentColorKeys = A.colorKeys;
            var currentAlphaKeys = A.alphaKeys;

            var newMode = B.mode;
            var newColorKeys = B.colorKeys;
            var newAlphaKeys = B.alphaKeys;

            if (currentMode != newMode || currentColorKeys.Length != newColorKeys.Length || currentAlphaKeys.Length != newAlphaKeys.Length)
            {
                return false;
            }
            else
            {
                for (var i = 0; i < currentColorKeys.Length; i++)
                {
                    if (currentColorKeys[i].color != newColorKeys[i].color || Mathf.Abs(currentColorKeys[i].time - newColorKeys[i].time) > 1e-9)
                        return false;
                }

                for (var i = 0; i < currentAlphaKeys.Length; i++)
                {
                    if (Mathf.Abs(currentAlphaKeys[i].alpha - newAlphaKeys[i].alpha) > 1e-9 || Mathf.Abs(currentAlphaKeys[i].time - newAlphaKeys[i].time) > 1e-9)
                        return false;
                }
            }
            return true;
        }

        private static string GetColorKey(int index, Color color, float time)
        {
            return string.Format("g.colors[{0}] = float4({1}, {2}, {3}, {4});", index, color.r, color.g, color.b, time);
        }

        private static string GetAlphaKey(int index, float alpha, float time)
        {
            return string.Format("g.alphas[{0}] = float2({1}, {2});", index, alpha, time);
        }

        public static Vector4 ColorKeyToVector(GradientColorKey key)
        {
            return new Vector4(key.color.r, key.color.g, key.color.b, key.time);
        }

        public static Vector2 AlphaKeyToVector(GradientAlphaKey key)
        {
            return new Vector2(key.alpha, key.time);
        }
    }

    [Serializable]
    public class GradientShaderProperty : AbstractShaderProperty<Gradient>
    {
        public GradientShaderProperty()
        {
            displayName = "Gradient";
            value = new Gradient();
        }

        private bool m_OverrideMembers = false;

        private string m_OverrideSlotName;

        public override PropertyType propertyType
        {
            get { return PropertyType.Gradient; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(); }
        }

        public override string GetPropertyBlockString()
        {
            return string.Empty;
        }

        public void OverrideMembers(string slotName)
        {
            m_OverrideMembers = true;
            m_OverrideSlotName = slotName;
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            if (m_OverrideMembers)
            {
                ShaderStringBuilder s = new ShaderStringBuilder();
                s.AppendLine("Gradient Unity{0} ()",
                    referenceName);
                using (s.BlockScope())
                {
                    s.AppendLine("Gradient g;");
                    s.AppendLine("g.type = {0}_Type;", m_OverrideSlotName);
                    s.AppendLine("g.colorsLength = {0}_ColorsLength;", m_OverrideSlotName);
                    s.AppendLine("g.alphasLength = {0}_AlphasLength;", m_OverrideSlotName);
                    for (int i = 0; i < 8; i++)
                    {
                        s.AppendLine("g.colors[{0}] = {1}_ColorKey{0};", i, m_OverrideSlotName);
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        s.AppendLine("g.alphas[{0}] = {1}_AlphaKey{0};", i, m_OverrideSlotName);
                    }
                    s.AppendLine("return g;", true);
                }
                return s.ToString();
            }
            else
            {
                ShaderStringBuilder s = new ShaderStringBuilder();
                s.AppendLine("Gradient Unity{0} ()", referenceName);
                using (s.BlockScope())
                {
                    GradientUtils.GetGradientDeclaration(value, ref s);
                    s.AppendLine("return g;", true);
                }
                return s.ToString();
            }
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Gradient)
            {
                name = referenceName,
                gradientValue = value
            };
        }

        public override INode ToConcreteNode()
        {
            return new GradientNode { gradient = value };
        }

        public override IShaderProperty Copy()
        {
            return new GradientShaderProperty
            {
                value = value
            };
        }
    }
}
