#ifndef UNITY_GEOMETRICTOOLS_INCLUDED
#define UNITY_GEOMETRICTOOLS_INCLUDED

// Solves the quadratic equation of the form: a*t^2 + b*t + c = 0.
// Returns 'false' if there are no real roots, 'true' otherwise.
// Numerically stable.
// Ref: Numerical Recipes in C++ (3rd Edition)
bool SolveQuadraticEquation(float a, float b, float c, out float2 roots)
{
    float d = b * b - 4 * a * c;
    float q = -0.5 * (b + CopySign(sqrt(d), b));
    roots   = float2(q / a, c / q);

    return (d >= 0);
}

//-----------------------------------------------------------------------------
// Intersection functions
//-----------------------------------------------------------------------------

bool IntersectRayAABB(float3 rayOrigin, float3 rayDirection,
                      float3 boxMin,    float3 boxMax,
                      float  tMin,       float tMax,
                  out float  tEntr,  out float tExit)
{
    // Could be precomputed. Clamp to avoid INF. clamp() is a single ALU on GCN.
    // rcp(FLT_EPS) = 16,777,216, which is large enough for our purposes,
    // yet doesn't cause a lot of numerical issues associated with FLT_MAX.
    float3 rayDirInv = clamp(rcp(rayDirection), -rcp(FLT_EPS), rcp(FLT_EPS));

    // Perform ray-slab intersection (component-wise).
    float3 t0 = boxMin * rayDirInv - (rayOrigin * rayDirInv);
    float3 t1 = boxMax * rayDirInv - (rayOrigin * rayDirInv);

    // Find the closest/farthest distance (component-wise).
    float3 tSlabEntr = min(t0, t1);
    float3 tSlabExit = max(t0, t1);

    // Find the farthest entry and the nearest exit.
    tEntr = Max3(tSlabEntr.x, tSlabEntr.y, tSlabEntr.z);
    tExit = Min3(tSlabExit.x, tSlabExit.y, tSlabExit.z);

    // Clamp to the range.
    tEntr = max(tEntr, tMin);
    tExit = min(tExit, tMax);

    return tEntr < tExit;
}

// This simplified version assume that we care about the result only when we are inside the box
float IntersectRayAABBSimple(float3 start, float3 dir, float3 boxMin, float3 boxMax)
{
    float3 invDir = rcp(dir);

    // Find the ray intersection with box plane
    float3 rbmin = (boxMin - start) * invDir;
    float3 rbmax = (boxMax - start) * invDir;

    float3 rbminmax = (dir > 0.0) ? rbmax : rbmin;

    return min(min(rbminmax.x, rbminmax.y), rbminmax.z);
}

// Assume Sphere is at the origin (i.e start = position - spherePosition)
bool IntersectRaySphere(float3 start, float3 dir, float radius, out float2 intersections)
{
    float a = dot(dir, dir);
    float b = dot(dir, start) * 2.0;
    float c = dot(start, start) - radius * radius;
    float discriminant = b * b - 4.0 * a * c;

    bool intersect = false;
    intersections = float2(0.0, 0.0);

    if (discriminant < 0.0 || a == 0.0)
    {
        intersections.x = 0.0;
        intersections.y = 0.0;
    }
    else
    {
        float sqrtDiscriminant = sqrt(discriminant);
        intersections.x = (-b - sqrtDiscriminant) / (2.0 * a);
        intersections.y = (-b + sqrtDiscriminant) / (2.0 * a);
        intersect = true;
    }

    return intersect;
}

// This simplified version assume that we care about the result only when we are inside the sphere
// Assume Sphere is at the origin (i.e start = position - spherePosition) and dir is normalized
// Ref: http://http.developer.nvidia.com/GPUGems/gpugems_ch19.html
float IntersectRaySphereSimple(float3 start, float3 dir, float radius)
{
    float b = dot(dir, start) * 2.0;
    float c = dot(start, start) - radius * radius;
    float discriminant = b * b - 4.0 * c;

    return abs(sqrt(discriminant) - b) * 0.5;
}

float3 IntersectRayPlane(float3 rayOrigin, float3 rayDirection, float3 planeOrigin, float3 planeNormal)
{
    float dist = dot(planeNormal, planeOrigin - rayOrigin) / dot(planeNormal, rayDirection);
    return rayOrigin + rayDirection * dist;
}

