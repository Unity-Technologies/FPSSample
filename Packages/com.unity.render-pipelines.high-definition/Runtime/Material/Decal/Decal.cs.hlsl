//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef DECAL_CS_HLSL
#define DECAL_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Decal+DBufferMaterial:  static fields
//
#define DBUFFERMATERIAL_COUNT (4)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Decal+DBufferHTileBit:  static fields
//
#define DBUFFERHTILEBIT_DIFFUSE (1)
#define DBUFFERHTILEBIT_NORMAL (2)
#define DBUFFERHTILEBIT_MASK (4)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Decal+DecalSurfaceData
// PackingRules = Exact
struct DecalSurfaceData
{
    float4 baseColor;
    float4 normalWS;
    float4 mask;
    float2 MAOSBlend;
    uint HTileMask;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.DecalData
// PackingRules = Exact
struct DecalData
{
    float4x4 worldToDecal;
    float4x4 normalToWorld;
    float4 diffuseScaleBias;
    float4 normalScaleBias;
    float4 maskScaleBias;
    float4 baseColor;
    float3 blendParams;
};


#endif
