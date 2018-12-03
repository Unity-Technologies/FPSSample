// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PockerHammer/TCHeightmapShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_HeighOffset ("HeightOffset", Range (-1.0,1.0)) = 0.0
		_HeightScale ("HeightScale", Range (-1.0,1.0)) = 1.0
	}
	SubShader
    {

        //BlendOp Max // Max
      	Blend One One // Additive blending
      	
      	
        Pass
        {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            float _HeighOffset;
            float _HeightScale;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col.r = (col.r + _HeighOffset)*_HeightScale;
                return col;
            }
            ENDCG
        }
    }
}
