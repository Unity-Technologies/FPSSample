// this produces an orthonormal basis of the tangent and bitangent WITHOUT vertex level tangent/bitangent for any UV including procedurally generated
// method released with the demo for publication of "bump mapping unparametrized surfaces on the GPU"
// http://mmikkelsen3d.blogspot.com/2011/07/derivative-maps.html
void SurfaceGradientGenBasisTB(real3 nrmVertexNormal, real3 sigmaX, real3 sigmaY, real flipSign, real2 texST, out real3 vT, out real3 vB)
{
    real2 dSTdx = ddx_fine(texST), dSTdy = ddy_fine(texST);

    real det = dot(dSTdx, real2(dSTdy.y, -dSTdy.x));
    real sign_det = det < 0 ? -1 : 1;

    // invC0 represents (dXds, dYds); but we don't divide by determinant (scale by sign instead)
    real2 invC0 = sign_det * real2(dSTdy.y, -dSTdx.y);
    vT = sigmaX * invC0.x + sigmaY * invC0.y;
    if (abs(det) > 0.0)
        vT = normalize(vT);
    vB = (sign_det * flipSign) * cross(nrmVertexNormal, vT);
}

// surface gradient from an on the fly TBN (deriv obtained using tspaceNormalToDerivative()) or from conventional vertex level TBN (mikktspace compliant and deriv obtained using tspaceNormalToDerivative())
real3 SurfaceGradientFromTBN(real2 deriv, real3 vT, real3 vB)
{
    return deriv.x * vT + deriv.y * vB;
}

// surface gradient from an already generated "normal" such as from an object or world space normal map
// CAUTION: nrmVertexNormal and v must be in the same space. i.e world or object
// this allows us to mix the contribution together with a series of other contributions including tangent space normals
// v does not need to be unit length as long as it establishes the direction.
real3 SurfaceGradientFromPerturbedNormal(real3 nrmVertexNormal, real3 v)
{
    real3 n = nrmVertexNormal;
    real s = 1.0 / max(FLT_EPS, abs(dot(n, v)));
    return s * (dot(n, v) * n - v);
}

// used to produce a surface gradient from the gradient of a volume bump function such as a volume of perlin noise.
// equation 2. in "bump mapping unparametrized surfaces on the GPU".
// Observe the difference in figure 2. between using the gradient vs. the surface gradient to do bump mapping (the original method is proved wrong in the paper!).
real3 SurfaceGradientFromVolumeGradient(real3 nrmVertexNormal, real3 grad)
{
    return grad - dot(nrmVertexNormal, grad) * nrmVertexNormal;
}

// triplanar projection considered special case of volume bump map
// described here:  http://mmikkelsen3d.blogspot.com/2013/10/volume-height-maps-and-triplanar-bump.html
// derivs obtained using tspaceNormalToDerivative() and weights using computeTriplanarWeights().
real3 SurfaceGradientFromTriplanarProjection(real3 nrmVertexNormal, real3 triplanarWeights, real2 deriv_xplane, real2 deriv_yplane, real2 deriv_zplane)
{
    const real w0 = triplanarWeights.x, w1 = triplanarWeights.y, w2 = triplanarWeights.z;

    // assume deriv_xplane, deriv_yplane and deriv_zplane sampled using (z,y), (z,x) and (x,y) respectively.
    // positive scales of the look-up coordinate will work as well but for negative scales the derivative components will need to be negated accordingly.
    real3 volumeGrad = real3(w2 * deriv_zplane.x + w1 * deriv_yplane.y, w2 * deriv_zplane.y + w0 * deriv_xplane.y, w0 * deriv_xplane.x + w1 * deriv_yplane.x);

    return SurfaceGradientFromVolumeGradient(nrmVertexNormal, volumeGrad);
}

real3 SurfaceGradientResolveNormal(real3 nrmVertexNormal, real3 surfGrad)
{
    return normalize(nrmVertexNormal - surfGrad);
}

real2 ConvertTangentSpaceNormalToHeightMapGradient(real2 normalXY, real rcpNormalZ, real scale)
{
    // scale * (-normal.xy / normal.z)
    return normalXY * (-rcpNormalZ * scale);
}

// Converts tangent space normal to slopes (height map gradient).
real2 UnpackDerivativeNormalRGB(real4 packedNormal, real scale = 1.0)
{
    real3 vT   = packedNormal.rgb * 2.0 - 1.0; // Unsigned to signed
    real  rcpZ = rcp(max(vT.z, FLT_EPS));      // Clamp to avoid INF

    return ConvertTangentSpaceNormalToHeightMapGradient(vT.xy, rcpZ, scale);
}

// Converts tangent space normal to slopes (height map gradient).
real2 UnpackDerivativeNormalAG(real4 packedNormal, real scale = 1.0)
{
    real2 vT   = packedNormal.ag * 2.0 - 1.0;                      // Unsigned to signed
    real  rcpZ = rsqrt(max(1 - Sq(vT.x) - Sq(vT.y), Sq(FLT_EPS))); // Clamp to avoid INF

    return ConvertTangentSpaceNormalToHeightMapGradient(vT.xy, rcpZ, scale);
}

// Unpack normal as DXT5nm (1, y, 0, x) or BC5 (x, y, 0, 1)
real2 UnpackDerivativeNormalRGorAG(real4 packedNormal, real scale = 1.0)
{
    // Convert to (?, y, 0, x)
    packedNormal.a *= packedNormal.r;
    return UnpackDerivativeNormalAG(packedNormal, scale);
}
