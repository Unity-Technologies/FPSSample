//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef UNLIT_CS_HLSL
#define UNLIT_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Unlit+SurfaceData:  static fields
//
#define DEBUGVIEW_UNLIT_SURFACEDATA_COLOR (300)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Unlit+BSDFData:  static fields
//
#define DEBUGVIEW_UNLIT_BSDFDATA_COLOR (350)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Unlit+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    float3 color;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Unlit+BSDFData
// PackingRules = Exact
struct BSDFData
{
    float3 color;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_UNLIT_SURFACEDATA_COLOR:
            result = surfacedata.color;
            needLinearToSRGB = true;
            break;
    }
}

//
// Debug functions
//
void GetGeneratedBSDFDataDebug(uint paramId, BSDFData bsdfdata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_UNLIT_BSDFDATA_COLOR:
            result = bsdfdata.color;
            needLinearToSRGB = true;
            break;
    }
}


#endif
