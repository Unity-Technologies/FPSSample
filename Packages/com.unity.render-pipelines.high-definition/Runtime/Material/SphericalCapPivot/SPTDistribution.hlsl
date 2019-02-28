// SPTD: Spherical Pivot Transformed Distributions
// Keep in synch with the c# side (eg in Bind() and for dims)
TEXTURE2D(_PivotData);

#define PIVOT_LUT_SIZE   64
#define PIVOT_LUT_SCALE  ((PIVOT_LUT_SIZE - 1) * rcp(PIVOT_LUT_SIZE))
#define PIVOT_LUT_OFFSET (0.5 * rcp(PIVOT_LUT_SIZE))

//-----------------------------------------------------------------------------
// SPTD structures
//-----------------------------------------------------------------------------

struct SphereCap
{
    float3 dir; // direction of cone
    float cosA; // cos(aperture angle) of cone (full opening is 2*aperture)
};

SphereCap GetSphereCap(float3 dir, float cosA)
{
    SphereCap sCap;
    sCap.dir = dir;
    sCap.cosA = cosA;
    return sCap;
}

//-----------------------------------------------------------------------------
// SPTD functions
//-----------------------------------------------------------------------------
float SphereCapSolidAngle(SphereCap c)
{
    return (TWO_PI * (1.0 - c.cosA));
}

// Extract pivot parameters fitting a GGX_projected (BSDF with the cos projection factor
// folded in so we don't have to carry projected solid angle measure when integrating)
// via an SPTD, the non "pivoted" distribution being the uniform spherical distribution
// over the whole sphere (ie Dstd(w) = 1/4*PI )
//
// FGD is required to normalize the fit, otherwise integrating the SPTD over a spherical
// cap implies calculating:
//
// SolidAngle( PivotTransform(sCap) ) / 4*PI
//
// Integral of fitted GGX_projected is thus: 
//
// [ SolidAngle( PivotTransform(sCap) ) / 4*PI ] * FGD
//
// orthoBasisViewNormal is assumed as follow:
//
// Basis vectors b1, b2 and b3 arranged as rows, b3 = shading normal,
// view vector lies in the b0-b2 plane.
// 
float3 ExtractPivot(float clampedNdotV, float perceptualRoughness, float3x3 orthoBasisViewNormal)
{
    float theta = FastACosPos(clampedNdotV);
    float2 uv = PIVOT_LUT_OFFSET + PIVOT_LUT_SCALE * float2(perceptualRoughness, theta * INV_HALF_PI);
    
    float2 pivotParams = SAMPLE_TEXTURE2D_LOD(_PivotData, s_linear_clamp_sampler, uv, 0).rg;
    float pivotNorm = pivotParams.r;
    float pivotElev = pivotParams.g;
    float3 pivot = pivotNorm * float3(sin(pivotElev), 0, cos(pivotElev));

    // express the pivot in world space 
    // (basis is left-mul WtoFrame rotation, so a right-mul FrameToW rotation)
    pivot = mul(pivot, orthoBasisViewNormal);

    return pivot;
}

// Pivot 2D Transformation (helper for the full pivot transform, CapToPCap)
float2 R2ToPR2(float2 pivotDir, float pivotMag)
{
    float2 tmp1 = float2(pivotDir.x - pivotMag, pivotDir.y);
    float2 tmp2 = pivotMag * pivotDir - float2(1, 0);
    float x = dot(tmp1, tmp2);
    float y = tmp1.y * tmp2.x - tmp1.x * tmp2.y;
    float qf = dot(tmp2, tmp2);

    return (float2(x, y) / qf);
}

