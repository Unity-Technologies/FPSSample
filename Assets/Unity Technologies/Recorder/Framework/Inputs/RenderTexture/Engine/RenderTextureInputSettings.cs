using System;
using System.Collections.Generic;

namespace UnityEngine.Recorder.Input
{
    public class RenderTextureInputSettings : ImageInputSettings
    {
        public RenderTexture m_SourceRTxtr;

        public override Type inputType
        {
            get { return typeof(RenderTextureInput); }
        }

        public override bool ValidityCheck(List<string> errors)
        {
            var ok = true;

            if (m_SourceRTxtr == null)
            {
                ok = false;
                errors.Add("Missing source render texture object/asset.");
            }

            return ok;
        }

    }
}
