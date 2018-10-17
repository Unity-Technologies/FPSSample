#ifndef UNITY_SAMPLING_INCLUDED
#define UNITY_SAMPLING_INCLUDED

//-----------------------------------------------------------------------------
// Sample generator
//-----------------------------------------------------------------------------

#include "Fibonacci.hlsl"
#include "Hammersley.hlsl"

//-----------------------------------------------------------------------------
// Coordinate system conversion
//-----------------------------------------------------------------------------

// Transforms the unit vector from the spherical to the Cartesian (right-handed, Z up) coordinate.
real3 SphericalToCartesian(real phi, real cosTheta)
{
    real sinPhi, cosPhi;
    sincos(phi, sinPhi, cosPhi);

    real sinTheta = sqrt(saturate(1.0 - cosTheta * cosTheta));

    return real3(sinTheta * cosPhi, sinTheta * sinPhi, cosTheta);
}

// Converts Cartesian coordinates given in the right-handed coordinate system
// with Z pointing upwards (OpenGL style) to the coordinates in the left-handed
// coordinate system with Y pointing up and Z facing forward (DirectX style).
real3 TransformGLtoDX(real3 v)
{
    return v.xzy;
}

// Performs conversion from equiareal map coordinates to Cartesian (DirectX cubemap) ones.
real3 ConvertEquiarealToCubemap(real u, real v)
{
    real phi      = TWO_PI - TWO_PI * u;
    real cosTheta = 1.0 - 2.0 * v;

    return TransformGLtoDX(SphericalToCartesian(phi, cosTheta));
}

// Convert a texel position into normalized position [-1..1]x[-1..1]
real2 CubemapTexelToNVC(uint2 unPositionTXS, uint cubemapSize)
{
    return 2.0 * real2(unPositionTXS) / real(max(cubemapSize - 1, 1)) - 1.0;
}

// Map cubemap face to world vector basis
static const real3 CUBEMAP_FACE_BASIS_MAPPING[6][3] =
{
    //XPOS face
    {
        real3(0.0, 0.0, -1.0),
        real3(0.0, -1.0, 0.0),
        real3(1.0, 0.0, 0.0)
    },
    //XNEG face
    {
        real3(0.0, 0.0, 1.0),
        real3(0.0, -1.0, 0.0),
        real3(-1.0, 0.0, 0.0)
    },
    //YPOS face
    {
        real3(1.0, 0.0, 0.0),
        real3(0.0, 0.0, 1.0),
        real3(0.0, 1.0, 0.0)
    },
    //YNEG face
    {
        real3(1.0, 0.0, 0.0),
        real3(0.0, 0.0, -1.0),
        real3(0.0, -1.0, 0.0)
    },
    //ZPOS face
    {
        real3(1.0, 0.0, 0.0),
        real3(0.0, -1.0, 0.0),
        real3(0.0, 0.0, 1.0)
    },
    //ZNEG face
    {
        real3(-1.0, 0.0, 0.0),
        real3(0.0, -1.0, 0.0),
        real3(0.0, 0.0, -1.0)
    }
};

// Convert a normalized cubemap face position into a direction
real3 CubemapTexelToDirection(real2 positionNVC, uint faceId)
{
    real3 dir = CUBEMAP_FACE_BASIS_MAPPING[faceId][0] * positionNVC.x
               + CUBEMAP_FACE_BASIS_MAPPING[faceId][1] * positionNVC.y
               + CUBEMAP_FACE_BASIS_MAPPING[faceId][2];

    return normalize(dir);
}

//-----------------------------------------------------------------------------
// Sampling function
// Reference : http://www.cs.virginia.edu/~jdl/bib/globillum/mis/shirley96.pdf + PBRT
//-----------------------------------------------------------------------------

// Performs uniform sampling of the unit disk.
// Ref: PBRT v3, p. 777.
real2 SampleDiskUniform(real u1, real u2)
{
    real r   = sqrt(u1);
    real phi = TWO_PI * u2;

    real sinPhi, cosPhi;
    sincos(phi, sinPhi, cosPhi);

    return r * real2(cosPhi, sinPhi);
}

real3 SampleSphereUniform(real u1, real u2)
{
    real phi      = TWO_PI * u2;
    real cosTheta = 1.0 - 2.0 * u1;

    return SphericalToCartesian(phi, cosTheta);
}