// Pivot transform a spherical cap: pivot and cap should
// be expressed in the same basis.
SphereCap CapToPCap(SphereCap cap, float3 pivot)
{
    // Avoid instability between returning huge apertures to 
    // none when near these extremes (eg near 1.0, ie degenerate
    // cap, depending on the pivot, we can get a cap of 
    // cos aperture near -1.0 or 1.0 ). See area calculation 
    // below: we can clamp here, or test area later.
    cap.cosA = clamp(cap.cosA, -0.9999, 0.9999);

    // extract pivot length and direction
    float pivotMag = length(pivot);
    // special case: the pivot is at the origin, trivial:
    if (pivotMag < 0.001)
    {
        return GetSphereCap(-cap.dir, cap.cosA);
    }
    float3 pivotDir = pivot / pivotMag;

    // 2D cap dir in the capDir/pivotDir/pivotCapDir 2D plane,
    // using the pivotDir as the first axis.
    float cosPhi = dot(cap.dir, pivotDir);
    float sinPhi = sqrt(1.0 - cosPhi * cosPhi);

    // Make a 2D basis for that 2D plane:
    // 2D basis = (pivotDir, PivotOrthogonalDirection)
    float3 pivotOrthoDir;
    if (abs(cosPhi) < 0.9999)
    {
        pivotOrthoDir = (cap.dir - cosPhi * pivotDir) / sinPhi;
    }
    else
    {
        pivotOrthoDir = float3(0, 0, 0);
    }

    // Compute the original cap 2D end points that intersect and
    // lie in the previously mentionned 2D plane.
    // We rotate the capDir vector (cosPhi, sinPhi) (coordinates
    // expressed in the 2D pivot plane frame above) with +aperture
    // and -aperture angles to find dir1 and dir2, the 2 endpoint
    // vectors:
    float capSinA = sqrt(1.0 - cap.cosA * cap.cosA);
    float a1 = cosPhi * cap.cosA;
    float a2 = sinPhi * capSinA;
    float a3 = sinPhi * cap.cosA;
    float a4 = cosPhi * capSinA;
    float2 dir1 = float2(a1 + a2, a3 - a4); // Rot(-aperture) (clockwise)
    float2 dir2 = float2(a1 - a2, a3 + a4); // Rot(+aperture) (counter clockwise)

    // Pivot transform the original cap endpoints in the 2D plane 
    // to get the pivotCap endpoints:
    float2 dir1Xf = R2ToPR2(dir1, pivotMag);
    float2 dir2Xf = R2ToPR2(dir2, pivotMag);

    // Compute the pivotCap 2D direction (note that the pivotCap 
    // direction is NOT the pivot transform of the original direction):
    // It is the mean direction direction of the two pivotCap endpoints 
    // ie their half-vector, up to a sign. 
    // This sign is important, as a smaller than 90 degree aperture cap
    // can, with the proper pivot, yield a cap with a much larger 
    // aperture (ie covering more than an hemisphere).
    //
    float area = dir1Xf.x * dir2Xf.y - dir1Xf.y * dir2Xf.x;
    //if (abs(area) < 0.0001) area = 0.0; // see clamp above
    float s = area >= 0.0 ? 1.0 : -1.0;
    float2 dirXf = s * normalize(dir1Xf + dir2Xf);

    // Compute the 3D pivotCap parameters: 
    // Transform back the pivotCap endpoints into 3D and compute 
    // cosine of aperture.
    float3 pivotCapDir = dirXf.x * pivotDir + dirXf.y * pivotOrthoDir;
    float pivotCapCosA = dot(dirXf, dir1Xf);

    return GetSphereCap(pivotCapDir, pivotCapCosA);
}

