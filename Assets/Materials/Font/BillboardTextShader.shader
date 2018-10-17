// Tweak of the standard font shader that renders text with fixed screen space size 
// but still taking the z position of the object and do z test

Shader "GUI/Billboard Text Shader"
{
	Properties
	{
		_MainTex("Font Texture", 2D) = "white" {}
		_Color("Text Color", Color) = (1,1,1,1)
		_ScreenOffsetX("Screen Offset X", Float) = 0
		_ScreenOffsetY("Screen Offset Y", Float) = 0
	}

	SubShader
	{

		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
		}

		Lighting Off 
		Cull Off 
		ZTest Always 
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			ZTest LEqual
			CGPROGRAM

#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform fixed4 _Color;
			uniform float _ScreenOffsetX;
			uniform float _ScreenOffsetY;

			v2f vert(appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(unity_ObjectToWorld[3]);
				o.vertex /= o.vertex.w;

				float aspect = _ScreenParams.y / _ScreenParams.x;
				float2 scale = float2(0.02f, -0.02f) * aspect;

				o.vertex.xy += v.vertex.xy * scale + float2(_ScreenOffsetX / _ScreenParams.x, _ScreenOffsetY / _ScreenParams.y);

				o.color = v.color * _Color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = i.color;
				col.a *= tex2D(_MainTex, i.texcoord).a;
				return col;
			}
			ENDCG
		}
	}
}
