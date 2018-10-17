Shader "Hidden/Recorder/Inputs/CBRenderTexture/CopyFB" {

CGINCLUDE
#include "UnityCG.cginc"
#pragma multi_compile ___ UNITY_HDR_ON
#pragma multi_compile ___ OFFSCREEN
#pragma multi_compile ___ TRANSPARENCY_ON

sampler2D _TmpFrameBuffer;
sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;
sampler2D _CameraGBufferTexture3;
sampler2D_float _CameraDepthTexture;
sampler2D_half _CameraMotionVectorsTexture;
sampler2D _TmpRenderTarget;

struct v2f {
    float4 pos : POSITION;
    float4 spos : TEXCOORD0;
};

v2f vert(appdata_img v)
{
    v2f o;
    o.pos = o.spos = v.vertex;
    return o;
}


float2 get_texcoord(v2f i)
{
    float2 t = i.spos.xy * 0.5 + 0.5;
    return t;
}

float2 get_texcoord_gb(v2f i)
{
    float2 t = i.spos.xy * 0.5 + 0.5;
#if !defined(UNITY_UV_STARTS_AT_TOP)
    t.y = 1.0 - t.y;
#endif
    return t;
}


half4 copy_framebuffer(v2f I) : SV_Target
{
    float2 t = get_texcoord(I);
#if !defined(OFFSCREEN) //|| !defined(UNITY_UV_STARTS_AT_TOP)
    t.y = 1.0 - t.y;
#endif
    half4 O = tex2D(_TmpFrameBuffer, t);
#if !defined(TRANSPARENCY_ON)
    O.a = 1.0;
#endif
    return O;
}

// render target (for offscreen-recorder)
half4 copy_rendertarget(v2f I) : SV_Target
{
    half4 O = tex2D(_TmpRenderTarget, get_texcoord_gb(I));
    return O;
}


// gbuffer
struct gbuffer_out
{
    half4 albedo        : SV_Target0;
    half4 occlusion     : SV_Target1;
    half4 specular      : SV_Target2;
    half4 smoothness    : SV_Target3;
    half4 normal        : SV_Target4;
    half4 emission      : SV_Target5;
    half4 depth         : SV_Target6;
};
gbuffer_out copy_gbuffer(v2f I)
{
    float2 t = get_texcoord_gb(I);
    half4 ao = tex2D(_CameraGBufferTexture0, t);
    half4 ss = tex2D(_CameraGBufferTexture1, t);
    half4 normal = tex2D(_CameraGBufferTexture2, t);
    half4 emission = tex2D(_CameraGBufferTexture3, t);
    half depth = tex2D(_CameraDepthTexture, get_texcoord_gb(I));
#if defined(UNITY_REVERSED_Z)
    depth = 1.0 - depth;
#endif

    gbuffer_out O;
    O.albedo = half4(ao.rgb, 1.0);
    O.occlusion = half4(ao.aaa, 1.0);
    O.specular = half4(ss.rgb, 1.0);
    O.smoothness = half4(ss.aaa, 1.0);
    O.normal = half4(normal.rgb, 1.0);
    O.emission = emission;
#ifndef UNITY_HDR_ON
    O.emission.rgb = -log2(O.emission.rgb);
#endif
    O.depth = half4(depth.rrr, 1.0);
    return O;
}


// clear gbuffer
struct clear_out
{
    half4 gb0   : SV_Target0;
    half4 gb1   : SV_Target1;
    half4 gb2   : SV_Target2;
    half4 gb3   : SV_Target3;
    float depth : SV_Depth;
};
clear_out clear_gbuffer(v2f I)
{
    clear_out O;
    O.gb0.xyzw = 0.0;
    O.gb1.xyzw = 0.0;
    O.gb2.xyzw = 0.0;
    O.gb3.xyzw = 0.0;
    O.depth = 1.0;
    return O;
}

// velocity
struct velocity_out
{
    half4 velocity : SV_Target0;
};
velocity_out copy_velocity(v2f I)
{
    float2 t = get_texcoord_gb(I);
    half2 velocity = tex2D(_CameraMotionVectorsTexture, t).rg;

    velocity_out O;
    O.velocity = half4(velocity, 1.0, 1.0);
    return O;
}

ENDCG

Subshader {
    // Pass 0: framebuffer
    Pass {
        Blend Off Cull Off ZTest Off ZWrite Off
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment copy_framebuffer
        ENDCG
    }

    // Pass 1: render target
    Pass {
        Blend Off Cull Off ZTest Off ZWrite Off
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment copy_rendertarget
        ENDCG
    }

    // Pass 2: gbuffer
    Pass{
        Blend Off Cull Off ZTest Off ZWrite Off
        CGPROGRAM
        #pragma exclude_renderers d3d9
        #pragma vertex vert
        #pragma fragment copy_gbuffer
        ENDCG
    }

    // Pass 3: clear
    Pass {
        Blend Off Cull Off ZTest Off ZWrite Off
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment clear_gbuffer
        ENDCG
    }

    // Pass 4: velocity
    Pass{
        Blend Off Cull Off ZTest Off ZWrite Off
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment copy_velocity
        ENDCG
    }
}

Fallback off
}
