Shader "Hidden/Unity/Recorder/Custom/VerticalFlipper" {
    Properties{_MainTex("Texture", any) = "" {}}

CGINCLUDE
#include "UnityCG.cginc"

uniform sampler2D _MainTex;

struct v2f
{
    float4 pos	: SV_Position;
    float2 uv	: TEXCOORD0;
};

v2f vert(appdata_img v)
{
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord;
    return o;
}

half4 copy_and_flip_verticaly(v2f i) : SV_Target{
    float2 uv = i.uv;
    uv.y = 1.0 - uv.y;
    return tex2D(_MainTex, uv);
}

ENDCG
   
Subshader  {
    Pass{
        Blend Off Cull Off ZTest Off ZWrite Off
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment copy_and_flip_verticaly
        ENDCG
    }
}

Fallback off
}