// Can support cones with an elliptic base: pre-scale 'coneAxisX' and 'coneAxisY' by (h/r_x) and (h/r_y).
// Returns parametric distances 'tEntr' and 'tExit' along the ray,
// subject to constraints 'tMin' and 'tMax'.
bool IntersectRayCone(float3 rayOrigin,  float3 rayDirection,
                      float3 coneOrigin, float3 coneDirection,
                      float3 coneAxisX,  float3 coneAxisY,
                      float tMin, float tMax,
                      out float tEntr, out float tExit)
{
    // Inverse transform the ray into a coordinate system with the cone at the origin facing along the Z axis.
    float3x3 rotMat = float3x3(coneAxisX, coneAxisY, coneDirection);

    float3 o = mul(rotMat, rayOrigin - coneOrigin);
    float3 d = mul(rotMat, rayDirection);

    // Cone equation (facing along Z): (h/r*x)^2 + (h/r*y)^2 - z^2 = 0.
    // Cone axes are premultiplied with (h/r).
    // Set up the quadratic equation: a*t^2 + b*t + c = 0.
    float a = d.x * d.x + d.y * d.y - d.z * d.z;
    float b = o.x * d.x + o.y * d.y - o.z * d.z;
    float c = o.x * o.x + o.y * o.y - o.z * o.z;

    float2 roots;

    // Check whether we have at least 1 root.
    bool hit = SolveQuadraticEquation(a, 2 * b, c, roots);

    tEntr = min(roots.x, roots.y);
    tExit = max(roots.x, roots.y);
    float3 pEntr = o + tEntr * d;
    float3 pExit = o + tExit * d;

    // Clip the negative cone.
    bool pEntrNeg = pEntr.z < 0;
    bool pExitNeg = pExit.z < 0;
    if (pEntrNeg && pExitNeg) { hit = false; }
    if (pEntrNeg) { tEntr = tExit; tExit = tMax; }
    if (pExitNeg) { tExit = tEntr; tEntr = tMin; }

    // Clamp using the values passed into the function.
    tEntr = clamp(tEntr, tMin, tMax);
    tExit = clamp(tExit, tMin, tMax);

    // Check for grazing intersections.
    if (tEntr == tExit) { hit = false; }

    return hit;
}

//-----------------------------------------------------------------------------
// Miscellaneous functions
//-----------------------------------------------------------------------------

// Box is AABB
float DistancePointBox(float3 position, float3 boxMin, float3 boxMax)
{
    return length(max(max(position - boxMax, boxMin - position), float3(0.0, 0.0, 0.0)));
}

float3 ProjectPointOnPlane(float3 position, float3 planePosition, float3 planeNormal)
{
    return position - (dot(position - planePosition, planeNormal) * planeNormal);
}

// Plane equation: {(a, b, c) = N, d = -dot(N, P)}.
// Returns the distance from the plane to the point 'p' along the normal.
// Positive -> in front (above), negative -> behind (below).
float DistanceFromPlane(float3 p, float4 plane)
{
    return dot(float4(p, 1.0), plane);
}

// Returns 'true' if the triangle is outside of the frustum.
// 'epsilon' is the (negative) distance to (outside of) the frustum below which we cull the triangle.
bool CullTriangleFrustum(float3 p0, float3 p1, float3 p2, float epsilon, float4 frustumPlanes[6], int numPlanes)
{
    bool outside = false;

    for (int i = 0; i < numPlanes; i++)
    {
        // If all 3 points are behind any of the planes, we cull.
        outside = outside || Max3(DistanceFromPlane(p0, frustumPlanes[i]),
                                  DistanceFromPlane(p1, frustumPlanes[i]),
                                  DistanceFromPlane(p2, frustumPlanes[i])) < epsilon;
    }

    return outside;
}

// Returns 'true' if the edge of the triangle is outside of the frustum.
// The edges are defined s.t. they are on the opposite side of the point with the given index.
// 'epsilon' is the (negative) distance to (outside of) the frustum below which we cull the triangle.
bool3 CullTriangleEdgesFrustum(float3 p0, float3 p1, float3 p2, float epsilon, float4 frustumPlanes[6], int numPlanes)
{
    bool3 edgesOutside = false;

    for (int i = 0; i < numPlanes; i++)
    {
        bool3 pointsOutside = bool3(DistanceFromPlane(p0, frustumPlanes[i]) < epsilon,
                                    DistanceFromPlane(p1, frustumPlanes[i]) < epsilon,
                                    DistanceFromPlane(p2, frustumPlanes[i]) < epsilon);

        // If both points of the edge are behind any of the planes, we cull.
        edgesOutside.x = edgesOutside.x || (pointsOutside.y && pointsOutside.z);
        edgesOutside.y = edgesOutside.y || (pointsOutside.x && pointsOutside.z);
        edgesOutside.z = edgesOutside.z || (pointsOutside.x && pointsOutside.y);
    }

    return edgesOutside;
}

bool CullTriangleBackFaceView(float3 p0, float3 p1, float3 p2, float epsilon, float3 V, float winding)
{
    float3 edge1 = p1 - p0;
    float3 edge2 = p2 - p0;

    float3 N = cross(edge1, edge2);
    float  NdotV = dot(N, V) * winding;

    // Optimize:
    // NdotV / (length(N) * length(V)) < Epsilon
    // NdotV < Epsilon * length(N) * length(V)
    // NdotV < Epsilon * sqrt(dot(N, N)) * sqrt(dot(V, V))
    // NdotV < Epsilon * sqrt(dot(N, N) * dot(V, V))
    return NdotV < epsilon * sqrt(dot(N, N) * dot(V, V));
}

// Returns 'true' if a triangle defined by 3 vertices is back-facing.
// 'epsilon' is the (negative) value of dot(N, V) below which we cull the triangle.
// 'winding' can be used to change the order: pass 1 for (p0 -> p1 -> p2), or -1 for (p0 -> p2 -> p1).
bool CullTriangleBackFace(float3 p0, float3 p1, float3 p2, float epsilon, float3 viewPos, float winding)
{
    float3 V = viewPos - p0;
    return CullTriangleBackFaceView(p0, p1, p2, epsilon, V, winding);
}

#endif // UNITY_GEOMETRICTOOLS_INCLUDED
