#ifndef UNITY_POSTFX_DISTORTION
#define UNITY_POSTFX_DISTORTION

float4 _Distortion_Amount;
float4 _Distortion_CenterScale;

float2 Distort(float2 uv)
{
    // Note: lens distortion is automatically disabled in VR so we won't bother handling stereo uvs
    #if DISTORT
    {
        uv = (uv - 0.5) * _Distortion_Amount.z + 0.5;
        float2 ruv = _Distortion_CenterScale.zw * (uv - 0.5 - _Distortion_CenterScale.xy);
        float ru = length(float2(ruv));

        UNITY_BRANCH
        if (_Distortion_Amount.w > 0.0)
        {
            float wu = ru * _Distortion_Amount.x;
            ru = tan(wu) * (1.0 / (ru * _Distortion_Amount.y));
            uv = uv + ruv * (ru - 1.0);
        }
        else
        {
            ru = (1.0 / ru) * _Distortion_Amount.x * atan(ru * _Distortion_Amount.y);
            uv = uv + ruv * (ru - 1.0);
        }
    }
    #endif

    return uv;
}

#endif // UNITY_POSTFX_DISTORTION
