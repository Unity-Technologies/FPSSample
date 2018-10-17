#ifndef __LIGHTINGCONVEXHULLUTILS_H__
#define __LIGHTINGCONVEXHULLUTILS_H__

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.cs.hlsl"

float3 GetHullVertex(const float3 boxX, const float3 boxY, const float3 boxZ, const float3 center, const float2 scaleXY, const int p)
{
    const bool bIsTopVertex = (p&4)!=0;
    float3 vScales = float3( ((p&1)!=0 ? 1.0f : (-1.0f))*(bIsTopVertex ? scaleXY.x : 1.0), ((p&2)!=0 ? 1.0f : (-1.0f))*(bIsTopVertex ? scaleXY.y : 1.0), (p&4)!=0 ? 1.0f : (-1.0f) );
    return (vScales.x*boxX + vScales.y*boxY + vScales.z*boxZ) + center;
}

void GetHullEdge(out int idx0, out int idx_twin, out float3 vP0, out float3 vE0, const int e0, const float3 boxX, const float3 boxY, const float3 boxZ, const float3 center, const float2 scaleXY)
{
    int iAxis = e0>>2;
    int iSwizzle = e0&0x3;
    bool bIsSwizzleOneOrTwo = ((iSwizzle-1)&0x2)==0;

    const int i0 = iAxis==0 ? (2*iSwizzle+0) : ( iAxis==1 ? (iSwizzle+(iSwizzle&2)) : iSwizzle);
    const int i1 = i0 + (1<<iAxis);
    const bool bSwap = iAxis==0 ? (!bIsSwizzleOneOrTwo) : (iAxis==1 ? false : bIsSwizzleOneOrTwo);

    idx0 = bSwap ? i1 : i0;
    idx_twin = bSwap ? i0 : i1;
    float3 p0 = GetHullVertex(boxX, boxY, boxZ, center, scaleXY, idx0);
    float3 p1 = GetHullVertex(boxX, boxY, boxZ, center, scaleXY, idx_twin);

    vP0 = p0;
    vE0 = p1-p0;
}

void GetHullQuad(out float3 p0, out float3 p1, out float3 p2, out float3 p3, const float3 boxX, const float3 boxY, const float3 boxZ, const float3 center, const float2 scaleXY, const int sideIndex)
{
    //const int iAbsSide = (sideIndex == 0 || sideIndex == 1) ? 0 : ((sideIndex == 2 || sideIndex == 3) ? 1 : 2);
    const int iAbsSide = min(sideIndex>>1, 2);
    const float fS = (sideIndex & 1) != 0 ? 1 : (-1);

    float3 vA = fS*(iAbsSide == 0 ? boxX : (iAbsSide == 1 ? (-boxY) : boxZ));
    float3 vB = fS*(iAbsSide == 0 ? (-boxY) : (iAbsSide == 1 ? (-boxX) : (-boxY)));
    float3 vC = iAbsSide == 0 ? boxZ : (iAbsSide == 1 ? boxZ : (-boxX));

    bool bIsTopQuad = iAbsSide == 2 && (sideIndex & 1) != 0;        // in this case all 4 verts get scaled.
    bool bIsSideQuad = (iAbsSide == 0 || iAbsSide == 1);        // if side quad only two verts get scaled (impacts q1 and q2)

    if (bIsTopQuad) { vB *= scaleXY.y; vC *= scaleXY.x; }

    float3 vA2 = vA;
    float3 vB2 = vB;

    if (bIsSideQuad) { vA2 *= (iAbsSide == 0 ? scaleXY.x : scaleXY.y); vB2 *= (iAbsSide == 0 ? scaleXY.y : scaleXY.x); }

    // delivered counterclockwise in right hand space and clockwise in left hand space
    p0 = center + (vA + vB - vC);       // center + vA is center of face when scaleXY is 1.0
    p1 = center + (vA - vB - vC);
    p2 = center + (vA2 - vB2 + vC);
    p3 = center + (vA2 + vB2 + vC);
}

