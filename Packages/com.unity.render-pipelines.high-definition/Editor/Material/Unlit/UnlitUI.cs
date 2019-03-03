using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class UnlitGUI : BaseUnlitGUI
    {
        protected override uint defaultExpandedState { get { return (uint)(Expandable.Base | Expandable.Input | Expandable.Transparency); }  }

        protected static class Styles
        {
            public static string InputsText = "Inputs";

            public static GUIContent colorText = new GUIContent("Color", "Color");

            public static GUIContent emissiveText = new GUIContent("Emissive Color", "Emissive");
        }

        protected MaterialProperty color = null;
        protected const string kColor = "_UnlitColor";
        protected MaterialProperty colorMap = null;
        protected const string kColorMap = "_UnlitColorMap";
        protected MaterialProperty emissiveColor = null;
        protected const string kEmissiveColor = "_EmissiveColor";
        protected MaterialProperty emissiveColorMap = null;
        protected const string kEmissiveColorMap = "_EmissiveColorMap";

        override protected void FindMaterialProperties(MaterialProperty[] props)
        {
            color = FindProperty(kColor, props);
            colorMap = FindProperty(kColorMap, props);

            emissiveColor = FindProperty(kEmissiveColor, props);
            emissiveColorMap = FindProperty(kEmissiveColorMap, props);
        }

        protected override void MaterialPropertiesGUI(Material material)
        {
            using (var header = new HeaderScope(Styles.InputsText, (uint)Expandable.Input, this))
            {
                if (header.expanded)
                {
                    m_MaterialEditor.TexturePropertySingleLine(Styles.colorText, colorMap, color);
                    m_MaterialEditor.TextureScaleOffsetProperty(colorMap);

                    m_MaterialEditor.TexturePropertySingleLine(Styles.emissiveText, emissiveColorMap, emissiveColor);
                    m_MaterialEditor.TextureScaleOffsetProperty(emissiveColorMap);
                }
            }
            var surfaceTypeValue = (SurfaceType)surfaceType.floatValue;
            if (surfaceTypeValue == SurfaceType.Transparent)
            {
                using (var header = new HeaderScope(StylesBaseUnlit.TransparencyInputsText, (uint)Expandable.Transparency, this))
                {
                    if (header.expanded)
                    {
                        DoDistortionInputsGUI();
                    }
                }
            }
        }

        protected override void MaterialPropertiesAdvanceGUI(Material material)
        {
        }

        protected override void VertexAnimationPropertiesGUI()
        {
        }

        protected override bool ShouldEmissionBeEnabled(Material material)
        {
            return (material.GetColor(kEmissiveColor) != Color.black) || material.GetTexture(kEmissiveColorMap);
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            SetupBaseUnlitKeywords(material);
            SetupBaseUnlitMaterialPass(material);

            CoreUtils.SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap));
        }
    }
} // namespace UnityEditor
