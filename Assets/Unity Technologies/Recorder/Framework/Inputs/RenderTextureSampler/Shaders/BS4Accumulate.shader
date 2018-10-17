// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/BeautyShot/Accumulate" {
Properties {
	_MainTex("Diffuse", 2D) = "white" {}
    _OfsX("OfsX", Float) = 0
    _OfsY("OfsY", Float) = 0
    _Width("Width", Float) = 1
    _Height("Height", Float) = 1
    _Scale("Scale", Float) = 1
    _Pass("Pass", int) = 0
}

CGINCLUDE

// FIXME: Had to comment out to make it work on OSX. Needs to be revised.
// #pragma only_renderers d3d11 ps4 opengl

#include "UnityCG.cginc"

struct v2f {
	float4 pos	: SV_Position;
	float2 uv	: TEXCOORD0;
};

v2f vert(appdata_img v)  {
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = v.texcoord;
	return o;
}

uniform sampler2D _MainTex;
uniform float4 _MainTex_TexelSize;

uniform sampler2D _PreviousTexture;
Float _OfsX;
Float _OfsY;
Float _Width;
Float _Height;
Float _Scale;
int _Pass;

float4 frag(v2f i) : SV_Target {
	float4 previous = tex2D(_PreviousTexture, i.uv);
    float2 tmp = i.uv;
    float4 current = {0,0,0,0};
    if(  i.uv.x >= _OfsX && i.uv.x <= (_OfsX+ _Width) && i.uv.y >= _OfsY && i.uv.y <= (_OfsY + _Height))
    {
        tmp.x = (tmp.x - _OfsX) / _Scale;
        tmp.y = (tmp.y - _OfsY) / _Scale;
        current = tex2D(_MainTex, tmp);

        if (_Pass == 0)
            return current;
        else
            return previous + current;
    }
    else
        return previous;
}

ENDCG

SubShader {
	Cull Off ZTest Always ZWrite Off

	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		ENDCG
	}
}}