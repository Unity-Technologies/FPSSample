#ifndef UNITY_VOLUME_RENDERING_INCLUDED
#define UNITY_VOLUME_RENDERING_INCLUDED

// Reminder:
// Optical_Depth(x, y) = Integral{x, y}{Extinction(t) dt}
// Transmittance(x, y) = Exp(-Optical_Depth(x, y))
// Transmittance(x, z) = Transmittance(x, y) * Transmittance(y, z)
// Integral{a, b}{Transmittance(0, t) * L_s(t) dt} = Transmittance(0, a) * Integral{a, b}{Transmittance(0, t - a) * L_s(t) dt}.

real OpticalDepthHomogeneousMedium(real extinction, real intervalLength)
{
    return extinction * intervalLength;
}

real3 OpticalDepthHomogeneousMedium(real3 extinction, real intervalLength)
{
    return extinction * intervalLength;
}

real Transmittance(real opticalDepth)
{
    return exp(-opticalDepth);
}

real3 Transmittance(real3 opticalDepth)
{
    return exp(-opticalDepth);
}

real TransmittanceHomogeneousMedium(real extinction, real intervalLength)
{
    return Transmittance(OpticalDepthHomogeneousMedium(extinction, intervalLength));
}

real3 TransmittanceHomogeneousMedium(real3 extinction, real intervalLength)
{
    return Transmittance(OpticalDepthHomogeneousMedium(extinction, intervalLength));
}

// Integral{a, b}{Transmittance(0, t - a) dt}.
real TransmittanceIntegralHomogeneousMedium(real extinction, real intervalLength)
{
    return rcp(extinction) - rcp(extinction) * exp(-extinction * intervalLength);
}

// Integral{a, b}{Transmittance(0, t - a) dt}.
real3 TransmittanceIntegralHomogeneousMedium(real3 extinction, real intervalLength)
{
    return rcp(extinction) - rcp(extinction) * exp(-extinction * intervalLength);
}

real IsotropicPhaseFunction()
{
    return INV_FOUR_PI;
}

real HenyeyGreensteinPhasePartConstant(real anisotropy)
{
    real g = anisotropy;

    return INV_FOUR_PI * (1 - g * g);
}

real HenyeyGreensteinPhasePartVarying(real anisotropy, real cosTheta)
{
    real g = anisotropy;
    real f = rsqrt(saturate(1 + g * g - 2 * g * cosTheta)); // x^(-1/2)

    return f * f * f; // x^(-3/2)
}

real HenyeyGreensteinPhaseFunction(real anisotropy, real cosTheta)
{
    return HenyeyGreensteinPhasePartConstant(anisotropy) *
           HenyeyGreensteinPhasePartVarying(anisotropy, cosTheta);
}

real CornetteShanksPhasePartConstant(real anisotropy)
{
    real g = anisotropy;

    return INV_FOUR_PI * 1.5 * (1 - g * g) / (2 + g * g);
}

real CornetteShanksPhasePartVarying(real anisotropy, real cosTheta)
{
    real g = anisotropy;
    real f = rsqrt(saturate(1 + g * g - 2 * g * cosTheta)); // x^(-1/2)
    real h = (1 + cosTheta * cosTheta);

    // Note that this function is not perfectly isotropic for (g = 0). We force it to be.
    // TODO: in the future, when (g = 0), specialize the Volumetric Lighting kernel
    // to not do anything anisotropy-specific. This way we could avoid this test
    // (along with tons of other overhead and hacks).
    return (g == 0) ? 1.33333333 : h * (f * f * f); // h * x^(-3/2)
}

// A better approximation of the Mie phase function.
// Ref: Henyey–Greenstein and Mie phase functions in Monte Carlo radiative transfer computations
real CornetteShanksPhaseFunction(real anisotropy, real cosTheta)
{
    return CornetteShanksPhasePartConstant(anisotropy) *
           CornetteShanksPhasePartVarying(anisotropy, cosTheta);
}

// Samples the interval of homogeneous participating medium using the closed-form tracking approach
// (proportionally to the transmittance).
// Returns the offset from the start of the interval and the weight = (transmittance / pdf).
// Ref: Monte Carlo Methods for Volumetric Light Transport Simulation, p. 5.
void ImportanceSampleHomogeneousMedium(real rndVal, real extinction, real intervalLength,
                                       out real offset, out real weight)
{
    // pdf    = extinction * exp(extinction * (intervalLength - t)) / (exp(intervalLength * extinction - 1)
    // weight = exp(-extinction * t) / pdf
    // weight = (1 - exp(-extinction * intervalLength)) / extinction

    real x = 1 - exp(-extinction * intervalLength);
    real c = rcp(extinction);

    weight = x * c;
    offset = -log(1 - rndVal * x) * c;
}

// Implements equiangular light sampling.
// Returns the distance from the origin of the ray, the squared distance from the light,
// and the reciprocal of the PDF.
// Ref: Importance Sampling of Area Lights in Participating Medium.
void ImportanceSamplePunctualLight(real rndVal, real3 lightPosition, real lightSqRadius,
                                   real3 rayOrigin, real3 rayDirection,
                                   real tMin, real tMax,
                                   out real t, out real sqDist, out real rcpPdf)
{
    real3 originToLight         = lightPosition - rayOrigin;
    real  originToLightProjDist = dot(originToLight, rayDirection);
    real  originToLightSqDist   = dot(originToLight, originToLight);
    real  rayToLightSqDist      = originToLightSqDist - originToLightProjDist * originToLightProjDist;

    // Virtually offset the light to modify the PDF distribution.
    real sqD  = max(rayToLightSqDist + lightSqRadius, FLT_EPS);
    real rcpD = rsqrt(sqD);
    real d    = sqD * rcpD;
    real a    = tMin - originToLightProjDist;
    real b    = tMax - originToLightProjDist;
    real x    = a * rcpD;
    real y    = b * rcpD;

#if 0
    real theta0   = FastATan(x);
    real theta1   = FastATan(y);
    real gamma    = theta1 - theta0;
    real tanTheta = tan(theta0 + rndVal * gamma);
#else
    // Same but faster:
    // atan(y) - atan(x) = atan((y - x) / (1 + x * y))
    // tan(atan(x) + z)  = (x * cos(z) + sin(z)) / (cos(z) - x * sin(z))
    real tanGamma = abs((y - x) * rcp(1 + x * y));
    real gamma    = FastATanPos(tanGamma);
    real z        = rndVal * gamma;
    real numer    = x * cos(z) + sin(z);
    real denom    = cos(z) - x * sin(z);
    real tanTheta = numer * rcp(denom);
#endif

    real tRelative = d * tanTheta;

    sqDist = sqD + tRelative * tRelative;
    rcpPdf = gamma * rcpD * sqDist;
    t      = originToLightProjDist + tRelative;

    // Remove the virtual light offset to obtain the real geometric distance.
    sqDist = max(sqDist - lightSqRadius, FLT_EPS);
}

// Absorption coefficient from Disney: http://blog.selfshadow.com/publications/s2015-shading-course/burley/s2015_pbs_disney_bsdf_notes.pdf
real3 TransmittanceColorAtDistanceToAbsorption(real3 transmittanceColor, real atDistance)
{
    return -log(transmittanceColor + FLT_EPS) / max(atDistance, FLT_EPS);
}

#endif // UNITY_VOLUME_RENDERING_INCLUDED
