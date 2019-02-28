Shader "Hidden/UnlitTransparentColored" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
    }

    SubShader {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        
        ZWrite Off
        Lighting Off
        Fog { Mode Off }
		Cull Off

        Blend SrcAlpha OneMinusSrcAlpha 

        Pass {
            Color [_Color]
        }
    }
}