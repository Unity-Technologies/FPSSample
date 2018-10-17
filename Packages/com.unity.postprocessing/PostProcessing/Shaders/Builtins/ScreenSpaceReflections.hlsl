#ifndef UNITY_POSTFX_SSR
#define UNITY_POSTFX_SSR

#include "UnityCG.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityStandardUtils.cginc"

#define SSR_MINIMUM_ATTENUATION 0.275
#define SSR_ATTENUATION_SCALE (1.0 - SSR_MINIMUM_ATTENUATION)

#define SSR_VIGNETTE_INTENSITY _VignetteIntensity
#define SSR_VIGNETTE_SMOOTHNESS 5.

#define SSR_COLOR_NEIGHBORHOOD_SAMPLE_SPREAD 1.0

#define SSR_FINAL_BLEND_STATIC_FACTOR 0.95
#define SSR_FINAL_BLEND_DYNAMIC_FACTOR 0.7

#define SSR_ENABLE_CONTACTS 0
#define SSR_KILL_FIREFLIES 0

//
// Helper structs
//
struct Ray
{
    float3 origin;
    float3 direction;
};

struct Segment
{
    float3 start;
    float3 end;

    float3 direction;
};

struct Result
{
    bool isHit;

    float2 uv;
    float3 position;

    int iterationCount;
};

//
// Uniforms
//
Texture2D _MainTex; SamplerState sampler_MainTex;
Texture2D _History; SamplerState sampler_History;

Texture2D _CameraDepthTexture; SamplerState sampler_CameraDepthTexture;
Texture2D _CameraMotionVectorsTexture; SamplerState sampler_CameraMotionVectorsTexture;
Texture2D _CameraReflectionsTexture; SamplerState sampler_CameraReflectionsTexture;

Texture2D _CameraGBufferTexture0; // albedo = g[0].rgb
Texture2D _CameraGBufferTexture1; // roughness = g[1].a
Texture2D _CameraGBufferTexture2; SamplerState sampler_CameraGBufferTexture2; // normal.xyz 2. * g[2].rgb - 1.

Texture2D _Noise; SamplerState sampler_Noise;

Texture2D _Test; SamplerState sampler_Test;
Texture2D _Resolve; SamplerState sampler_Resolve;

float4 _MainTex_TexelSize;
float4 _Test_TexelSize;

float4x4 _ViewMatrix;
float4x4 _InverseViewMatrix;
float4x4 _InverseProjectionMatrix;
float4x4 _ScreenSpaceProjectionMatrix;

float4 _Params; // x: vignette intensity, y: distance fade, z: maximum march distance, w: blur pyramid lod count
float4 _Params2; // x: aspect ratio, y: noise tiling, z: thickness, w: maximum iteration count
#define _Attenuation .25
#define _VignetteIntensity _Params.x
#define _DistanceFade _Params.y
#define _MaximumMarchDistance _Params.z
#define _BlurPyramidLODCount _Params.w
#define _AspectRatio _Params2.x
#define _NoiseTiling _Params2.y
#define _Bandwidth _Params2.z
#define _MaximumIterationCount _Params2.w

//
// Helper functions
//
float Attenuate(float2 uv)
{
    float offset = min(1.0 - max(uv.x, uv.y), min(uv.x, uv.y));

    float result = offset / (SSR_ATTENUATION_SCALE * _Attenuation + SSR_MINIMUM_ATTENUATION);
    result = saturate(result);

    return pow(result, 0.5);
}

float Vignette(float2 uv)
{
    float2 k = abs(uv - 0.5) * SSR_VIGNETTE_INTENSITY;
    k.x *= _MainTex_TexelSize.y * _MainTex_TexelSize.z;
    return pow(saturate(1.0 - dot(k, k)), SSR_VIGNETTE_SMOOTHNESS);
}

float3 GetViewSpacePosition(float2 uv)
{
    float depth = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv), 0).r;
    float4 result = mul(_InverseProjectionMatrix, float4(2.0 * uv - 1.0, depth, 1.0));
    return result.xyz / result.w;
}

