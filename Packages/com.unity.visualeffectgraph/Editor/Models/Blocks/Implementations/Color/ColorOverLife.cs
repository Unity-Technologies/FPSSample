using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{
    //[VFXInfo(category = "Color")] DEPRECATED
    class ColorOverLife : VFXBlock
    {
        public enum ColorApplicationMode
        {
            Color = 1 << 0,
            Alpha = 1 << 1,
            ColorAndAlpha = Color | Alpha,
        }

        [Tooltip("Whether the color is applied to RGB, alpha or both")]
        [VFXSetting]
        public ColorApplicationMode mode = ColorApplicationMode.ColorAndAlpha;
        [Tooltip("How the new computed color is composed with its previous value")]
        [VFXSetting]
        public AttributeCompositionMode ColorComposition = AttributeCompositionMode.Multiply;
        [Tooltip("How the new computed alpha is composed with its previous value")]
        [VFXSetting]
        public AttributeCompositionMode AlphaComposition = AttributeCompositionMode.Multiply;


        public override string name { get { return "Color / Alpha over Life"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);

                if ((mode & ColorApplicationMode.Color) != 0)
                    yield return new VFXAttributeInfo(VFXAttribute.Color, ColorComposition == AttributeCompositionMode.Overwrite ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite);
                if ((mode & ColorApplicationMode.Alpha) != 0)
                    yield return new VFXAttributeInfo(VFXAttribute.Alpha, AlphaComposition == AttributeCompositionMode.Overwrite ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite);
            }
        }

        private IEnumerable<string> skipInputProperties
        {
            get
            {
                if ((mode & ColorApplicationMode.Color) == 0 || ColorComposition != AttributeCompositionMode.Blend)
                    yield return "BlendColor";

                if ((mode & ColorApplicationMode.Alpha) == 0 || AlphaComposition != AttributeCompositionMode.Blend)
                    yield return "BlendAlpha";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                return base.inputProperties.Where(o => !skipInputProperties.Any(a => a == o.property.name));
            }
        }

        public class InputProperties
        {
            [Tooltip("The over-life Gradient")]
            public Gradient gradient = VFXResources.defaultResources.gradient;
            [Tooltip("Color blending factor")]
            [Range(0.0f, 1.0f)]
            public float BlendColor = 0.5f;
            [Tooltip("Alpha blending factor")]
            [Range(0.0f, 1.0f)]
            public float BlendAlpha = 0.5f;
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (string setting in base.filteredOutSettings) yield return setting;
                if ((mode & ColorApplicationMode.Color) == 0) yield return "ColorComposition";
                if ((mode & ColorApplicationMode.Alpha) == 0) yield return "AlphaComposition";
            }
        }

        public override string source
        {
            get
            {
                string outSource = @"
float4 sampledColor = SampleGradient(gradient, age/lifetime);
";
                if ((mode & ColorApplicationMode.Color) != 0) outSource += VFXBlockUtility.GetComposeString(ColorComposition, "color", "sampledColor.rgb", "BlendColor") + "\n";
                if ((mode & ColorApplicationMode.Alpha) != 0) outSource += VFXBlockUtility.GetComposeString(AlphaComposition, "alpha", "sampledColor.a", "BlendAlpha") + "\n";

                return outSource;
            }
        }

        // TODO - requires santizing Gradient -> Curve for Alpha. Needs fixing.
        /*public override void Sanitize(int version)
        {
            Debug.Log("Sanitizing Graph: Automatically replace ColorOverLife with AttributeOverLife");

            if ((mode & ColorApplicationMode.Color) != 0)
            {
                var attributeOverLifeColor = CreateInstance<AttributeOverLife>();

                attributeOverLifeColor.SetSettingValue("attribute", "color");
                attributeOverLifeColor.SetSettingValue("Composition", ColorComposition);

                // Transfer links
                VFXSlot.TransferLinksAndValue(attributeOverLifeColor.GetInputSlot(0), GetInputSlot(0), true);

                ReplaceModel(attributeOverLifeColor, this);
            }

            if ((mode & ColorApplicationMode.Alpha) != 0)
            {
                var attributeOverLifeAlpha = CreateInstance<AttributeOverLife>();

                attributeOverLifeAlpha.SetSettingValue("attribute", "alpha");
                attributeOverLifeAlpha.SetSettingValue("Composition", AlphaComposition);

                // Transfer links
                VFXSlot.TransferLinksAndValue(attributeOverLifeAlpha.GetInputSlot(0), GetInputSlot(0), true);

                ReplaceModel(attributeOverLifeAlpha, this);
            }
        }*/
    }
}
