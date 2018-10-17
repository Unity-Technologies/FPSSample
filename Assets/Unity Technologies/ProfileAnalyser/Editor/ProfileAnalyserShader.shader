Shader "Unlit/ProfileAnalyserShader"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" }
		LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
                fixed4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
			};

            v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.color.rgba = v.color;
				return o;
			}

            fixed4 frag (v2f i) : SV_Target { return i.color; }
			ENDCG
		}
	}
}
