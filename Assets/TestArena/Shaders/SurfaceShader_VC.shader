Shader "Custom/SurfaceShader_VC" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Normal("Normap Map", 2D) = "bump" {}
        _InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0

	}
		SubShader{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 200
		Blend One OneMinusSrcAlpha
		ColorMask RGB

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf Standard fullforwardshadows vertex:vert alpha:fade
//#pragma multi_compile _ SOFTPARTICLES_ON

		// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

	sampler2D _MainTex;
	sampler2D _Normal;

	UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

	float _InvFade;

	struct Input {
		float2 uv_MainTex;
		float4 vertex : SV_POSITION;
		float4 color : COLOR;
		float4 projPos : TEXCOORD2;
	};

	void vert(inout appdata_full v, out Input o)
	{
		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.color = v.color;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.projPos = ComputeScreenPos(o.vertex);
		COMPUTE_EYEDEPTH(o.projPos.z);
	}

	fixed4 _Color;

	void surf(Input IN, inout SurfaceOutputStandard o) {
		// Albedo comes from a texture tinted by color
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
		o.Albedo = c.rgb*IN.color;
		o.Normal = UnpackNormal(tex2D(_Normal, IN.uv_MainTex));
		float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.projPos)));
		float partZ = IN.projPos.z;
		float fade = saturate(_InvFade * (sceneZ - partZ));
		o.Alpha = c.a*fade*IN.color.a;
	}
	ENDCG
	}
		FallBack "Diffuse"
}
