using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Block
{
    class AttributeFromCurveProvider : VariantProvider
    {
        public override IEnumerable<IEnumerable<KeyValuePair<string, object>>> ComputeVariants()
        {
            var compositions = new[] { AttributeCompositionMode.Add, AttributeCompositionMode.Overwrite };
            var attributes = VFXAttribute.AllIncludingVariadicReadWritable.Except(new[] { VFXAttribute.Alive.name }).ToArray();
            var sampleModes = new[] { AttributeFromCurve.CurveSampleMode.OverLife, AttributeFromCurve.CurveSampleMode.BySpeed, AttributeFromCurve.CurveSampleMode.Random }.ToArray();

            foreach (var attribute in attributes)
            {
                foreach (var composition in compositions)
                {
                    foreach (var sampleMode in sampleModes)
                    {
                        if (attribute == VFXAttribute.Age.name && sampleMode == AttributeFromCurve.CurveSampleMode.OverLife)
                        {
                            continue;
                        }

                        if (attribute == VFXAttribute.Velocity.name && sampleMode == AttributeFromCurve.CurveSampleMode.BySpeed)
                        {
                            continue;
                        }

                        yield return new[] {    new KeyValuePair<string, object>("attribute", attribute),
                                                new KeyValuePair<string, object>("Composition", composition),
                                                new KeyValuePair<string, object>("SampleMode", sampleMode)};
                    }
                }
            }
        }
    }

    [VFXInfo(category = "Attribute/Curve", variantProvider = typeof(AttributeFromCurveProvider))]
    class AttributeFromCurve : VFXBlock
    {
        public enum CurveSampleMode
        {
            OverLife,
            BySpeed,
            Random,
            RandomConstantPerParticle,
            Custom
        }

        public enum ComputeMode
        {
            Uniform,
            PerComponent
        }

        public enum ColorApplicationMode
        {
            Color = 1 << 0,
            Alpha = 1 << 1,
            ColorAndAlpha = Color | Alpha,
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(ReadWritableAttributeProvider)), Tooltip("Target Attribute")]
        public string attribute = VFXAttribute.AllIncludingVariadicWritable.First();

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public AttributeCompositionMode AlphaComposition = AttributeCompositionMode.Overwrite;

        [VFXSetting, Tooltip("How to sample the curve")]
        public CurveSampleMode SampleMode = CurveSampleMode.OverLife;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public ComputeMode Mode = ComputeMode.PerComponent;

        [VFXSetting, Tooltip("Select whether the color is applied to RGB, alpha, or both")]
        public ColorApplicationMode ColorMode = ColorApplicationMode.ColorAndAlpha;

        [VFXSetting]
        public VariadicChannelOptions channels = VariadicChannelOptions.XYZ;
        private static readonly char[] channelNames = new char[] { 'x', 'y', 'z' };

        private string GenerateName(bool library)
        {
            var variadicName = (currentAttribute.variadic == VFXVariadic.True && !library) ? "." + channels.ToString() : string.Empty;
            var n = VFXBlockUtility.GetNameString(Composition) + " " + ObjectNames.NicifyVariableName(attribute) + variadicName;
            switch (SampleMode)
            {
                case CurveSampleMode.OverLife: n += " over Life"; break;
                case CurveSampleMode.BySpeed: n += " by Speed"; break;
                case CurveSampleMode.Random: n += " randomized"; break;
                case CurveSampleMode.RandomConstantPerParticle: n += " randomized"; break;
                case CurveSampleMode.Custom: n += " custom"; break;
                default:
                    throw new NotImplementedException("Invalid CurveSampleMode");
            }

            if (library && attribute == VFXAttribute.Color.name)
            {
                n += " (Gradient)";
            }
            return n;
        }

        public override string name
        {
            get
            {
                return GenerateName(false);
            }
        }

        public override string libraryName
        {
            get
            {
                return GenerateName(true);
            }
        }

        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                VFXAttributeMode attributeMode = (Composition != AttributeCompositionMode.Overwrite) ? VFXAttributeMode.ReadWrite : VFXAttributeMode.Write;
                VFXAttributeMode alphaAttributeMode = (AlphaComposition != AttributeCompositionMode.Overwrite) ? VFXAttributeMode.ReadWrite : VFXAttributeMode.Write;

                var attrib = currentAttribute;
                if (attrib.Equals(VFXAttribute.Color))
                {
                    if ((ColorMode & ColorApplicationMode.Color) != 0)
                        yield return new VFXAttributeInfo(VFXAttribute.Color, attributeMode);
                    if ((ColorMode & ColorApplicationMode.Alpha) != 0)
                        yield return new VFXAttributeInfo(VFXAttribute.Alpha, alphaAttributeMode);
                }
                else
                {
                    if (attrib.variadic == VFXVariadic.True)
                    {
                        string channelsString = channels.ToString();
                        for (int i = 0; i < channelsString.Length; i++)
                            yield return new VFXAttributeInfo(VFXAttribute.Find(attrib.name + channelsString[i]), attributeMode);
                    }
                    else
                    {
                        yield return new VFXAttributeInfo(attrib, attributeMode);
                    }
                }

                switch (SampleMode)
                {
                    case CurveSampleMode.OverLife:
                        yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                        yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);
                        break;

                    case CurveSampleMode.BySpeed:
                        yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);
                        break;

                    case CurveSampleMode.Random:
                        yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
                        break;

                    case CurveSampleMode.RandomConstantPerParticle:
                        yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);
                        break;

                    default:
                        break;

                }
            }
        }

        public override void Sanitize(int version)
        {
            if (VFXBlockUtility.SanitizeAttribute(ref attribute, ref channels, version))
                Invalidate(InvalidationCause.kSettingChanged);

            base.Sanitize(version);
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                var attrib = currentAttribute;

                if (attrib.variadic == VFXVariadic.False)
                    yield return "channels";
                if (VFXExpression.TypeToSize(attrib.type) == 1)
                    yield return "Mode";

                if (!currentAttribute.Equals(VFXAttribute.Color))
                {
                    yield return "ColorMode";
                    yield return "AlphaComposition";
                }

                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
            }
        }

        static private string GenerateLocalAttributeName(string name)
        {
            return name[0].ToString().ToUpper() + name.Substring(1);
        }

        public override string source
        {
            get
            {
                string source = "";

                var attrib = currentAttribute;

                bool isColor = currentAttribute.Equals(VFXAttribute.Color);
                int attributeCount = isColor ? 2 : 1;

                int attributeSize = isColor ? 4 : VFXExpression.TypeToSize(attrib.type);
                int loopCount = 1;
                if (attrib.variadic == VFXVariadic.True)
                {
                    attributeSize = channels.ToString().Length;
                    loopCount = attributeSize;
                }

                source += GetFetchValueString(GenerateLocalAttributeName(attrib.name), attributeSize, Mode, SampleMode);

                int attributeAddedCount = 0;
                for (int attribIndex = 0; attribIndex < attributeCount; attribIndex++)
                {
                    string attribName = attrib.name;
                    if (isColor)
                    {
                        if (((int)ColorMode & (1 << attribIndex)) == 0)
                            continue;
                        if (attribIndex == 1)
                            attribName = VFXAttribute.Alpha.name;
                    }

                    string channelSource = "";
                    if (attributeAddedCount > 0)
                        channelSource += "\n";

                    for (int i = 0; i < loopCount; i++)
                    {
                        AttributeCompositionMode compositionMode = Composition;
                        string paramPostfix = (attrib.variadic == VFXVariadic.True) ? "." + channelNames[i] : "";

                        if (isColor)
                        {
                            if (attribIndex == 0)
                            {
                                paramPostfix = ".rgb";
                            }
                            else
                            {
                                paramPostfix = ".a";
                                compositionMode = AlphaComposition;
                            }
                        }

                        string attributePostfix = (attrib.variadic == VFXVariadic.True) ? char.ToUpper(channels.ToString()[i]).ToString() : "";

                        if (compositionMode == AttributeCompositionMode.Blend)
                            channelSource += VFXBlockUtility.GetComposeString(compositionMode, attribName + attributePostfix, "value" + paramPostfix, "Blend");
                        else
                            channelSource += VFXBlockUtility.GetComposeString(compositionMode, attribName + attributePostfix, "value" + paramPostfix);

                        if (i < loopCount - 1)
                            channelSource += "\n";
                    }

                    source += channelSource;
                    attributeAddedCount++;
                }

                return source;
            }
        }

        public string GetFetchValueString(string localName, int size, ComputeMode computeMode, CurveSampleMode sampleMode)
        {
            string output;
            switch (SampleMode)
            {
                case CurveSampleMode.OverLife: output = "float t = age / lifetime;\n"; break;
                case CurveSampleMode.BySpeed: output = "float t = saturate((length(velocity) - SpeedRange.x) * SpeedRange.y);\n"; break;
                case CurveSampleMode.Random: output = "float t = RAND;\n"; break;
                case CurveSampleMode.RandomConstantPerParticle: output = "float t = FIXED_RAND(Seed);\n"; break;
                case CurveSampleMode.Custom: output = "float t = SampleTime;\n"; break;
                default:
                    throw new NotImplementedException("Invalid CurveSampleMode");
            }

            output += string.Format("float{0} value = 0.0f;\n", (size == 1) ? "" : size.ToString());

            if (computeMode == ComputeMode.Uniform || size == 1)
            {
                output += string.Format("value = SampleCurve({0}, t);\n", localName);
            }
            else
            {
                if (currentAttribute.Equals(VFXAttribute.Color))
                {
                    output += string.Format("value = SampleGradient({0}, t);\n", localName);
                }
                else
                {
                    if (size > 0) output += string.Format("value[0] = SampleCurve({0}, t);\n", localName + "_x");
                    if (size > 1) output += string.Format("value[1] = SampleCurve({0}, t);\n", localName + "_y");
                    if (size > 2) output += string.Format("value[2] = SampleCurve({0}, t);\n", localName + "_z");
                    if (size > 3) output += string.Format("value[3] = SampleCurve({0}, t);\n", localName + "_w");
                }
            }

            return output;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var attrib = currentAttribute;

                int size = VFXExpression.TypeToSize(attrib.type);
                if (attrib.variadic == VFXVariadic.True)
                    size = channels.ToString().Length;

                string localName = GenerateLocalAttributeName(attrib.name);
                if (Mode == ComputeMode.Uniform || size == 1)
                {
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName), VFXResources.defaultResources.animationCurve);
                }
                else
                {
                    if (attrib.Equals(VFXAttribute.Color))
                    {
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Gradient), localName), VFXResources.defaultResources.gradient);
                    }
                    else
                    {
                        if (size > 0) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_x"), VFXResources.defaultResources.animationCurve);
                        if (size > 1) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_y"), VFXResources.defaultResources.animationCurve);
                        if (size > 2) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_z"), VFXResources.defaultResources.animationCurve);
                        if (size > 3) yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), localName + "_w"), VFXResources.defaultResources.animationCurve);
                    }
                }

                if (SampleMode == CurveSampleMode.BySpeed)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "SpeedRange", new VFXPropertyAttribute[] { new VFXPropertyAttribute(VFXPropertyAttribute.Type.kMin, 0.0f) }));
                else if (SampleMode == CurveSampleMode.Custom)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "SampleTime"));
                else if (SampleMode == CurveSampleMode.RandomConstantPerParticle)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "Seed"));

                if (Composition == AttributeCompositionMode.Blend || (attrib.Equals(VFXAttribute.Color) && AlphaComposition == AttributeCompositionMode.Blend))
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "Blend"));
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                VFXExpression speedRange = null;
                foreach (var p in GetExpressionsFromSlots(this))
                {
                    if (p.name == "SpeedRange")
                        speedRange = p.exp;
                    else
                        yield return p;
                }

                if (SampleMode == CurveSampleMode.BySpeed)
                {
                    var speedRangeComponents = VFXOperatorUtility.ExtractComponents(speedRange).ToArray();
                    speedRangeComponents[1] = VFXOperatorUtility.OneExpression[VFXValueType.Float] / (speedRangeComponents[1] - speedRangeComponents[0]);
                    yield return new VFXNamedExpression(new VFXExpressionCombine(speedRangeComponents), "SpeedRange");
                }
            }
        }

        private VFXAttribute currentAttribute
        {
            get
            {
                return VFXAttribute.Find(attribute);
            }
        }
    }
}