float GetSquaredDistance(float2 first, float2 second)
{
    first -= second;
    return dot(first, first);
}

float4 ProjectToScreenSpace(float3 position)
{
    return float4(
        _ScreenSpaceProjectionMatrix[0][0] * position.x + _ScreenSpaceProjectionMatrix[0][2] * position.z,
        _ScreenSpaceProjectionMatrix[1][1] * position.y + _ScreenSpaceProjectionMatrix[1][2] * position.z,
        _ScreenSpaceProjectionMatrix[2][2] * position.z + _ScreenSpaceProjectionMatrix[2][3],
        _ScreenSpaceProjectionMatrix[3][2] * position.z
    );
}

// Heavily adapted from McGuire and Mara's original implementation
// http://casual-effects.blogspot.com/2014/08/screen-space-ray-tracing.html
Result March(Ray ray, VaryingsDefault input)
{
    Result result;

    result.isHit = false;

    result.uv = 0.0;
    result.position = 0.0;

    result.iterationCount = 0;

    Segment segment;

    segment.start = ray.origin;

    float end = ray.origin.z + ray.direction.z * _MaximumMarchDistance;
    float magnitude = _MaximumMarchDistance;

    if (end > -_ProjectionParams.y)
        magnitude = (-_ProjectionParams.y - ray.origin.z) / ray.direction.z;

    segment.end = ray.origin + ray.direction * magnitude;

    float4 r = ProjectToScreenSpace(segment.start);
    float4 q = ProjectToScreenSpace(segment.end);

    const float2 homogenizers = rcp(float2(r.w, q.w));

    segment.start *= homogenizers.x;
    segment.end *= homogenizers.y;

    float4 endPoints = float4(r.xy, q.xy) * homogenizers.xxyy;
    endPoints.zw += step(GetSquaredDistance(endPoints.xy, endPoints.zw), 0.0001) * max(_Test_TexelSize.x, _Test_TexelSize.y);

    float2 displacement = endPoints.zw - endPoints.xy;

    bool isPermuted = false;

    if (abs(displacement.x) < abs(displacement.y))
    {
        isPermuted = true;

        displacement = displacement.yx;
        endPoints.xyzw = endPoints.yxwz;
    }

    float direction = sign(displacement.x);
    float normalizer = direction / displacement.x;

    segment.direction = (segment.end - segment.start) * normalizer;
    float4 derivatives = float4(float2(direction, displacement.y * normalizer), (homogenizers.y - homogenizers.x) * normalizer, segment.direction.z);

    float stride = 1.0 - min(1.0, -ray.origin.z * 0.01);

    float2 uv = input.texcoord * _NoiseTiling;
    uv.y *= _AspectRatio;

    float jitter = _Noise.SampleLevel(sampler_Noise, uv + _WorldSpaceCameraPos.xz, 0).a;
    stride *= _Bandwidth;

    derivatives *= stride;
    segment.direction *= stride;

    float2 z = 0.0;
    float4 tracker = float4(endPoints.xy, homogenizers.x, segment.start.z) + derivatives * jitter;

    for (int i = 0; i < _MaximumIterationCount; ++i)
    {
        if (any(result.uv < 0.0) || any(result.uv > 1.0))
        {
            result.isHit = false;
            return result;
        }

        tracker += derivatives;

        z.x = z.y;
        z.y = tracker.w + derivatives.w * 0.5;
        z.y /= tracker.z + derivatives.z * 0.5;

#if SSR_KILL_FIREFLIES
        UNITY_FLATTEN
        if (z.y < -_MaximumMarchDistance)
        {
            result.isHit = false;
            return result;
        }
#endif

        UNITY_FLATTEN
        if (z.y > z.x)
        {
            float k = z.x;
            z.x = z.y;
            z.y = k;
        }

        uv = tracker.xy;

        UNITY_FLATTEN
        if (isPermuted)
            uv = uv.yx;

        uv *= _Test_TexelSize.xy;

        float d = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv), 0);
        float depth = -LinearEyeDepth(d);

        UNITY_FLATTEN
        if (z.y < depth)
        {
            result.uv = uv;
            result.isHit = true;
            result.iterationCount = i + 1;
            return result;
        }
    }

    return result;
}

