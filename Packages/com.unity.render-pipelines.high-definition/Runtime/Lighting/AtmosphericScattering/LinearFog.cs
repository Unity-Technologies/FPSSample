using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class LinearFog : AtmosphericScattering
    {
        private readonly static int m_LinearFogParam = Shader.PropertyToID("_LinearFogParameters");

        public MinFloatParameter    fogStart = new MinFloatParameter(500.0f, 0.0f);
        public MinFloatParameter    fogEnd = new MinFloatParameter(1000.0f, 0.0f);

        public FloatParameter fogHeightStart = new FloatParameter(0.0f);
        public FloatParameter fogHeightEnd = new FloatParameter(10.0f);

        public override void PushShaderParameters(HDCamera hdCamera, CommandBuffer cmd)
        {
            PushShaderParametersCommon(hdCamera, cmd, FogType.Linear);
            cmd.SetGlobalVector(m_LinearFogParam, new Vector4(fogStart, 1.0f / (fogEnd - fogStart), fogHeightEnd, 1.0f / (fogHeightEnd - fogHeightStart)));
        }
    }
}
