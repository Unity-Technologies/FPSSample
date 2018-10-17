#ifndef __LIGHTCULLUTILS_H__
#define __LIGHTCULLUTILS_H__

// Used to index into our SFiniteLightBound (g_data) and
// LightVolumeData (_LightVolumeData) buffers.
int GenerateLightCullDataIndex(int lightIndex, uint numVisibleLights, uint eyeIndex)
{
    // For monoscopic, there is just one set of light cull data structs.
    // In stereo, all of the left eye structs are first, followed by the right eye structs.
    const int perEyeBaseIndex = (int)eyeIndex * (int)numVisibleLights;
    return (perEyeBaseIndex + lightIndex);
}

struct ScreenSpaceBoundsIndices
{
    int min;
    int max;
};

// The returned values are used to index into our AABB screen space bounding box buffer
// Usually named g_vBoundsBuffer.  The two values represent the min/max indices.
ScreenSpaceBoundsIndices GenerateScreenSpaceBoundsIndices(int lightIndex, uint numVisibleLights, uint eyeIndex)
{
    // In the monoscopic mode, there is one set of bounds (min,max -> 2 * g_iNrVisibLights)
    // In stereo, there are two sets of bounds (leftMin, leftMax, rightMin, rightMax -> 4 * g_iNrVisibLights)
    const int eyeRelativeBase = (int)eyeIndex * 2 * (int)numVisibleLights;

    ScreenSpaceBoundsIndices indices;
    indices.min = eyeRelativeBase + lightIndex;
    indices.max = eyeRelativeBase + lightIndex + (int)numVisibleLights;

    return indices;
}

#endif //__LIGHTCULLUTILS_H__
