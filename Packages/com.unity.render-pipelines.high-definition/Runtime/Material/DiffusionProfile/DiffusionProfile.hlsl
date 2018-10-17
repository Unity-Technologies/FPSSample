// ----------------------------------------------------------------------------
// SSS/Transmittance helper
// ----------------------------------------------------------------------------

// Computes the value of the integrand in polar coordinates: f(r, s) = r * R(r, s).
// f(r, s) = (Exp[-r * s] + Exp[-r * s / 3]) * (s / (8 * Pi))
// We can drop the constant (s / (8 * Pi)) due to the subsequent weight renormalization.
float3 DisneyProfilePolar(float r, float3 S)
{
#if 0
    float3 expOneThird = exp((-1.0 / 3.0) * r * S);
#else
    // Help the compiler. S is premultiplied by ((-1.0 / 3.0) * LOG2_E) on the CPU.
    float3 p = r * S;
    float3 expOneThird = exp2(p);
#endif
    return expOneThird + expOneThird * expOneThird * expOneThird;
}

// Computes the fraction of light passing through the object.
// Evaluate Int{0, inf}{2 * Pi * r * R(sqrt(r^2 + d^2))}, where R is the diffusion profile.
// Note: 'volumeAlbedo' should be premultiplied by 0.25.
// Ref: Approximate Reflectance Profiles for Efficient Subsurface Scattering by Pixar (BSSRDF only).
float3 ComputeTransmittanceDisney(float3 S, float3 volumeAlbedo, float thickness)
{
    // Thickness and SSS mask are decoupled for artists.
    // In theory, we should modify the thickness by the inverse of the mask scale of the profile.
    // thickness /= subsurfaceMask;

#if 0
    float3 expOneThird = exp(((-1.0 / 3.0) * thickness) * S);
#else
    // Help the compiler. S is premultiplied by ((-1.0 / 3.0) * LOG2_E) on the CPU.
    float3 p = thickness * S;
    float3 expOneThird = exp2(p);
#endif

    // Premultiply & optimize: T = (1/4 * A) * (e^(-t * S) + 3 * e^(-1/3 * t * S))
    return volumeAlbedo * (expOneThird * expOneThird * expOneThird + 3 * expOneThird);
}