//
// Fragment shaders
//
float4 FragTest(VaryingsDefault i) : SV_Target
{
    float4 gbuffer2 = _CameraGBufferTexture2.Sample(sampler_CameraGBufferTexture2, i.texcoordStereo);

    if (dot(gbuffer2, 1.0) == 0.0)
        return 0.0;

    float3 normal = 2.0 * gbuffer2.rgb - 1.0;
    normal = mul((float3x3)_ViewMatrix, normal);

    Ray ray;

    ray.origin = GetViewSpacePosition(i.texcoord);

    if (ray.origin.z < -_MaximumMarchDistance)
        return 0.0;

    ray.direction = normalize(reflect(normalize(ray.origin), normal));

    if (ray.direction.z > 0.0)
        return 0.0;

    Result result = March(ray, i);

    float confidence = (float)result.iterationCount / (float)_MaximumIterationCount;
    return float4(result.uv, confidence, (float)result.isHit);
}

float4 FragResolve(VaryingsDefault i) : SV_Target
{
    float4 test = _Test.Load(int3(i.vertex.xy, 0));

    if (test.w == 0.0)
        return _MainTex.Sample(sampler_MainTex, i.texcoordStereo);

    float4 color = _MainTex.SampleLevel(sampler_MainTex, UnityStereoTransformScreenSpaceTex(test.xy), 0);

    float confidence = test.w * Attenuate(test.xy) * Vignette(test.xy);

    color.rgb *= confidence;
    color.a = test.z;

    return color;
}

float4 FragReproject(VaryingsDefault i) : SV_Target
{
    float2 motion = _CameraMotionVectorsTexture.SampleLevel(sampler_CameraMotionVectorsTexture, i.texcoordStereo, 0).xy;
    float2 uv = i.texcoord - motion;

    const float2 k = SSR_COLOR_NEIGHBORHOOD_SAMPLE_SPREAD * _MainTex_TexelSize.xy;

    float4 color = _MainTex.SampleLevel(sampler_MainTex, i.texcoordStereo, 0);

    // 0 1 2
    // 3
    float4x4 top = float4x4(
        _MainTex.SampleLevel(sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord + float2(-k.x, -k.y)), 0),
        _MainTex.SampleLevel(sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord + float2( 0.0, -k.y)), 0),
        _MainTex.SampleLevel(sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord + float2( k.x, -k.y)), 0),
        _MainTex.SampleLevel(sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord + float2(-k.x,  0.0)), 0)
    );

    //     0
    // 1 2 3
    float4x4 bottom = float4x4(
        _MainTex.SampleLevel(sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord + float2( k.x, 0.0)), 0),
        _MainTex.SampleLevel(sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord + float2(-k.x, k.y)), 0),
        _MainTex.SampleLevel(sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord + float2( 0.0, k.y)), 0),
        _MainTex.SampleLevel(sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord + float2( k.x, k.y)), 0)
    );

    // PS4 INTRINSIC_MINMAX3
    #if SHADER_API_PSSL
        float4 minimum = min3(min3(min3(min3(top[0], top[1], top[2]), top[3], bottom[0]), bottom[1], bottom[2]), bottom[3], color);
        float4 maximum = max3(max3(max3(max3(top[0], top[1], top[2]), top[3], bottom[0]), bottom[1], bottom[2]), bottom[3], color);
    #else
        float4 minimum = min(min(min(min(min(min(min(min(top[0], top[1]), top[2]), top[3]), bottom[0]), bottom[1]), bottom[2]), bottom[3]), color);
        float4 maximum = max(max(max(max(max(max(max(max(top[0], top[1]), top[2]), top[3]), bottom[0]), bottom[1]), bottom[2]), bottom[3]), color);
    #endif

    float4 history = _History.SampleLevel(sampler_History, UnityStereoTransformScreenSpaceTex(uv), 0);
    history = clamp(history, minimum, maximum);

    color.a = saturate(smoothstep(0.002 * _MainTex_TexelSize.z, 0.0035 * _MainTex_TexelSize.z, length(motion)));

    float weight = clamp(lerp(SSR_FINAL_BLEND_STATIC_FACTOR, SSR_FINAL_BLEND_DYNAMIC_FACTOR,
        history.a * 100.0), SSR_FINAL_BLEND_DYNAMIC_FACTOR, SSR_FINAL_BLEND_STATIC_FACTOR);

    color.a *= 0.85;
    return lerp(color, history, weight);
}

