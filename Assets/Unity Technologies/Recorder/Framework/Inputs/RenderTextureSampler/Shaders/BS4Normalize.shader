Shader "Hidden/BeautyShot/Normalize" {
	Properties { _MainTex ("Texture", any) = "" {} }

	SubShader { 
		Pass {
 			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			uniform float4 _MainTex_ST;

			float _NormalizationFactor;
            int _ApplyGammaCorrection;

            float floatToGammaSpace(float value)
            {
                if (value <= 0.0)
                    return 0.0F;
                else if (value <= 0.0031308)
                    return 12.92 * value;
                else if (value < 1.0)
                    return 1.055 * pow(value, 0.4166667) - 0.055;
                else if (value == 1.0)
                    return 1.0;
                else
                    return pow(value, 0.45454545454545);
            }

            float4 float4ToGammaSpace(float4 value)
            {
                float4 gammaValue;

                gammaValue[0] = floatToGammaSpace(value[0]);
                gammaValue[1] = floatToGammaSpace(value[1]);
                gammaValue[2] = floatToGammaSpace(value[2]);
                gammaValue[3] = value[3];

                return gammaValue;
            }

            struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 mainTex = tex2D(_MainTex, i.texcoord);
                if(_ApplyGammaCorrection == 0)
                     return mainTex * _NormalizationFactor;
                else
                    return float4ToGammaSpace( mainTex * _NormalizationFactor );
			}
			ENDCG 

		}
	}
	Fallback Off 
}
