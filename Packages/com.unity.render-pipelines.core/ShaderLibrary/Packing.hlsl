#ifndef UNITY_PACKING_INCLUDED
#define UNITY_PACKING_INCLUDED

//-----------------------------------------------------------------------------
// Normal packing
//-----------------------------------------------------------------------------

real3 PackNormalMaxComponent(real3 n)
{
    return (n / Max3(abs(n.x), abs(n.y), abs(n.z))) * 0.5 + 0.5;
}

real3 UnpackNormalMaxComponent(real3 n)
{
    return normalize(n * 2.0 - 1.0);
}

// Ref: http://www.vis.uni-stuttgart.de/~engelhts/paper/vmvOctaMaps.pdf
// Encode with Oct, this function work with any size of output
// return real between [-1, 1]
real2 PackNormalOctRectEncode(real3 n)
{
    // Perform planar projection.
    real3 p = n * rcp(dot(abs(n), 1.0));
    real  x = p.x, y = p.y, z = p.z;

    // Unfold the octahedron.
    // Also correct the aspect ratio from 2:1 to 1:1.
    real r = saturate(0.5 - 0.5 * x + 0.5 * y);
    real g = x + y;

    // Negative hemisphere on the left, positive on the right.
    return real2(CopySign(r, z), g);
}

real3 UnpackNormalOctRectEncode(real2 f)
{
    real r = f.r, g = f.g;

    // Solve for {x, y, z} given {r, g}.
    real x = 0.5 + 0.5 * g - abs(r);
    real y = g - x;
    real z = max(1.0 - abs(x) - abs(y), FLT_EPS); // EPS is absolutely crucial for anisotropy

    real3 p = real3(x, y, CopySign(z, r));

    return normalize(p);
}

// Ref: http://jcgt.org/published/0003/02/01/paper.pdf
// Encode with Oct, this function work with any size of output
// return real between [-1, 1]
real2 PackNormalOctQuadEncode(real3 n)
{
    //real l1norm    = dot(abs(n), 1.0);
    //real2 res0     = n.xy * (1.0 / l1norm);

    //real2 val      = 1.0 - abs(res0.yx);
    //return (n.zz < real2(0.0, 0.0) ? (res0 >= 0.0 ? val : -val) : res0);

    // Optimized version of above code:
    n *= rcp(dot(abs(n), 1.0));
    real t = saturate(-n.z);
    return n.xy + (n.xy >= 0.0 ? t : -t);
}

real3 UnpackNormalOctQuadEncode(real2 f)
{
    real3 n = real3(f.x, f.y, 1.0 - abs(f.x) - abs(f.y));

    //real2 val = 1.0 - abs(n.yx);
    //n.xy = (n.zz < real2(0.0, 0.0) ? (n.xy >= 0.0 ? val : -val) : n.xy);

    // Optimized version of above code:
    real t = max(-n.z, 0.0);
    n.xy += n.xy >= 0.0 ? -t.xx : t.xx;

    return normalize(n);
}

real2 PackNormalHemiOctEncode(real3 n)
{
    real l1norm = dot(abs(n), 1.0);
    real2 res = n.xy * (1.0 / l1norm);

    return real2(res.x + res.y, res.x - res.y);
}

real3 UnpackNormalHemiOctEncode(real2 f)
{
    real2 val = real2(f.x + f.y, f.x - f.y) * 0.5;
    real3 n = real3(val, 1.0 - dot(abs(val), 1.0));

    return normalize(n);
}

// Tetrahedral encoding - Looks like Tetra encoding 10:10 + 2 is similar to oct 11:11, as oct is cheaper prefer it
// To generate the basisNormal below we use these 4 vertex of a regular tetrahedron
// v0 = real3(1.0, 0.0, -1.0 / sqrt(2.0));
// v1 = real3(-1.0, 0.0, -1.0 / sqrt(2.0));
// v2 = real3(0.0, 1.0, 1.0 / sqrt(2.0));
// v3 = real3(0.0, -1.0, 1.0 / sqrt(2.0));
// Then we normalize the average of each face's vertices
// normalize(v0 + v1 + v2), etc...
static const real3 tetraBasisNormal[4] =
{
    real3(0., 0.816497, -0.57735),
    real3(-0.816497, 0., 0.57735),
    real3(0.816497, 0., 0.57735),
    real3(0., -0.816497, -0.57735)
};

// Then to get the local matrix (with z axis rotate to basisNormal) use GetLocalFrame(basisNormal[xxx])
static const real3x3 tetraBasisArray[4] =
{
    real3x3(-1., 0., 0.,0., 0.57735, 0.816497,0., 0.816497, -0.57735),
    real3x3(0., -1., 0.,0.57735, 0., 0.816497,-0.816497, 0., 0.57735),
    real3x3(0., 1., 0.,-0.57735, 0., 0.816497,0.816497, 0., 0.57735),
    real3x3(1., 0., 0.,0., -0.57735, 0.816497,0., -0.816497, -0.57735)
};

