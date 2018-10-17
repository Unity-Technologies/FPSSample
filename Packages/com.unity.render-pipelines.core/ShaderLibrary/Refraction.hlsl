#ifndef UNITY_REFRACTION_INCLUDED
#define UNITY_REFRACTION_INCLUDED

//-----------------------------------------------------------------------------
// Util refraction
//-----------------------------------------------------------------------------

struct RefractionModelResult
{
    real  dist;       // length of the transmission during refraction through the shape
    float3 positionWS; // out ray position
    real3 rayWS;      // out ray direction
};

RefractionModelResult RefractionModelSphere(real3 V, float3 positionWS, real3 normalWS, real ior, real thickness)
{
    // Sphere shape model:
    //  We approximate locally the shape of the object as sphere, that is tangent to the shape.
    //  The sphere has a diameter of {thickness}
    //  The center of the sphere is at {positionWS} - {normalWS} * {thickness}
    //
    //  So the light is refracted twice: in and out of the tangent sphere

    // First refraction (tangent sphere in)
    // Refracted ray
    real3 R1 = refract(-V, normalWS, 1.0 / ior);
    // Center of the tangent sphere
    real3 C = positionWS - normalWS * thickness * 0.5;

    // Second refraction (tangent sphere out)
    real NoR1 = dot(normalWS, R1);
    // Optical depth within the sphere
    real dist = -NoR1 * thickness;
    // Out hit point in the tangent sphere
    real3 P1 = positionWS + R1 * dist;
    // Out normal
    real3 N1 = normalize(C - P1);
    // Out refracted ray
    real3 R2 = refract(R1, N1, ior);
    real N1oR2 = dot(N1, R2);
    real VoR1 = dot(V, R1);

    RefractionModelResult result;
    result.dist = dist;
    result.positionWS = P1;
    result.rayWS = R2;

    return result;
}

RefractionModelResult RefractionModelPlane(real3 V, float3 positionWS, real3 normalWS, real ior, real thickness)
{
    // Plane shape model:
    //  We approximate locally the shape of the object as a plane with normal {normalWS} at {positionWS}
    //  with a thickness {thickness}

    // Refracted ray
    real3 R = refract(-V, normalWS, 1.0 / ior);

    // Optical depth within the thin plane
    real dist = thickness / dot(R, -normalWS);

    RefractionModelResult result;
    result.dist = dist;
    result.positionWS = positionWS + R * dist;
    result.rayWS = -V;

    return result;
}
#endif