// Compute specular occlusion from visibility cone:
//
//          Integral[ V(w_i) bsdf(w_i, w_o) (n dot w_i) dw_i ]_{over hemisphere}
//     Vs = --------------------------------------------------------------------
//          Integral[ bsdf(w_i, w_o) (n dot w_i) dw_i ]_{over hemisphere}
//
// where V(w_i) is the occlusion indicator function. The denominator is thus FGD.
// With the visibility cone approximation (aka bent occlusion from bentnormal),
// V becomes the cone ray-set indicator function. We have:
//
//     Vs = Integral[ bsdf(w_i, w_o) (n dot w_i) dw_i ]_{over visibility cone} / FGD
// 
// We approximate the GGX bsdf() with an SPTD transformed from a uniform distribution
// on the whole unit sphere S^2, and the integral thus becomes (see ExtractPivot)
//
//     Vs = Integral[ SPTD(w_i) dw_i ]_{over visibility cone} * normalization / FGD
//        = Integral[ Dstd(w_ii) dw_ii ]_{over g(visibility cone)} * normalization / FGD
//
// where normalization is as explained for ExtractPivot since the fit is up to that
// normalization, and here it is FGD;  
// g() is the pivot transform (ie CapToPCap), and w_ii := g(w_i) and here we use the
// uniform Dstd(w) = 1/4pi.
//
// Thus Vs becomes
//
//     Vs = [SolidAngle( CapToPCap(visibility cone) ) / 4*PI] * normalization /FGD
//        = [SolidAngle( CapToPCap(visibility cone) ) / 4*PI] * FGD /FGD
//        = [SolidAngle( CapToPCap(visibility cone) ) / 4*PI]
//
// Finally, here we also allow intersecting the visibility cone by another one
// (one of the SPTD property is that the pivot transform is homomorphic to such
// domain composition operation).
//
// For example, IBLs would typically use a normal oriented hemisphere to prevent
// light leaking if we don't trust the construction of our visibility cone to
// not cross under that visible hemisphere horizon: the leak happens in that case
// as SPTD has support spanning the whole sphere while normally the BSDF has a support
// limited to a hemisphere and even though the fit minimizes weight away from the
// specular lobe, it can't aligned support.
//
float ComputeVs(SphereCap visibleCap, 
                float clampedNdotV, 
                float perceptualRoughness, 
                float3x3 orthoBasisViewNormal,
                float useExtraCap = false,
                SphereCap extraCap = (SphereCap)0.0)
{
    float res;

    float3 pivot = ExtractPivot(clampedNdotV, perceptualRoughness, orthoBasisViewNormal);
    SphereCap c1 = CapToPCap(visibleCap, pivot);

    if (useExtraCap)
    {
        // eg for IBL: extraCap = GetSphereCap(Normal, 0.0)
        SphereCap c2 = CapToPCap(extraCap, pivot);
        res = SphericalCapIntersectionSolidArea(c1.cosA, c2.cosA, dot(c1.dir, c2.dir));
    }
    else 
    {
        res = SphereCapSolidAngle(c1);
    }

    res = res * INV_FOUR_PI;
    return saturate(res);
}

//-----------------------------------------------------------------------------
// Specular Occlusion using SPTD functions
//-----------------------------------------------------------------------------

// Choice of formulas to infer bent visibility:
#define BENT_VISIBILITY_FROM_AO_UNIFORM 0
#define BENT_VISIBILITY_FROM_AO_COS 1
#define BENT_VISIBILITY_FROM_AO_COS_BENT_CORRECTION 2

SphereCap GetBentVisibility(float3 bentNormalWS, float ambientOcclusion, int algorithm = BENT_VISIBILITY_FROM_AO_COS, float3 normalWS = float3(0,0,0))
{
    float cosAv;

    switch (algorithm)
    {
    case BENT_VISIBILITY_FROM_AO_UNIFORM:
        // AO is uniform (ie expresses non projected solid angle measure):
        cosAv = (1.0 - ambientOcclusion);
        break;

    case BENT_VISIBILITY_FROM_AO_COS:
        // AO is cosine weighted (expresses projected solid angle):
        cosAv = sqrt(1.0 - ambientOcclusion);
        break;

    case BENT_VISIBILITY_FROM_AO_COS_BENT_CORRECTION:
        // AO is cosine weighted, but this extraction of the cosine of the aperture
        // takes into account the fact that if the cone is not perflectly aligned
        // with the normal (or the axis by which we define elevation angle), then
        // the AO integral calculated yielded a projected solid angle measure of
        // an *inclined* spherical cap, and the simple formula above is wrong.
        // The projected solid angle measure of a spherical cap is given in (eg)
        //
        // Geometric Derivation of the Irradiance of Polygonal Lights - Heitz 2017
        // https://hal.archives-ouvertes.fr/hal-01458129/document
        // p5
        // 
        // The formula below is derived from AO (aka Vd) being considered that 
        // projected solid angle (given the bent visibility assumption).
        //
        // (Note that Monte Carlo with IS would typically be used to sample the
        // visibility to compute the AO, and the IS rebalancing PDF ratio (weights)
        // could then have been applied to the directions or not when calculating 
        // the bent cone direction. We don't do anything about that, but cone of 
        // visibility is a gross approximation anyway and can be pretty bad if its
        // shape on the hemisphere of directions is very segmented.)
        cosAv = sqrt(1.0 - saturate(ambientOcclusion/dot(bentNormalWS, normalWS)) );
        break;
    }

    return GetSphereCap(bentNormalWS, cosAv);
}