// Return [-1..1] vector2 oriented in plane of the faceIndex of a regular tetrahedron
real2 PackNormalTetraEncode(real3 n, out uint faceIndex)
{
    // Retrieve the tetrahedra's face for the normal direction
    // It is the one with the greatest dot value with face normal
    real dot0 = dot(n, tetraBasisNormal[0]);
    real dot1 = dot(n, tetraBasisNormal[1]);
    real dot2 = dot(n, tetraBasisNormal[2]);
    real dot3 = dot(n, tetraBasisNormal[3]);

    real maxi0 = max(dot0, dot1);
    real maxi1 = max(dot2, dot3);
    real maxi = max(maxi0, maxi1);

    // Get the index from the greatest dot
    if (maxi == dot0)
        faceIndex = 0;
    else if (maxi == dot1)
        faceIndex = 1;
    else if (maxi == dot2)
        faceIndex = 2;
    else //(maxi == dot3)
        faceIndex = 3;

    // Rotate n into this local basis
    n = mul(tetraBasisArray[faceIndex], n);

    // Project n onto the local plane
    return n.xy;
}

// Assume f [-1..1]
real3 UnpackNormalTetraEncode(real2 f, uint faceIndex)
{
    // Recover n from local plane
    real3 n = real3(f.xy, sqrt(1.0 - dot(f.xy, f.xy)));
    // Inverse of transform PackNormalTetraEncode (just swap order in mul as we have a rotation)
    return mul(n, tetraBasisArray[faceIndex]);
}

// Unpack from normal map
real3 UnpackNormalRGB(real4 packedNormal, real scale = 1.0)
{
    real3 normal;
    normal.xyz = packedNormal.rgb * 2.0 - 1.0;
    normal.xy *= scale;
    return normalize(normal);
}

real3 UnpackNormalRGBNoScale(real4 packedNormal)
{
    return packedNormal.rgb * 2.0 - 1.0;
}

