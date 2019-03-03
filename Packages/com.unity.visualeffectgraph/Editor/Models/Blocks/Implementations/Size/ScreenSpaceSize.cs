using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Size")]
    class ScreenSpaceSize : VFXBlock
    {
        public enum SizeMode
        {
            PixelAbsolute,
            PixelRelativeToResolution,
            RatioRelativeToHeight,
            RatioRelativeToWidth,
            RatioRelativeToHeightAndWidth
        }

        public enum SizeZMode
        {
            Ignore,
            SameAsSizeX,
            SameAsSizeY,
            MinOfSizeXY,
            MaxOfSizeXY,
            AverageOfSizeXY
        }

        public class InputPropertiesAbsolute
        {
            public float PixelSize = 16.0f;
        }

        public class InputPropertiesRelative
        {
            public float RelativeSize = 0.1f;
        }

        public class InputPropertiesResolution
        {
            public Vector2 ReferenceResolution = new Vector2(1920, 1080);
        }

        [SerializeField, VFXSetting]
        protected SizeMode sizeMode = SizeMode.PixelAbsolute;

        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        protected SizeZMode sizeZMode = SizeZMode.SameAsSizeX;

        public override string name { get { return "Screen Space Size"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                switch (sizeMode)
                {
                    case SizeMode.PixelAbsolute: return PropertiesFromType("InputPropertiesAbsolute");
                    case SizeMode.PixelRelativeToResolution: return PropertiesFromType("InputPropertiesResolution").Concat(PropertiesFromType("InputPropertiesAbsolute"));
                    case SizeMode.RatioRelativeToHeight:
                    case SizeMode.RatioRelativeToHeightAndWidth:
                    case SizeMode.RatioRelativeToWidth:
                        return PropertiesFromType("InputPropertiesRelative");
                    default:
                        throw new NotImplementedException(string.Format("Not Implemented SizeMode: {0}", sizeMode));
                }
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                if (!GetData().IsCurrentAttributeRead(VFXAttribute.ScaleZ))
                    yield return "sizeZMode";
            }
        }


        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);

                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.ReadWrite);

                if (GetData().IsCurrentAttributeWritten(VFXAttribute.ScaleY) || sizeMode == SizeMode.RatioRelativeToHeightAndWidth || sizeMode == SizeMode.PixelRelativeToResolution)
                    yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.ReadWrite);

                // if ScaleZ is used, we need to scale it too, in an uniform way.
                if (GetData().IsCurrentAttributeRead(VFXAttribute.ScaleZ) && sizeZMode != SizeZMode.Ignore)
                    yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.ReadWrite);
            }
        }

        public override string source
        {
            get
            {
                string sizeString = string.Empty;
                switch (sizeMode)
                {
                    case SizeMode.PixelAbsolute:                          sizeString = "float2(PixelSize, PixelSize)";                                            break;
                    case SizeMode.PixelRelativeToResolution:              sizeString = "float2(PixelSize, PixelSize) * (_ScreenParams.xy/ReferenceResolution)";   break;
                    case SizeMode.RatioRelativeToWidth:                   sizeString = "float2(_ScreenParams.x, _ScreenParams.x) * RelativeSize";                 break;
                    case SizeMode.RatioRelativeToHeight:                  sizeString = "float2(_ScreenParams.y, _ScreenParams.y) * RelativeSize";                 break;
                    case SizeMode.RatioRelativeToHeightAndWidth:          sizeString = "float2(_ScreenParams.x, _ScreenParams.y) * RelativeSize";                 break;
                    default: throw new NotImplementedException(string.Format("Not Implemented SizeMode: {0}", sizeMode));
                }

                string Source = string.Format(@"
float2 localSize = {0};
float clipPosW = TransformPositionVFXToClip(position).w;
float minSize = clipPosW / (0.5f * min(UNITY_MATRIX_P[0][0] * _ScreenParams.x,-UNITY_MATRIX_P[1][1] * _ScreenParams.y)); // max size in one pixel
float2 scale = {2};
localSize = minSize * scale;
{1}
",
                    VFXBlockUtility.GetSizeVector(GetParent(), 2),
                    VFXBlockUtility.SetSizesFromVector(GetParent(), "localSize", 2),
                    sizeString);

                if (GetData().IsCurrentAttributeRead(VFXAttribute.ScaleZ) && sizeZMode != SizeZMode.Ignore)
                {
                    switch (sizeZMode)
                    {
                        case SizeZMode.Ignore: break; // should not happen
                        case SizeZMode.SameAsSizeX:     Source += "scaleZ = localSize.x;"; break;
                        case SizeZMode.SameAsSizeY:     Source += "scaleZ = localSize.y;"; break;
                        case SizeZMode.MinOfSizeXY:     Source += "scaleZ = min(localSize.x,localSize.y);"; break;
                        case SizeZMode.MaxOfSizeXY:     Source += "scaleZ = max(localSize.x,localSize.y);"; break;
                        case SizeZMode.AverageOfSizeXY: Source += "scaleZ = (localSize.x + localSize.y) * 0.5;"; break;
                    }
                }
                return Source;
            }
        }
    }
}
