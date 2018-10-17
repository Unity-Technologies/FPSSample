#if UNITY_2017_3_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Recorder.Input
{
    public class ScreenCaptureInputSettings : ImageInputSettings
    {
        public override Type inputType
        {
            get { return typeof(ScreenCaptureInput); }
        }

        public override bool ValidityCheck( List<string> errors )
        {
            return true;
        }
    }
}

#endif