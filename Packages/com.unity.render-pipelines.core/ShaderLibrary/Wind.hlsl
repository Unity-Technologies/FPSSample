// TODO: no global variable or resource declarations in the Shader Library. Functions and macros only!
TEXTURE2D(_WIND_SETTINGS_TexNoise);
SAMPLER(sampler_WIND_SETTINGS_TexNoise);
TEXTURE2D(_WIND_SETTINGS_TexGust);
SAMPLER(sampler_WIND_SETTINGS_TexGust);

float4  WIND_SETTINGS_WorldDirectionAndSpeed;
float   WIND_SETTINGS_FlexNoiseScale;
float   WIND_SETTINGS_ShiverNoiseScale;
float   WIND_SETTINGS_Turbulence;
float   WIND_SETTINGS_GustSpeed;
float   WIND_SETTINGS_GustScale;
float   WIND_SETTINGS_GustWorldScale;

float AttenuateTrunk(float x, float s)
{
    float r = (x / s);
    return PositivePow(r,1/s);
}


float3 Rotate(float3 pivot, float3 position, float3 rotationAxis, float angle)
{
    rotationAxis = normalize(rotationAxis);
    float3 cpa = pivot + rotationAxis * dot(rotationAxis, position - pivot);
    return cpa + ((position - cpa) * cos(angle) + cross(rotationAxis, (position - cpa)) * sin(angle));
}

struct WindData
{
    float3 Direction;
    float Strength;
    float3 ShiverStrength;
    float3 ShiverDirection;
};


float3 texNoise(float3 worldPos, float LOD)
{
    return SAMPLE_TEXTURE2D_LOD(_WIND_SETTINGS_TexNoise, sampler_WIND_SETTINGS_TexNoise, worldPos.xz, LOD).xyz -0.5;
}

float texGust(float3 worldPos, float LOD)
{
    return SAMPLE_TEXTURE2D_LOD(_WIND_SETTINGS_TexGust, sampler_WIND_SETTINGS_TexGust, worldPos.xz, LOD).x;
}


WindData GetAnalyticalWind(float3 WorldPosition, float3 PivotPosition, float drag, float shiverDrag, float initialBend, float4 time)
{
    WindData result;
    float3 normalizedDir = normalize(WIND_SETTINGS_WorldDirectionAndSpeed.xyz);

    float3 worldOffset = normalizedDir * WIND_SETTINGS_WorldDirectionAndSpeed.w * time.y;
    float3 gustWorldOffset = normalizedDir * WIND_SETTINGS_GustSpeed * time.y;

    // Trunk noise is base wind + gusts + noise

    float3 trunk = float3(0,0,0);

    if(WIND_SETTINGS_WorldDirectionAndSpeed.w > 0.0 || WIND_SETTINGS_Turbulence > 0.0)
    {
        trunk = texNoise((PivotPosition - worldOffset)*WIND_SETTINGS_FlexNoiseScale,3);
    }

    float gust  = 0.0;

    if(WIND_SETTINGS_GustSpeed > 0.0)
    {
        gust = texGust((PivotPosition - gustWorldOffset)*WIND_SETTINGS_GustWorldScale,3);
        gust = pow(gust, 2) * WIND_SETTINGS_GustScale;
    }

    float3 trunkNoise =
        (
                (normalizedDir * WIND_SETTINGS_WorldDirectionAndSpeed.w)
                + (gust * normalizedDir * WIND_SETTINGS_GustSpeed)
                + (trunk * WIND_SETTINGS_Turbulence)
        ) * drag;

    // Shiver Noise
    float3 shiverNoise = texNoise((WorldPosition - worldOffset)*WIND_SETTINGS_ShiverNoiseScale,0) * shiverDrag * WIND_SETTINGS_Turbulence;

    float3 dir = trunkNoise;
    float flex = length(trunkNoise) + initialBend;
    float shiver = length(shiverNoise);

    result.Direction = dir;
    result.ShiverDirection = shiverNoise;
    result.Strength = flex;
    result.ShiverStrength = shiver + shiver * gust;

    return result;
}



void ApplyWindDisplacement( inout float3    positionWS,
                            float3          normalWS,
                            float3          rootWP,
                            float           stiffness,
                            float           drag,
                            float           shiverDrag,
                            float           shiverDirectionality,
                            float           initialBend,
                            float           shiverMask,
                            float4          time)
{
    WindData wind = GetAnalyticalWind(positionWS, rootWP, drag, shiverDrag, initialBend, time);

    if (wind.Strength > 0.0)
    {
        float att = AttenuateTrunk(distance(positionWS, rootWP), stiffness);
        float3 rotAxis = cross(float3(0, 1, 0), wind.Direction);

        positionWS = Rotate(rootWP, positionWS, rotAxis, (wind.Strength) * 0.001 * att);

        float3 shiverDirection = normalize(lerp(normalWS, normalize(wind.Direction + wind.ShiverDirection), shiverDirectionality));
        positionWS += wind.ShiverStrength * shiverDirection * shiverMask;
    }

}
