#ifndef __CLUSTEREDUTILS_H__
#define __CLUSTEREDUTILS_H__

float GetScaleFromBase(float base)
{
    const float C = (float)(1 << g_iLog2NumClusters);
    const float geomSeries = (1.0 - PositivePow(base, C)) / (1 - base);     // geometric series: sum_k=0^{C-1} base^k
    return geomSeries / (g_fFarPlane - g_fNearPlane);
}

float LogBase(float x, float b)
{
    return log2(x) / log2(b);
}

int SnapToClusterIdxFlex(float z_in, float suggestedBase, bool logBasePerTile)
{
#if USE_LEFT_HAND_CAMERA_SPACE
    float z = z_in;
#else
    float z = -z_in;
#endif

    //float userscale = g_fClustScale;
    //if (logBasePerTile)
    //    userscale = GetScaleFromBase(suggestedBase);

    // using the inverse of the geometric series
    //const float dist = max(0, z - g_fNearPlane);
    //return (int)clamp(log2(dist * userscale * (suggestedBase - 1.0f) + 1) / log2(suggestedBase), 0.0, (float)((1 << g_iLog2NumClusters) - 1));

    const int C = 1 << g_iLog2NumClusters;
    const float rangeFittedDistance = max(0, z - g_fNearPlane) / (g_fFarPlane - g_fNearPlane);
    return (int)clamp( LogBase( lerp(1.0, PositivePow(suggestedBase, (float) C), rangeFittedDistance), suggestedBase), 0.0, (float)(C - 1));
}

int SnapToClusterIdx(float z_in, float suggestedBase)
{
#ifdef ENABLE_DEPTH_TEXTURE_BACKPLANE
    bool logBasePerTile = true;     // resolved compile time
#else
    bool logBasePerTile = false;
#endif

    return SnapToClusterIdxFlex(z_in, suggestedBase, logBasePerTile);
}

float ClusterIdxToZFlex(int k, float suggestedBase, bool logBasePerTile)
{
    float res;

    //float userscale = g_fClustScale;
    //if (logBasePerTile)
    //    userscale = GetScaleFromBase(suggestedBase);

    //float dist = (PositivePow(suggestedBase, (float)k) - 1.0) / (userscale * (suggestedBase - 1.0f));
    //res = dist + g_fNearPlane;

    const float C = (float)(1 << g_iLog2NumClusters);
    float rangeFittedDistance = (PositivePow(suggestedBase, (float)k) - 1.0) / (PositivePow(suggestedBase, C) - 1.0);
    res = lerp(g_fNearPlane, g_fFarPlane, rangeFittedDistance);


#if USE_LEFT_HAND_CAMERA_SPACE
    return res;
#else
    return -res;
#endif
}

float ClusterIdxToZ(int k, float suggestedBase)
{
#ifdef ENABLE_DEPTH_TEXTURE_BACKPLANE
    bool logBasePerTile = true;     // resolved compile time
#else
    bool logBasePerTile = false;
#endif

    return ClusterIdxToZFlex(k, suggestedBase, logBasePerTile);
}

// generate a log-base value such that half of the clusters are consumed from near plane to max. opaque depth of tile.
float SuggestLogBase50(float tileFarPlane)
{
    const float C = (float)(1 << g_iLog2NumClusters);
    float rangeFittedDistance = clamp((tileFarPlane - g_fNearPlane) / (g_fFarPlane - g_fNearPlane), FLT_EPS, 1.0);
    float suggested_base = pow((1.0 + sqrt(max(0.0, 1.0 - 4.0 * rangeFittedDistance * (1.0 - rangeFittedDistance)))) / (2.0 * rangeFittedDistance), 2.0 / C);      //
    return max(g_fClustBase, suggested_base);
}

// generate a log-base value such that (approximately) a quarter of the clusters are consumed from near plane to max. opaque depth of tile.
float SuggestLogBase25(float tileFarPlane)
{
    const float C = (float)(1 << g_iLog2NumClusters);
    float rangeFittedDistance = clamp((tileFarPlane - g_fNearPlane) / (g_fFarPlane - g_fNearPlane), FLT_EPS, 1.0);
    float suggested_base = pow((1 / 2.3) * max(0.0, (0.8 / rangeFittedDistance) - 1), 4.0 / (C * 2));     // approximate inverse of d*x^4 + (-x) + (1-d) = 0       - d is normalized distance
    return max(g_fClustBase, suggested_base);
}

uint GenerateLogBaseBufferIndex(uint2 tileIndex, uint numTilesX, uint numTilesY, uint eyeIndex)
{
    uint eyeOffset = eyeIndex * numTilesX * numTilesY;
    return (eyeOffset + (tileIndex.y * numTilesX) + tileIndex.x);
}

uint GenerateLayeredOffsetBufferIndex(uint lightCategory, uint2 tileIndex, uint clusterIndex, uint numTilesX, uint numTilesY, int numClusters, uint eyeIndex)
{
    // Each eye is split into category, cluster, x, y

    uint eyeOffset = eyeIndex * LIGHTCATEGORY_COUNT * numClusters * numTilesX * numTilesY;
    int lightOffset = ((lightCategory * numClusters + clusterIndex) * numTilesY + tileIndex.y) * numTilesX + tileIndex.x;

    return (eyeOffset + lightOffset);
}

#endif
