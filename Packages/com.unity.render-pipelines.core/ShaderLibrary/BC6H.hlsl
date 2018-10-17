// Ref: https://github.com/knarkowicz/GPURealTimeBC6H/blob/master/bin/compress.hlsl
// Doc: https://msdn.microsoft.com/en-us/library/windows/desktop/hh308952(v=vs.85).aspx

// Measure compression error
float CalcMSLE(float3 a, float3 b)
{
    float3 err = log2(( b + 1.0) / (a + 1.0 ));
    err = err * err;
    return err.x + err.y + err.z;
}

// Quantification Helpers
float3 Quantize7(float3 x)
{
    return (f32tof16(x) * 128.0) / (0x7bff + 1.0);
}

float3 Quantize9(float3 x)
{
    return (f32tof16(x) * 512.0) / (0x7bff + 1.0);
}

float3 Quantize10(float3 x)
{
    return (f32tof16(x) * 1024.0) / (0x7bff + 1.0);
}

float3 Unquantize7(float3 x)
{
    return (x * 65536.0 + 0x8000) / 128.0;
}

float3 Unquantize9(float3 x)
{
    return (x * 65536.0 + 0x8000) / 512.0;
}

float3 Unquantize10(float3 x)
{
    return (x * 65536.0 + 0x8000) / 1024.0;
}

// BC6H Helpers
// Compute index of a texel projected against endpoints
uint ComputeIndex3(float texelPos, float endPoint0Pos, float endPoint1Pos )
{
    float r = ( texelPos - endPoint0Pos ) / ( endPoint1Pos - endPoint0Pos );
    return (uint) clamp( r * 6.98182f + 0.00909f + 0.5f, 0.0, 7.0 );
}

uint ComputeIndex4(float texelPos, float endPoint0Pos, float endPoint1Pos )
{
    float r = ( texelPos - endPoint0Pos ) / ( endPoint1Pos - endPoint0Pos );
    return (uint) clamp( r * 14.93333f + 0.03333f + 0.5f, 0.0, 15.0 );
}

void SignExtend(inout float3 v1, uint mask, uint signFlag )
{
    int3 v = (int3) v1;
    v.x = ( v.x & mask ) | ( v.x < 0 ? signFlag : 0 );
    v.y = ( v.y & mask ) | ( v.y < 0 ? signFlag : 0 );
    v.z = ( v.z & mask ) | ( v.z < 0 ? signFlag : 0 );
    v1 = v;
}

// 2nd step for unquantize
float3 FinishUnquantize( float3 endpoint0Unq, float3 endpoint1Unq, float weight )
{
    float3 comp = ( endpoint0Unq * ( 64.0 - weight ) + endpoint1Unq * weight + 32.0 ) * ( 31.0 / 4096.0 );
    return f16tof32( uint3( comp ) );
}

// BC6H Modes
void EncodeMode11( inout uint4 block, inout float blockMSLE, float3 texels[ 16 ] )
{
    // compute endpoints (min/max RGB bbox)
    float3 blockMin = texels[ 0 ];
    float3 blockMax = texels[ 0 ];
    uint i;
    for (i = 1; i < 16; ++i )
    {
        blockMin = min( blockMin, texels[ i ] );
        blockMax = max( blockMax, texels[ i ] );
    }

    // refine endpoints in log2 RGB space
    float3 refinedBlockMin = blockMax;
    float3 refinedBlockMax = blockMin;
    for (i = 0; i < 16; ++i )
    {
        refinedBlockMin = min( refinedBlockMin, texels[ i ] == blockMin ? refinedBlockMin : texels[ i ] );
        refinedBlockMax = max( refinedBlockMax, texels[ i ] == blockMax ? refinedBlockMax : texels[ i ] );
    }

    float3 logBlockMax          = log2( blockMax + 1.0 );
    float3 logBlockMin          = log2( blockMin + 1.0 );
    float3 logRefinedBlockMax   = log2( refinedBlockMax + 1.0 );
    float3 logRefinedBlockMin   = log2( refinedBlockMin + 1.0 );
    float3 logBlockMaxExt       = ( logBlockMax - logBlockMin ) * ( 1.0 / 32.0 );
    logBlockMin += min( logRefinedBlockMin - logBlockMin, logBlockMaxExt );
    logBlockMax -= min( logBlockMax - logRefinedBlockMax, logBlockMaxExt );
    blockMin = exp2( logBlockMin ) - 1.0;
    blockMax = exp2( logBlockMax ) - 1.0;

    float3 blockDir = blockMax - blockMin;
    blockDir = blockDir / ( blockDir.x + blockDir.y + blockDir.z );

    float3 endpoint0    = Quantize10( blockMin );
    float3 endpoint1    = Quantize10( blockMax );
    float endPoint0Pos  = f32tof16( dot( blockMin, blockDir ) );
    float endPoint1Pos  = f32tof16( dot( blockMax, blockDir ) );


    // check if endpoint swap is required
    float fixupTexelPos = f32tof16( dot( texels[ 0 ], blockDir ) );
    uint fixupIndex = ComputeIndex4( fixupTexelPos, endPoint0Pos, endPoint1Pos );
    if ( fixupIndex > 7 )
    {
        Swap( endPoint0Pos, endPoint1Pos );
        Swap( endpoint0, endpoint1 );
    }

    // compute indices
    uint indices[ 16 ] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    for (i = 0; i < 16; ++i )
    {
        float texelPos = f32tof16( dot( texels[ i ], blockDir ) );
        indices[ i ] = ComputeIndex4( texelPos, endPoint0Pos, endPoint1Pos );
    }

    // compute compression error (MSLE)
    float3 endpoint0Unq = Unquantize10( endpoint0 );
    float3 endpoint1Unq = Unquantize10( endpoint1 );
    float msle = 0.0;
    for (i = 0; i < 16; ++i )
    {
        float weight = floor( ( indices[ i ] * 64.0 ) / 15.0 + 0.5);
        float3 texelUnc = FinishUnquantize( endpoint0Unq, endpoint1Unq, weight );

        msle += CalcMSLE( texels[ i ], texelUnc );
    }


    // encode block for mode 11
    blockMSLE = msle;
    block.x = 0x03;

    // endpoints
    block.x |= (uint) endpoint0.x << 5;
    block.x |= (uint) endpoint0.y << 15;
    block.x |= (uint) endpoint0.z << 25;
    block.y |= (uint) endpoint0.z >> 7;
    block.y |= (uint) endpoint1.x << 3;
    block.y |= (uint) endpoint1.y << 13;
    block.y |= (uint) endpoint1.z << 23;
    block.z |= (uint) endpoint1.z >> 9;

    // indices
    block.z |= indices[ 0 ] << 1;
    block.z |= indices[ 1 ] << 4;
    block.z |= indices[ 2 ] << 8;
    block.z |= indices[ 3 ] << 12;
    block.z |= indices[ 4 ] << 16;
    block.z |= indices[ 5 ] << 20;
    block.z |= indices[ 6 ] << 24;
    block.z |= indices[ 7 ] << 28;
    block.w |= indices[ 8 ] << 0;
    block.w |= indices[ 9 ] << 4;
    block.w |= indices[ 10 ] << 8;
    block.w |= indices[ 11 ] << 12;
    block.w |= indices[ 12 ] << 16;
    block.w |= indices[ 13 ] << 20;
    block.w |= indices[ 14 ] << 24;
    block.w |= indices[ 15 ] << 28;
}