float GetSpecularOcclusionFromBentAOPivot(float3 V, float3 bentNormalWS, float3 normalWS, float ambientOcclusion, float perceptualRoughness, int bentconeAlgorithm = BENT_VISIBILITY_FROM_AO_COS,
                                          bool useGivenBasis = false, float3x3 orthoBasisViewNormal = (float3x3)(0), bool useExtraCap = false, SphereCap extraCap = (SphereCap)(0))
{
    SphereCap bentVisibility = GetBentVisibility(bentNormalWS, ambientOcclusion, bentconeAlgorithm, normalWS);

    //bentNormalWS = lerp(bentNormalWS, normalWS, pow((1.0-ambientOcclusion),5)); // TEST TODO, the bent direction becomes meaningless with AO = 0.
    //bentVisibility.dir = normalize(bentNormalWS);

    //perceptualRoughness = max(perceptualRoughness, 0.01);

    if (useGivenBasis == false) 
    {
        //orthoBasisViewNormal = GetOrthoBasisViewNormal(V, normalWS, dot(normalWS, V), true); // true => avoid singularity when V == N by returning arbitrary tangent/bitangents
        orthoBasisViewNormal = GetOrthoBasisViewNormal(V, normalWS, dot(normalWS, V));
    }

    float Vs = ComputeVs(bentVisibility,
                         ClampNdotV(dot(normalWS, V)),
                         perceptualRoughness,
                         orthoBasisViewNormal,
                         useExtraCap, //false, // true => clip with a second spherical cap, eg here the visible hemisphere:
                         extraCap);   //GetSphereCap(normalWS, 0.0));
    return Vs;
}

// Different tweaks to the cone-cone method:
float GetSpecularOcclusionFromBentAOConeCone(float3 V, float3 bentNormalWS, float3 normalWS, float ambientOcclusion, float perceptualRoughness, int bentconeAlgorithm = BENT_VISIBILITY_FROM_AO_COS)
{
    // Retrieve cone angle
    // Ambient occlusion is cosine weighted, thus use following equation. See slide 129
    //SphereCap bentVisibility = GetBentVisibility(bentNormalWS, ambientOcclusion, BENT_VISIBILITY_FROM_AO_COS);
    SphereCap bentVisibility = GetBentVisibility(bentNormalWS, ambientOcclusion, bentconeAlgorithm, normalWS);

    float cosAv = bentVisibility.cosA;
    float roughness = max(PerceptualRoughnessToRoughness(perceptualRoughness), 0.01); // Clamp to 0.01 to avoid edge cases
    float cosAs = exp2((-log(10.0)/log(2.0)) * Sq(roughness));
    float ReflectionLobeSolidAngle = (TWO_PI * (1.0 - cosAs));

    float3 R = reflect(-V, normalWS);

    float3 modifiedR = GetSpecularDominantDir(normalWS, R, perceptualRoughness, ClampNdotV(dot(normalWS, V)) );
    modifiedR = normalize(modifiedR);

    float cosB;
#if 1
    cosB = dot(bentNormalWS, R);
#else
    // Test: offspecular modification
    cosB = dot(bentNormalWS, modifiedR);
#endif

    float HemiClippedReflectionLobeSolidAngle = SphericalCapIntersectionSolidArea(0.0, cosAs, cosB);
#if 1
    // Original, less expensive, but allow the cone approximation to go under horizon of full hemisphere
    // and unecessarily dampens SO (ie more occlusion). 
    return SphericalCapIntersectionSolidArea(cosAv, cosAs, cosB) / ReflectionLobeSolidAngle;
#else
    // More correct, but more expensive:
    return saturate(SphericalCapIntersectionSolidArea(cosAv, cosAs, cosB) / HemiClippedReflectionLobeSolidAngle);
#endif
}

