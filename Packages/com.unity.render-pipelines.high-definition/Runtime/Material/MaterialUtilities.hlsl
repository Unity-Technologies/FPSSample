// Flipping or mirroring a normal can be done directly on the tangent space. This has the benefit to apply to the whole process either in surface gradient or not.
// This function will modify FragInputs and this is not propagate outside of GetSurfaceAndBuiltinData(). This is ok as tangent space is not use outside of GetSurfaceAndBuiltinData().
void ApplyDoubleSidedFlipOrMirror(inout FragInputs input)
{
#ifdef _DOUBLESIDED_ON
    // _DoubleSidedConstants is float3(-1, -1, -1) in flip mode and float3(1, 1, -1) in mirror mode
    // To get a flipped normal with the tangent space, we must flip bitangent (because it is construct from the normal) and normal
    // To get a mirror normal with the tangent space, we only need to flip the normal and not the tangent
    float2 flipSign = input.isFrontFace ? float2(1.0, 1.0) : _DoubleSidedConstants.yz; // TOCHECK :  GetOddNegativeScale() is not necessary here as it is apply for tangent space creation.
    input.worldToTangent[1] = flipSign.x * input.worldToTangent[1]; // bitangent
    input.worldToTangent[2] = flipSign.y * input.worldToTangent[2]; // normal

    #ifdef SURFACE_GRADIENT
    // TOCHECK: seems that we don't need to invert any genBasisTB(), sign cancel. Which is expected as we deal with surface gradient.

    // TODO: For surface gradient we must invert or mirror the normal just after the interpolation. It will allow to work with layered with all basis. Currently it is not the case
    #endif
#endif
}

// This function convert the tangent space normal/tangent to world space and orthonormalize it + apply a correction of the normal if it is not pointing towards the near plane
void GetNormalWS(FragInputs input, float3 normalTS, out float3 normalWS)
{
#ifdef SURFACE_GRADIENT
    normalWS = SurfaceGradientResolveNormal(input.worldToTangent[2], normalTS);
#else
    // We need to normalize as we use mikkt tangent space and this is expected (tangent space is not normalize)
    normalWS = normalize(TransformTangentToWorld(normalTS, input.worldToTangent));
#endif
}