float4 FragComposite(VaryingsDefault i) : SV_Target
{
    float z = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, i.texcoordStereo, 0).r;

    if (Linear01Depth(z) > 0.999)
        return _MainTex.Sample(sampler_MainTex, i.texcoordStereo);

    float4 gbuffer0 = _CameraGBufferTexture0.Load(int3(i.vertex.xy, 0));
    float4 gbuffer1 = _CameraGBufferTexture1.Load(int3(i.vertex.xy, 0));
    float4 gbuffer2 = _CameraGBufferTexture2.Load(int3(i.vertex.xy, 0));

    float oneMinusReflectivity = 0.0;
    EnergyConservationBetweenDiffuseAndSpecular(gbuffer0.rgb, gbuffer1.rgb, oneMinusReflectivity);

    float3 normal = 2.0 * gbuffer2.rgb - 1.0;
    float3 position = GetViewSpacePosition(i.texcoord);

    float3 eye = mul((float3x3)_InverseViewMatrix, normalize(position));
    position = mul(_InverseViewMatrix, float4(position, 1.0)).xyz;

#if SSR_ENABLE_CONTACTS
    float4 test = _Test.SampleLevel(sampler_Test, i.texcoordStereo, 0);
    float4 resolve = _Resolve.SampleLevel(sampler_Resolve, i.texcoordStereo, SmoothnessToRoughness(gbuffer1.a) * (_BlurPyramidLODCount - 1.0) * test.z + 1.0);
#else
    float4 resolve = _Resolve.SampleLevel(sampler_Resolve, i.texcoordStereo, SmoothnessToRoughness(gbuffer1.a) * (_BlurPyramidLODCount - 1.0) + 1.0);
#endif

    float confidence = saturate(2.0 * dot(-eye, normalize(reflect(-eye, normal))));

    UnityLight light;
    light.color = 0.0;
    light.dir = 0.0;
    light.ndotl = 0.0;

    UnityIndirect indirect;
    indirect.diffuse = 0.0;
    indirect.specular = resolve.rgb;

    resolve.rgb = UNITY_BRDF_PBS(gbuffer0.rgb, gbuffer1.rgb, oneMinusReflectivity, gbuffer1.a, normal, -eye, light, indirect).rgb;

    float4 reflectionProbes = _CameraReflectionsTexture.Sample(sampler_CameraReflectionsTexture, i.texcoordStereo);

    float4 color = _MainTex.Sample(sampler_MainTex, i.texcoordStereo);
    color.rgb = max(0.0, color.rgb - reflectionProbes.rgb);

    resolve.a *= 2. * resolve.a; // 2 and 1.5 are quite important for the correct ratio of 3:2 distribution
    float fade = 1.0 - saturate(1.5 * resolve.a * smoothstep(0.5, 1.0, 1.5 * resolve.a) * _DistanceFade);

    resolve.rgb = lerp(reflectionProbes.rgb, resolve.rgb, confidence * fade);
    color.rgb += resolve.rgb * gbuffer0.a;

    return color;
}

#endif // UNITY_POSTFX_SSR
