Shader "Instanced/LineShaderProc" {
	Properties
	{
	}
	SubShader
	{
		Pass
		{
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

			float4 scales; // scale (x,y)

			struct instanceData
			{
				float4 position;
				float4 color;
			};

			StructuredBuffer<instanceData> positionBuffer;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 color : TEXCOORD3;
			};

			v2f vert(uint vid : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				// We just draw a bunch of vertices but want to pretend to
				// be drawing two-triangle quads. Build inst/vert id for this:
				int instID = vid / 6.0;
				int vertID = vid - instID * 6;
				
				// Generates (0,0) (1,0) (1,1) (1,1) (0,1) (0,0) from vertID
				float4 v_pos = saturate(float4(2 - abs(vertID - 2), 2 - abs(vertID - 3), 0, 0));

				// Center around y
				v_pos.x -= 0.5;

				// Read instance data
				float4 pos   = positionBuffer[instID].position;
				float4 color = positionBuffer[instID].color;

				// Generate position
				float2 dir = pos.zw - pos.xy;
				float2 pdir = normalize(float2(-dir.y, dir.x));
				float2 p = (pos.xy + dir*v_pos.y)*scales.xy + pdir * 3.0 * v_pos.x * scales.zw;
				p = float2(-1, -1) + p * 2.0;

				// Need to flip this for HD pipe for some reason
				p.y *= -1;

				v2f o;
				o.pos = float4(p.xy, 1, 1);
				o.color = color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float4 albedo = float4(1,1,1,1);
				fixed4 output = albedo * float4(i.color.rgb, 1);
				return output;
			}

			ENDCG
		}
	}
}