void GetHullPlane(out float3 p0, out float3 n0, const float3 boxX, const float3 boxY, const float3 boxZ, const float3 center, const float2 scaleXY, const int sideIndex)
{
    //const int iAbsSide = (sideIndex == 0 || sideIndex == 1) ? 0 : ((sideIndex == 2 || sideIndex == 3) ? 1 : 2);
    const int iAbsSide = min(sideIndex>>1, 2);
    const float fS = (sideIndex & 1) != 0 ? 1 : (-1);

    float3 vA = fS*(iAbsSide == 0 ? boxX : (iAbsSide == 1 ? (-boxY) : boxZ));
    float3 vB = fS*(iAbsSide == 0 ? (-boxY) : (iAbsSide == 1 ? (-boxX) : (-boxY)));
    float3 vC = iAbsSide == 0 ? boxZ : (iAbsSide == 1 ? boxZ : (-boxX));

    bool bIsTopQuad = iAbsSide == 2 && (sideIndex & 1) != 0;        // in this case all 4 verts get scaled.
    bool bIsSideQuad = (iAbsSide == 0 || iAbsSide == 1);        // if side quad only two verts get scaled (impacts q1 and q2)

    if (bIsTopQuad) { vB *= scaleXY.y; vC *= scaleXY.x; }

    float3 vA2 = vA;
    float3 vB2 = vB;

    if (bIsSideQuad) { vA2 *= (iAbsSide == 0 ? scaleXY.x : scaleXY.y); vB2 *= (iAbsSide == 0 ? scaleXY.y : scaleXY.x); }

    float3 vN = cross(vB2, 0.5 * (vA - vA2) - vC);  // +/- normal
    float3 v0 = vA + vB - vC;   // vector from center to p0
    p0 = center + v0;           // center + vA is center of face when scaleXY is 1.0
    n0 = dot(vN,v0) < 0.0 ? (-vN) : vN;
}

float4 GetHullPlaneEq(const float3 boxX, const float3 boxY, const float3 boxZ, const float3 center, const float2 scaleXY, const int sideIndex)
{
    float3 p0, vN;
    GetHullPlane(p0, vN, boxX, boxY, boxZ, center, scaleXY, sideIndex);

    return float4(vN, -dot(vN,p0));
}

bool DoesSphereOverlapTile(float3 dir, float halfTileSizeAtZDistOne, float3 sphCen_in, float sphRadiusIn, bool isOrthographic)
{
    float3 V = float3(isOrthographic ? 0.0 : dir.x, isOrthographic ? 0.0 : dir.y, dir.z);     // ray direction down center of tile (does not need to be normalized).
    float3 sphCen = float3(sphCen_in.x - (isOrthographic ? dir.x : 0.0), sphCen_in.y - (isOrthographic ? dir.y : 0.0), sphCen_in.z);

#if 1
    float3 maxZdir = float3(-sphCen.z*sphCen.x, -sphCen.z*sphCen.y, sphCen.x*sphCen.x + sphCen.y*sphCen.y);     // cross(sphCen,cross(Zaxis,sphCen))
    float len = length(maxZdir);
    float scalarProj = len>0.0001 ? (maxZdir.z/len) : len;  // if len<=0.0001 then either |sphCen|<sphRadius or sphCen is very closely aligned with Z axis in which case little to no additional offs needed.
    float offs = scalarProj*sphRadiusIn;
#else
    float offs = sphRadiusIn;       // more false positives due to larger radius but works too
#endif

    // enlarge sphere so it overlaps the center of the tile assuming it overlaps the tile to begin with.
#if USE_LEFT_HAND_CAMERA_SPACE
    float s = sphCen.z+offs;
#else
    float s = -(sphCen.z-offs);
#endif
    float sphRadius = sphRadiusIn + (isOrthographic ? 1.0 : s)*halfTileSizeAtZDistOne;

    float a = dot(V,V);
    float CdotV = dot(sphCen,V);
    float c = dot(sphCen,sphCen) - sphRadius*sphRadius;

    float fDescDivFour = CdotV*CdotV - a*c;

    return c<0 || (fDescDivFour>0 && CdotV>0);      // if ray hits bounding sphere
}



#endif
