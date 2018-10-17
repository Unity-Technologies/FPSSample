using System;

namespace UnityEngine.Recorder
{

    /// <summary>
    /// What is this: Standard industry frame rates.
    /// Motivation  : Some framerates are not correctly expressible as floats du to precision, so
    ///               so having an enum with the standard frame rates used in industry, allows us
    ///               to correctly carry precision in the settings. Precision loss is then the fault
    ///               of the components further down the line.
    /// </summary>
    [Flags]
    public enum EFrameRate
    {
        FR_CUSTOM = 1,
        FR_23 = 1 << 2, // 24 * 1000 / 1001
        FR_24 = 1 << 3,
        FR_25 = 1 << 4,
        FR_29 = 1 << 5, // 30 * 1000 / 1001,
        FR_30 = 1 << 6,
        FR_50 = 1 << 7,
        FR_59 = 1 << 8, // 60 * 1000 / 1001,
        FR_60 = 1 << 9
    }

    /// <summary>
    /// What is this: Utility class that converts  EFrameRate to text and to float 
    /// Motivation  : since the enum is expressed as integers, need something to provide associated float values
    ///               and also provide human readable lables for the UI.
    /// </summary>    
    public class FrameRateHelper
    {
        public static float ToFloat(EFrameRate value, float customValue)
        {
            switch (value)
            {
                case EFrameRate.FR_CUSTOM:
                    return customValue;
                case EFrameRate.FR_23:
                    return 24 * 1000 / 1001f;
                case EFrameRate.FR_24:
                    return 24;
                case EFrameRate.FR_25:
                    return 25;
                case EFrameRate.FR_29:
                    return 30 * 1000 / 1001f;
                case EFrameRate.FR_30:
                    return 30;
                case EFrameRate.FR_50:
                    return 50;
                case EFrameRate.FR_59:
                    return 60 * 1000 / 1001f;
                case EFrameRate.FR_60:
                    return 60;
                default:
                    throw new ArgumentOutOfRangeException("value", value, null);
            }
        }

        public static string ToLable(EFrameRate value)
        {
            switch (value)
            {
                case EFrameRate.FR_23:
                    return "23.97";
                case EFrameRate.FR_24:
                    return "Film (24)";
                case EFrameRate.FR_25:
                    return "PAL (25)";
                case EFrameRate.FR_29:
                    return "NTSC (29.97)";
                case EFrameRate.FR_30:
                    return "30";
                case EFrameRate.FR_50:
                    return "50";
                case EFrameRate.FR_59:
                    return "59.94" ;
                case EFrameRate.FR_60:
                    return "60";
                case EFrameRate.FR_CUSTOM:
                default:
                    return "(custom)";
            }
        }       
    }
}