// Performs cosine-weighted sampling of the hemisphere.
// Ref: PBRT v3, p. 780.
real3 SampleHemisphereCosine(real u1, real u2)
{
    real3 localL;

    // Since we don't really care about the area distortion,
    // we substitute uniform disk sampling for the concentric one.
    localL.xy = SampleDiskUniform(u1, u2);

    // Project the point from the disk onto the hemisphere.
    localL.z = sqrt(1.0 - u1);

    return localL;
}

// Cosine-weighted sampling without the tangent frame.
// Ref: http://www.amietia.com/lambertnotangent.html
real3 SampleHemisphereCosine(real u1, real u2, real3 normal)
{
    real3 pointOnSphere = SampleSphereUniform(u1, u2);
    return normalize(normal + pointOnSphere);
}

real3 SampleHemisphereUniform(real u1, real u2)
{
    real phi      = TWO_PI * u2;
    real cosTheta = 1.0 - u1;

    return SphericalToCartesian(phi, cosTheta);
}

void SampleSphere(real2   u,
                  real4x4 localToWorld,
                  real    radius,
              out real    lightPdf,
              out real3   P,
              out real3   Ns)
{
    real u1 = u.x;
    real u2 = u.y;

    Ns = SampleSphereUniform(u1, u2);

    // Transform from unit sphere to world space
    P = radius * Ns + localToWorld[3].xyz;

    // pdf is inverse of area
    lightPdf = 1.0 / (FOUR_PI * radius * radius);
}

void SampleHemisphere(real2   u,
                      real4x4 localToWorld,
                      real    radius,
                  out real    lightPdf,
                  out real3   P,
                  out real3   Ns)
{
    real u1 = u.x;
    real u2 = u.y;

    // Random point at hemisphere surface
    Ns = -SampleHemisphereUniform(u1, u2); // We want the y down hemisphere
    P = radius * Ns;

    // Transform to world space
    P = mul(real4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, (real3x3)(localToWorld));

    // pdf is inverse of area
    lightPdf = 1.0 / (TWO_PI * radius * radius);
}

// Note: The cylinder has no end caps (i.e. no disk on the side)
void SampleCylinder(real2   u,
                    real4x4 localToWorld,
                    real    radius,
                    real    width,
                out real    lightPdf,
                out real3   P,
                out real3   Ns)
{
    real u1 = u.x;
    real u2 = u.y;

    // Random point at cylinder surface
    real t = (u1 - 0.5) * width;
    real theta = 2.0 * PI * u2;
    real cosTheta = cos(theta);
    real sinTheta = sin(theta);

    // Cylinder are align on the right axis
    P = real3(t, radius * cosTheta, radius * sinTheta);
    Ns = normalize(real3(0.0, cosTheta, sinTheta));

    // Transform to world space
    P = mul(real4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, (real3x3)(localToWorld));

    // pdf is inverse of area
    lightPdf = 1.0 / (TWO_PI * radius * width);
}

void SampleRectangle(real2   u,
                     real4x4 localToWorld,
                     real    width,
                     real    height,
                 out real    lightPdf,
                 out real3   P,
                 out real3   Ns)
{
    // Random point at rectangle surface
    P = real3((u.x - 0.5) * width, (u.y - 0.5) * height, 0);
    Ns = real3(0, 0, -1); // Light down (-Z)

    // Transform to world space
    P = mul(real4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, (real3x3)(localToWorld));

    // pdf is inverse of area
    lightPdf = 1.0 / (width * height);
}

void SampleDisk(real2   u,
                real4x4 localToWorld,
                real    radius,
            out real    lightPdf,
            out real3   P,
            out real3   Ns)
{
    // Random point at disk surface
    P  = real3(radius * SampleDiskUniform(u.x, u.y), 0);
    Ns = real3(0.0, 0.0, -1.0); // Light down (-Z)

    // Transform to world space
    P = mul(real4(P, 1.0), localToWorld).xyz;
    Ns = mul(Ns, (real3x3)(localToWorld));

    // pdf is inverse of area
    lightPdf = 1.0 / (PI * radius * radius);
}

#endif // UNITY_SAMPLING_INCLUDED
