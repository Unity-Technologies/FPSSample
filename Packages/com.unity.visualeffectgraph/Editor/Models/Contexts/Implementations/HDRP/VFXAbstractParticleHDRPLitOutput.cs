using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractParticleHDRPLitOutput : VFXAbstractParticleOutput
    {
        public enum MaterialType
        {
            Standard,
            SpecularColor,
            Translucent,
            SimpleLit,
            SimpleLitTranslucent,
        }

        [Flags]
        public enum ColorMode
        {
            None = 0,
            BaseColor = 1 << 0,
            Emissive = 1 << 1,
            BaseColorAndEmissive = BaseColor | Emissive,
        }

        [Flags]
        public enum BaseColorMapMode
        {
            None = 0,
            Color = 1 << 0,
            Alpha = 1 << 1,
            ColorAndAlpha = Color | Alpha
        }

        private readonly string[] kMaterialTypeToName = new string[]
        {
            "StandardProperties",
            "SpecularColorProperties",
            "TranslucentProperties",
            "StandardProperties",
            "TranslucentProperties",
        };

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("Lighting")]
        protected MaterialType materialType = MaterialType.Standard;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool onlyAmbientLighting = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Range(1, 15)]
        protected uint diffusionProfile = 1;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool multiplyThicknessWithAlpha = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected BaseColorMapMode useBaseColorMap = BaseColorMapMode.ColorAndAlpha;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool useMaskMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool useNormalMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool useEmissiveMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected ColorMode colorMode = ColorMode.BaseColor;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool useEmissive = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool doubleSided = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Header("Simple Lit features")]
        protected bool enableShadows = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool enableSpecular = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool enableCookie = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool enableEnvLight = true;

        protected virtual bool allowTextures { get { return true; }}

        public class HDRPLitInputProperties
        {
            [Range(0, 1)]
            public float smoothness = 0.5f;
        }

        public class StandardProperties
        {
            [Range(0, 1)]
            public float metallic = 0.5f;
        }

        public class SpecularColorProperties
        {
            public Color specularColor = Color.gray;
        }

        public class TranslucentProperties
        {
            [Range(0, 1)]
            public float thickness = 1.0f;
        }

        public class BaseColorMapProperties
        {
            [Tooltip("Base Color (RGB) Opacity (A)")]
            public Texture2D baseColorMap = VFXResources.defaultResources.particleTexture;
        }

        public class MaskMapProperties
        {
            [Tooltip("Metallic (R) AO (G) Smoothness (A)")]
            public Texture2D maskMap = VFXResources.defaultResources.noiseTexture;
        }

        public class NormalMapProperties
        {
            [Tooltip("Normal in tangent space")]
            public Texture2D normalMap;
            [Range(0, 2)]
            public float normalScale = 1.0f;
        }

        public class EmissiveMapProperties
        {
            [Tooltip("Normal in tangent space")]
            public Texture2D emissiveMap;
            public float emissiveScale = 1.0f;
        }

        public class BaseColorProperties
        {
            public Color baseColor = Color.white;
        }

        public class EmissiveColorProperties
        {
            public Color emissiveColor = Color.black;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;
                properties = properties.Concat(PropertiesFromType("HDRPLitInputProperties"));
                properties = properties.Concat(PropertiesFromType(kMaterialTypeToName[(int)materialType]));

                if (allowTextures)
                {
                    if (useBaseColorMap != BaseColorMapMode.None)
                        properties = properties.Concat(PropertiesFromType("BaseColorMapProperties"));
                }

                if ((colorMode & ColorMode.BaseColor) == 0) // particle color is not used as base color so add a slot
                    properties = properties.Concat(PropertiesFromType("BaseColorProperties"));

                if (allowTextures)
                {
                    if (useMaskMap)
                        properties = properties.Concat(PropertiesFromType("MaskMapProperties"));
                    if (useNormalMap)
                        properties = properties.Concat(PropertiesFromType("NormalMapProperties"));
                    if (useEmissiveMap)
                        properties = properties.Concat(PropertiesFromType("EmissiveMapProperties"));
                }

                if (((colorMode & ColorMode.Emissive) == 0) && useEmissive)
                    properties = properties.Concat(PropertiesFromType("EmissiveColorProperties"));

                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            yield return slotExpressions.First(o => o.name == "smoothness");

            switch (materialType)
            {
                case MaterialType.Standard:
                case MaterialType.SimpleLit:
                    yield return slotExpressions.First(o => o.name == "metallic");
                    break;

                case MaterialType.SpecularColor:
                    yield return slotExpressions.First(o => o.name == "specularColor");
                    break;

                case MaterialType.Translucent:
                case MaterialType.SimpleLitTranslucent:
                    yield return slotExpressions.First(o => o.name == "thickness");
                    yield return new VFXNamedExpression(VFXValue.Constant(diffusionProfile), "diffusionProfile");
                    break;

                default: break;
            }

            if (allowTextures)
            {
                if (useBaseColorMap != BaseColorMapMode.None)
                    yield return slotExpressions.First(o => o.name == "baseColorMap");
                if (useMaskMap)
                    yield return slotExpressions.First(o => o.name == "maskMap");
                if (useNormalMap)
                {
                    yield return slotExpressions.First(o => o.name == "normalMap");
                    yield return slotExpressions.First(o => o.name == "normalScale");
                }
                if (useEmissiveMap)
                {
                    yield return slotExpressions.First(o => o.name == "emissiveMap");
                    yield return slotExpressions.First(o => o.name == "emissiveScale");
                }
            }

            if ((colorMode & ColorMode.BaseColor) == 0)
                yield return slotExpressions.First(o => o.name == "baseColor");

            if (((colorMode & ColorMode.Emissive) == 0) && useEmissive)
                yield return slotExpressions.First(o => o.name == "emissiveColor");
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                yield return "HDRP_LIT";

                switch (materialType)
                {
                    case MaterialType.Standard:
                        yield return "HDRP_MATERIAL_TYPE_STANDARD";
                        break;

                    case MaterialType.SpecularColor:
                        yield return "HDRP_MATERIAL_TYPE_SPECULAR";
                        break;

                    case MaterialType.Translucent:
                        yield return "HDRP_MATERIAL_TYPE_TRANSLUCENT";
                        if (multiplyThicknessWithAlpha)
                            yield return "HDRP_MULTIPLY_THICKNESS_WITH_ALPHA";
                        break;

                    case MaterialType.SimpleLit:
                        yield return "HDRP_MATERIAL_TYPE_SIMPLELIT";
                        if (enableShadows)
                            yield return "HDRP_ENABLE_SHADOWS";
                        if (enableSpecular)
                            yield return "HDRP_ENABLE_SPECULAR";
                        if (enableCookie)
                            yield return "HDRP_ENABLE_COOKIE";
                        if (enableEnvLight)
                            yield return "HDRP_ENABLE_ENV_LIGHT";
                        break;

                    case MaterialType.SimpleLitTranslucent:
                        yield return "HDRP_MATERIAL_TYPE_SIMPLELIT_TRANSLUCENT";
                        if (enableShadows)
                            yield return "HDRP_ENABLE_SHADOWS";
                        if (enableSpecular)
                            yield return "HDRP_ENABLE_SPECULAR";
                        if (enableCookie)
                            yield return "HDRP_ENABLE_COOKIE";
                        if (enableEnvLight)
                            yield return "HDRP_ENABLE_ENV_LIGHT";
                        if (multiplyThicknessWithAlpha)
                            yield return "HDRP_MULTIPLY_THICKNESS_WITH_ALPHA";
                        break;

                    default: break;
                }

                if (allowTextures)
                {
                    if (useBaseColorMap != BaseColorMapMode.None)
                        yield return "HDRP_USE_BASE_COLOR_MAP";
                    if ((useBaseColorMap & BaseColorMapMode.Color) != 0)
                        yield return "HDRP_USE_BASE_COLOR_MAP_COLOR";
                    if ((useBaseColorMap & BaseColorMapMode.Alpha) != 0)
                        yield return "HDRP_USE_BASE_COLOR_MAP_ALPHA";
                    if (useMaskMap)
                        yield return "HDRP_USE_MASK_MAP";
                    if (useNormalMap)
                        yield return "USE_NORMAL_MAP";
                    if (useEmissiveMap)
                        yield return "HDRP_USE_EMISSIVE_MAP";
                }

                if ((colorMode & ColorMode.BaseColor) != 0)
                    yield return "HDRP_USE_BASE_COLOR";
                else
                    yield return "HDRP_USE_ADDITIONAL_BASE_COLOR";

                if ((colorMode & ColorMode.Emissive) != 0)
                    yield return "HDRP_USE_EMISSIVE_COLOR";
                else if (useEmissive)
                    yield return "HDRP_USE_ADDITIONAL_EMISSIVE_COLOR";

                if (doubleSided)
                    yield return "USE_DOUBLE_SIDED";

                if (onlyAmbientLighting && !isBlendModeOpaque)
                    yield return "USE_ONLY_AMBIENT_LIGHTING";

                if (isBlendModeOpaque && materialType != MaterialType.SimpleLit && materialType != MaterialType.SimpleLitTranslucent)
                    yield return "IS_OPAQUE_NOT_SIMPLE_LIT_PARTICLE";
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                if (materialType != MaterialType.Translucent && materialType != MaterialType.SimpleLitTranslucent)
                {
                    yield return "diffusionProfile";
                    yield return "multiplyThicknessWithAlpha";
                }

                if (materialType != MaterialType.SimpleLit && materialType != MaterialType.SimpleLitTranslucent)
                {
                    yield return "enableShadows";
                    yield return "enableSpecular";
                    yield return "enableTransmission";
                    yield return "enableCookie";
                    yield return "enableEnvLight";
                }

                if (!allowTextures)
                {
                    yield return "useBaseColorMap";
                    yield return "useMaskMap";
                    yield return "useNormalMap";
                    yield return "useEmissiveMap";
                    yield return "alphaMask";
                }

                if ((colorMode & ColorMode.Emissive) != 0)
                    yield return "useEmissive";

                if (isBlendModeOpaque)
                    yield return "onlyAmbientLighting";
            }
        }

        // HDRP always premultiplies in shader
        protected override void WriteBlendMode(VFXShaderWriter writer)
        {
            if (blendMode == BlendMode.Additive)
                writer.WriteLine("Blend One One");
            else if (blendMode == BlendMode.Alpha)
                writer.WriteLine("Blend One OneMinusSrcAlpha");
            else if (blendMode == BlendMode.AlphaPremultiplied)
                writer.WriteLine("Blend One OneMinusSrcAlpha");
        }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionalReplacements
        {
            get
            {
                foreach (var kvp in base.additionalReplacements)
                    yield return kvp;

                // HDRP Forward specific defines
                var forwardDefines = new VFXShaderWriter();
                forwardDefines.WriteLine("#define _ENABLE_FOG_ON_TRANSPARENT");
                forwardDefines.WriteLine("#define _DISABLE_DECALS");
                switch (blendMode)
                {
                    case BlendMode.Alpha:
                        forwardDefines.WriteLine("#define _BLENDMODE_ALPHA");
                        break;
                    case BlendMode.Additive:
                        forwardDefines.WriteLine("#define _BLENDMODE_ADD");
                        break;
                    case BlendMode.AlphaPremultiplied:
                        forwardDefines.WriteLine("#define _BLENDMODE_PRE_MULTIPLY");
                        break;
                }
                if (!isBlendModeOpaque)
                    forwardDefines.WriteLine("#define _SURFACE_TYPE_TRANSPARENT");

                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXHDRPForwardDefines}", forwardDefines);
                var forwardPassName = new VFXShaderWriter();
                forwardPassName.Write(materialType == MaterialType.SimpleLit || materialType == MaterialType.SimpleLitTranslucent ? "ForwardOnly" : "Forward");
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXHDRPForwardPassName}", forwardPassName);
            }
        }
    }
}
