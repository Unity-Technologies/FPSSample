Shader "Hidden/CubeToPano" {
Properties {
    _SrcBlend ("", Float) = 1
    _DstBlend ("", Float) = 1
}
SubShader {




Pass
{
    ZWrite Off
    ZTest Always
    Cull Off
    Blend Off


CGPROGRAM
#pragma target 4.5
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

UNITY_DECLARE_TEXCUBE(_srcCubeTexture);

uniform int _cubeMipLvl;


struct v2f {
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

v2f vert (float4 vertex : POSITION, float2 texcoord : TEXCOORD0)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(vertex);
    o.texcoord = texcoord.xy;
    return o;
}

half2 DirectionToSphericalTexCoordinate(half3 dir_in)      // use this for the lookup
{
    half3 dir = normalize(dir_in);
    // coordinate frame is (-Z,X) meaning negative Z is primary axis and X is secondary axis.
    float recipPi = 1.0/3.1415926535897932384626433832795;
    return half2( 1.0-0.5*recipPi*atan2(dir.x, -dir.z), asin(dir.y)*recipPi+0.5 );
}

half3 SphericalTexCoordinateToDirection(half2 sphTexCoord)
{
    float pi = 3.1415926535897932384626433832795;
    float theta = (1-sphTexCoord.x) * (pi*2);
    float phi = (sphTexCoord.y-0.5) * pi;

    float csTh, siTh, csPh, siPh;
    sincos(theta, siTh, csTh);
    sincos(phi, siPh, csPh);

    // theta is 0 at negative Z (backwards). Coordinate frame is (-Z,X) meaning negative Z is primary axis and X is secondary axis.
    return float3(siTh*csPh, siPh, -csTh*csPh);
}

half4 frag (v2f i) : SV_Target
{
    uint2 pixCoord = ((uint2) i.vertex.xy);

    half3 dir = SphericalTexCoordinateToDirection(i.texcoord.xy);

    return (half4) UNITY_SAMPLE_TEXCUBE_LOD(_srcCubeTexture, dir, (float) _cubeMipLvl);
}

ENDCG
}

}
Fallback Off
}
