Shader "Instanced/GlyphShaderProc" {
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
	}
	SubShader{

		Pass{

			Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }

			ZWrite off
			ZTest Always
			Cull off
			//Blend One One
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma target 4.5

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 scales; // glyph scale in world (x,y) and on texture (z,w)

			struct instanceData
			{
				float4 position;
				float4 size;
				float4 color;
			};

			StructuredBuffer<instanceData> positionBuffer;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv_MainTex : TEXCOORD0;
				float4 color : TEXCOORD3;
			};

			v2f vert(uint vid : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				// We just draw a bunch of vertices but want to pretend to
				// be drawing two-triangle quads. Build inst/vert id for this:
				int instID = vid / 6.0;
				int vertID = vid - instID * 6;
				
				// Generates (0,0) (1,0) (1,1) (1,1) (1,0) (0,0) from vertID
				float4 v_pos = saturate(float4(2 - abs(vertID - 2), 2 - abs(vertID - 3), 0, 0));

				// Read instance data
				float4 pos_uv = positionBuffer[instID].position;
				float2 scale = positionBuffer[instID].size.xy;
				float4 color = positionBuffer[instID].color;

				// Generate uv
				float2 uv = (pos_uv.zw + v_pos.xy) * scales.zw;
				uv.y = 1.0 - uv.y;

				// Generate position
				float2 p = (v_pos*scale + pos_uv.xy) * scales.xy;
				p = float2(-1, -1) + p * 2.0;

				// Need to flip y for HD pipe for some reason
				p.y *= -1;

				v2f o;
				o.pos = float4(p.xy, 1, 1);
				o.uv_MainTex = uv;
				o.color = color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float4 albedo = float4(1,1,1,1);
				// TODO fix up this hack
				if (length(i.uv_MainTex) > 0)
				{
					albedo = tex2D(_MainTex, i.uv_MainTex);
					albedo = lerp(albedo, float4(1, 1, 1, 1), i.color.a);
				}
				fixed4 output = albedo * float4(i.color.rgb, 1);
				return output;
			}

		ENDCG
	}
	}
}
