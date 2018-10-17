Shader "Debug/Line3DShaderProc" {
	Properties
	{
	}
	SubShader
	{
		Pass
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }

		ZWrite on
		ZTest Less
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
		float4 start;
		float4 end;
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
		float4 start = positionBuffer[instID].start;
		float4 end = positionBuffer[instID].end;
		float4 color = positionBuffer[instID].color;

		float4 dir = end - start;
		float3 startDir = start - _WorldSpaceCameraPos;
		float3 endDir = end - _WorldSpaceCameraPos;

		float3 offset = v_pos.y*normalize(cross(dir, endDir))*length(endDir)*0.5 + (1 - v_pos.y)*normalize(cross(dir, startDir))*length(startDir)*0.5;

		float pointScale = 0.01f;
		float4 p = (start + dir*v_pos.y) + float4(offset,0) * v_pos.x * pointScale;

		
		float4 clipPos = UnityObjectToClipPos(p);

		v2f o;
		o.pos = clipPos;
		o.color = color;
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		return float4(i.color.rgb,1);
	}

		ENDCG
	}
	}
}



//Shader "Unlit/NewUnlitShader"
//{
//	Properties
//	{
//		_MainTex ("Texture", 2D) = "white" {}
//	}
//	SubShader
//	{
//		Tags { "RenderType"="Opaque" }
//		LOD 100
//
//		Pass
//		{
//			CGPROGRAM
//			#pragma vertex vert
//			#pragma fragment frag
//			// make fog work
//			#pragma multi_compile_fog
//			
//			#include "UnityCG.cginc"
//
//			struct appdata
//			{
//				float4 vertex : POSITION;
//				float2 uv : TEXCOORD0;
//			};
//
//			struct v2f
//			{
//				float2 uv : TEXCOORD0;
//				UNITY_FOG_COORDS(1)
//				float4 vertex : SV_POSITION;
//			};
//
//			sampler2D _MainTex;
//			float4 _MainTex_ST;
//			
//			v2f vert (appdata v)
//			{
//				v2f o;
//				o.vertex = UnityObjectToClipPos(v.vertex);
//				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
//				UNITY_TRANSFER_FOG(o,o.vertex);
//				return o;
//			}
//			
//			fixed4 frag (v2f i) : SV_Target
//			{
//				// sample the texture
//				fixed4 col = tex2D(_MainTex, i.uv);
//				// apply fog
//				UNITY_APPLY_FOG(i.fogCoord, col);
//				return col;
//			}
//			ENDCG
//		}
//	}
//}

