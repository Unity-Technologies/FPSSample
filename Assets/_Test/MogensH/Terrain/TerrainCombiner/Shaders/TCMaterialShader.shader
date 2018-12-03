// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PockerHammer/TCMaterialShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Texture2 ("Alpha Material", 2D) = "white" {}
	}
	SubShader
    {

      	Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // vertex shader inputs
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0; 
            };

            struct v2f
            {
                float2 uv : TEXCOORD0; 
                float4 vertex : SV_POSITION; 
            };

            // vertex shader
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            sampler2D _Texture2;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);
                fixed4 alphaTexColor = tex2D(_Texture2, i.uv);

				float alpha = alphaTexColor.r;
				float val = texColor.r;

				// Scale value so sum of all layers always is one (even if one layer is used as alpha)
				val = val/(1.0f - alpha);

				texColor.r = val;
				texColor.a = 1.0f - alpha;
                return texColor;
            }
            ENDCG
        }
    }
}
