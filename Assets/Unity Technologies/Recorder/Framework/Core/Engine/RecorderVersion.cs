using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Recorder
{
    public class RecorderVersion : ScriptableObject
    {
        public const string Version = "0.2";  // major.minor.build
        public static int BuildNumber = 32;
        public static string Tag
        {
            get { return string.Format("{0}.{1:0000}", Version, BuildNumber); }
        }
        public const string Stage = "(experimental)";
    }
}
