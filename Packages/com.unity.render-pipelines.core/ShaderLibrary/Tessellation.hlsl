#define TESSELLATION_INTERPOLATE_BARY(name, bary) output.name = input0.name * bary.x +  input1.name * bary.y +  input2.name * bary.z

// p0, p1, p2 triangle world position
// p0, p1, p2 triangle world vertex normal
real3 PhongTessellation(real3 positionWS, real3 p0, real3 p1, real3 p2, real3 n0, real3 n1, real3 n2, real3 baryCoords, real shape)
{
    real3 c0 = ProjectPointOnPlane(positionWS, p0, n0);
    real3 c1 = ProjectPointOnPlane(positionWS, p1, n1);
    real3 c2 = ProjectPointOnPlane(positionWS, p2, n2);

    real3 phongPositionWS = baryCoords.x * c0 + baryCoords.y * c1 + baryCoords.z * c2;

    return lerp(positionWS, phongPositionWS, shape);
}

// Reference: http://twvideo01.ubm-us.net/o1/vault/gdc10/slides/Bilodeau_Bill_Direct3D11TutorialTessellation.pdf

// Compute both screen and distance based adaptation - return factor between 0 and 1
real3 GetScreenSpaceTessFactor(real3 p0, real3 p1, real3 p2, real4x4 viewProjectionMatrix, real4 screenSize, real triangleSize)
{
    // Get screen space adaptive scale factor
    real2 edgeScreenPosition0 = ComputeNormalizedDeviceCoordinates(p0, viewProjectionMatrix) * screenSize.xy;
    real2 edgeScreenPosition1 = ComputeNormalizedDeviceCoordinates(p1, viewProjectionMatrix) * screenSize.xy;
    real2 edgeScreenPosition2 = ComputeNormalizedDeviceCoordinates(p2, viewProjectionMatrix) * screenSize.xy;

    real EdgeScale = 1.0 / triangleSize; // Edge size in reality, but name is simpler
    real3 tessFactor;
    tessFactor.x = saturate(distance(edgeScreenPosition1, edgeScreenPosition2) * EdgeScale);
    tessFactor.y = saturate(distance(edgeScreenPosition0, edgeScreenPosition2) * EdgeScale);
    tessFactor.z = saturate(distance(edgeScreenPosition0, edgeScreenPosition1) * EdgeScale);

    return tessFactor;
}

real3 GetDistanceBasedTessFactor(real3 p0, real3 p1, real3 p2, real3 cameraPosWS, real tessMinDist, real tessMaxDist)
{
    real3 edgePosition0 = 0.5 * (p1 + p2);
    real3 edgePosition1 = 0.5 * (p0 + p2);
    real3 edgePosition2 = 0.5 * (p0 + p1);

    // In case camera-relative rendering is enabled, 'cameraPosWS' is statically known to be 0,
    // so the compiler will be able to optimize distance() to length().
    real dist0 = distance(edgePosition0, cameraPosWS);
    real dist1 = distance(edgePosition1, cameraPosWS);
    real dist2 = distance(edgePosition2, cameraPosWS);

    // The saturate will handle the produced NaN in case min == max
    real fadeDist = tessMaxDist - tessMinDist;
    real3 tessFactor;
    tessFactor.x = saturate(1.0 - (dist0 - tessMinDist) / fadeDist);
    tessFactor.y = saturate(1.0 - (dist1 - tessMinDist) / fadeDist);
    tessFactor.z = saturate(1.0 - (dist2 - tessMinDist) / fadeDist);

    return tessFactor;
}

real4 CalcTriTessFactorsFromEdgeTessFactors(real3 triVertexFactors)
{
    real4 tess;
    tess.x = triVertexFactors.x;
    tess.y = triVertexFactors.y;
    tess.z = triVertexFactors.z;
    tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0;

    return tess;
}