real3 UnpackNormalAG(real4 packedNormal, real scale = 1.0)
{
    real3 normal;
    normal.xy = packedNormal.ag * 2.0 - 1.0;
    normal.xy *= scale;
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

// Unpack normal as DXT5nm (1, y, 0, x) or BC5 (x, y, 0, 1)
real3 UnpackNormalmapRGorAG(real4 packedNormal, real scale = 1.0)
{
    // Convert to (?, y, 0, x)
    packedNormal.a *= packedNormal.r;
    return UnpackNormalAG(packedNormal, scale);
}

//-----------------------------------------------------------------------------
// HDR packing
//-----------------------------------------------------------------------------

// HDR Packing not defined in GLES2
#if !defined(SHADER_API_GLES)

// Ref: http://realtimecollisiondetection.net/blog/?p=15
real4 PackToLogLuv(real3 vRGB)
{
    // M matrix, for encoding
    const real3x3 M = real3x3(
        0.2209, 0.3390, 0.4184,
        0.1138, 0.6780, 0.7319,
        0.0102, 0.1130, 0.2969);

    real4 vResult;
    real3 Xp_Y_XYZp = mul(vRGB, M);
    Xp_Y_XYZp = max(Xp_Y_XYZp, real3(1e-6, 1e-6, 1e-6));
    vResult.xy = Xp_Y_XYZp.xy / Xp_Y_XYZp.z;
    real Le = 2.0 * log2(Xp_Y_XYZp.y) + 127.0;
    vResult.w = frac(Le);
    vResult.z = (Le - (floor(vResult.w * 255.0)) / 255.0) / 255.0;
    return vResult;
}

real3 UnpackFromLogLuv(real4 vLogLuv)
{
    // Inverse M matrix, for decoding
    const real3x3 InverseM = real3x3(
        6.0014, -2.7008, -1.7996,
        -1.3320, 3.1029, -5.7721,
        0.3008, -1.0882, 5.6268);

    real Le = vLogLuv.z * 255.0 + vLogLuv.w;
    real3 Xp_Y_XYZp;
    Xp_Y_XYZp.y = exp2((Le - 127.0) / 2.0);
    Xp_Y_XYZp.z = Xp_Y_XYZp.y / vLogLuv.y;
    Xp_Y_XYZp.x = vLogLuv.x * Xp_Y_XYZp.z;
    real3 vRGB = mul(Xp_Y_XYZp, InverseM);
    return max(vRGB, real3(0.0, 0.0, 0.0));
}

// The standard 32-bit HDR color format
uint PackToR11G11B10f(float3 rgb)
{
    uint r = (f32tof16(rgb.x) << 17) & 0xFFE00000;
    uint g = (f32tof16(rgb.y) << 6) & 0x001FFC00;
    uint b = (f32tof16(rgb.z) >> 5) & 0x000003FF;
    return r | g | b;
}

float3 UnpackFromR11G11B10f(uint rgb)
{
    float r = f16tof32((rgb >> 17) & 0x7FF0);
    float g = f16tof32((rgb >> 6) & 0x7FF0);
    float b = f16tof32((rgb << 5) & 0x7FE0);
    return float3(r, g, b);
}

#endif // SHADER_API_GLES

//-----------------------------------------------------------------------------
// Quaternion packing
//-----------------------------------------------------------------------------

// Ref: https://cedec.cesa.or.jp/2015/session/ENG/14698.html The Rendering Materials of Far Cry 4

/*
// This is GCN intrinsic
uint FindBiggestComponent(real4 q)
{
    uint xyzIndex = CubeMapFaceID(q.x, q.y, q.z) * 0.5f;
    uint wIndex = 3;

    bool wBiggest = abs(q.w) > max3(abs(q.x), qbs(q.y), qbs(q.z));

    return wBiggest ? wIndex : xyzIndex;
}

// Pack a quaternion into a 10:10:10:2
real4  PackQuat(real4 quat)
{
    uint index = FindBiggestComponent(quat);

    if (index == 0) quat = quat.yzwx;
    if (index == 1) quat = quat.xzwy;
    if (index == 2) quat = quat.xywz;

    real4 packedQuat;
    packedQuat.xyz = quat.xyz * FastSign(quat.w) * sqrt(0.5) + 0.5;
    packedQuat.w = index / 3.0;

    return packedQuat;
}
*/

// Unpack a quaternion from a 10:10:10:2
real4 UnpackQuat(real4 packedQuat)
{
    uint index = (uint)(packedQuat.w * 3.0);

    real4 quat;
    quat.xyz = packedQuat.xyz * sqrt(2.0) - (1.0 / sqrt(2.0));
    quat.w = sqrt(1.0 - saturate(dot(quat.xyz, quat.xyz)));

    if (index == 0) quat = quat.wxyz;
    if (index == 1) quat = quat.xwyz;
    if (index == 2) quat = quat.xywz;

    return quat;
}

// Integer and Float packing not defined in GLES2
#if !defined(SHADER_API_GLES)

//-----------------------------------------------------------------------------
// Integer packing
//-----------------------------------------------------------------------------

// Packs an integer stored using at most 'numBits' into a [0..1] real.
real PackInt(uint i, uint numBits)
{
    uint maxInt = (1u << numBits) - 1u;
    return saturate(i * rcp(maxInt));
}

// Unpacks a [0..1] real into an integer of size 'numBits'.
uint UnpackInt(real f, uint numBits)
{
    uint maxInt = (1u << numBits) - 1u;
    return (uint)(f * maxInt + 0.5); // Round instead of truncating
}

// Packs a [0..255] integer into a [0..1] real.
real PackByte(uint i)
{
    return PackInt(i, 8);
}

// Unpacks a [0..1] real into a [0..255] integer.
uint UnpackByte(real f)
{
    return UnpackInt(f, 8);
}

// Packs a [0..65535] integer into a [0..1] real.
real PackShort(uint i)
{
    return PackInt(i, 16);
}

// Unpacks a [0..1] real into a [0..65535] integer.
uint UnpackShort(real f)
{
    return UnpackInt(f, 16);
}

// Packs 8 lowermost bits of a [0..65535] integer into a [0..1] real.
real PackShortLo(uint i)
{
    uint lo = BitFieldExtract(i, 0u, 8u);
    return PackInt(lo, 8);
}

// Packs 8 uppermost bits of a [0..65535] integer into a [0..1] real.
real PackShortHi(uint i)
{
    uint hi = BitFieldExtract(i, 8u, 8u);
    return PackInt(hi, 8);
}

real Pack2Byte(real2 inputs)
{
    real2 temp = inputs * real2(255.0, 255.0);
    temp.x *= 256.0;
    temp = round(temp);
    real combined = temp.x + temp.y;
    return combined * (1.0 / 65535.0);
}

real2 Unpack2Byte(real inputs)
{
    real temp = round(inputs * 65535.0);
    real ipart;
    real fpart = modf(temp / 256.0, ipart);
    real2 result = real2(ipart, round(256.0 * fpart));
    return result * (1.0 / real2(255.0, 255.0));
}

// Encode a real in [0..1] and an int in [0..maxi - 1] as a real [0..1] to be store in log2(precision) bit
// maxi must be a power of two and define the number of bit dedicated 0..1 to the int part (log2(maxi))
// Example: precision is 256.0, maxi is 2, i is [0..1] encode on 1 bit. f is [0..1] encode on 7 bit.
// Example: precision is 256.0, maxi is 4, i is [0..3] encode on 2 bit. f is [0..1] encode on 6 bit.
// Example: precision is 256.0, maxi is 8, i is [0..7] encode on 3 bit. f is [0..1] encode on 5 bit.
// ...
// Example: precision is 1024.0, maxi is 8, i is [0..7] encode on 3 bit. f is [0..1] encode on 7 bit.
//...
real PackFloatInt(real f, uint i, real maxi, real precision)
{
    // Constant
    real precisionMinusOne = precision - 1.0;
    real t1 = ((precision / maxi) - 1.0) / precisionMinusOne;
    real t2 = (precision / maxi) / precisionMinusOne;

    return t1 * f + t2 * real(i);
}

void UnpackFloatInt(real val, real maxi, real precision, out real f, out uint i)
{
    // Constant
    real precisionMinusOne = precision - 1.0;
    real t1 = ((precision / maxi) - 1.0) / precisionMinusOne;
    real t2 = (precision / maxi) / precisionMinusOne;

    // extract integer part
    i = int((val / t2) + rcp(precisionMinusOne)); // + rcp(precisionMinusOne) to deal with precision issue (can't use round() as val contain the floating number
    // Now that we have i, solve formula in PackFloatInt for f
    //f = (val - t2 * real(i)) / t1 => convert in mads form
    f = saturate((-t2 * real(i) + val) / t1); // Saturate in case of precision issue
}

// Define various variante for ease of read
real PackFloatInt8bit(real f, uint i, real maxi)
{
    return PackFloatInt(f, i, maxi, 256.0);
}

void UnpackFloatInt8bit(real val, real maxi, out real f, out uint i)
{
    UnpackFloatInt(val, maxi, 256.0, f, i);
}

real PackFloatInt10bit(real f, uint i, real maxi)
{
    return PackFloatInt(f, i, maxi, 1024.0);
}

void UnpackFloatInt10bit(real val, real maxi, out real f, out uint i)
{
    UnpackFloatInt(val, maxi, 1024.0, f, i);
}

real PackFloatInt16bit(real f, uint i, real maxi)
{
    return PackFloatInt(f, i, maxi, 65536.0);
}

void UnpackFloatInt16bit(real val, real maxi, out real f, out uint i)
{
    UnpackFloatInt(val, maxi, 65536.0, f, i);
}

//-----------------------------------------------------------------------------
// Float packing
//-----------------------------------------------------------------------------

// src must be between 0.0 and 1.0
uint PackFloatToUInt(real src, uint offset, uint numBits)
{
    return UnpackInt(src, numBits) << offset;
}

real UnpackUIntToFloat(uint src, uint offset, uint numBits)
{
    uint maxInt = (1u << numBits) - 1u;
    return real(BitFieldExtract(src, offset, numBits)) * rcp(maxInt);
}

uint PackToR10G10B10A2(real4 rgba)
{
    return (PackFloatToUInt(rgba.x, 0,  10) |
            PackFloatToUInt(rgba.y, 10, 10) |
            PackFloatToUInt(rgba.z, 20, 10) |
            PackFloatToUInt(rgba.w, 30, 2));
}

real4 UnpackFromR10G10B10A2(uint rgba)
{
    real4 output;
    output.x = UnpackUIntToFloat(rgba, 0,  10);
    output.y = UnpackUIntToFloat(rgba, 10, 10);
    output.z = UnpackUIntToFloat(rgba, 20, 10);
    output.w = UnpackUIntToFloat(rgba, 30, 2);
    return output;
}

// Both the input and the output are in the [0, 1] range.
real2 PackFloatToR8G8(real f)
{
    uint i = UnpackShort(f);
    return real2(PackShortLo(i), PackShortHi(i));
}

// Both the input and the output are in the [0, 1] range.
real UnpackFloatFromR8G8(real2 f)
{
    uint lo = UnpackByte(f.x);
    uint hi = UnpackByte(f.y);
    uint cb = (hi << 8) + lo;
    return PackShort(cb);
}

// Pack float2 (each of 12 bit) in 888
real3 PackFloat2To888(real2 f)
{
    uint2 i = (uint2)(f * 4095.5);
    uint2 hi = i >> 8;
    uint2 lo = i & 255;
    // 8 bit in lo, 4 bit in hi
    uint3 cb = uint3(lo, hi.x | (hi.y << 4));

    return cb / 255.0;
}

// Unpack 2 float of 12bit packed into a 888
real2 Unpack888ToFloat2(real3 x)
{
    uint3 i = (uint3)(x * 255.0);
    // 8 bit in lo, 4 bit in hi
    uint hi = i.z >> 4;
    uint lo = i.z & 15;
    uint2 cb = i.xy | uint2(lo << 8, hi << 8);

    return cb / 4095.0;
}
#endif // SHADER_API_GLES

#endif // UNITY_PACKING_INCLUDED
