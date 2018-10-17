#define DBufferType0 float4
#define DBufferType1 float4
#define DBufferType2 float4

#ifdef DECALS_4RT
#define DBufferType3 float2

#define OUTPUT_DBUFFER(NAME)							\
	out DBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,	\
	out DBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,	\
	out DBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,	\
	out DBufferType3 MERGE_NAME(NAME, 3) : SV_Target3

#define DECLARE_DBUFFER_TEXTURE(NAME)	\
	TEXTURE2D(MERGE_NAME(NAME, 0));		\
	TEXTURE2D(MERGE_NAME(NAME, 1));		\
	TEXTURE2D(MERGE_NAME(NAME, 2));		\
	TEXTURE2D(MERGE_NAME(NAME, 3));

#define FETCH_DBUFFER(NAME, TEX, unCoord2)												\
	DBufferType0 MERGE_NAME(NAME, 0) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 0), unCoord2);	\
	DBufferType1 MERGE_NAME(NAME, 1) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 1), unCoord2);	\
	DBufferType2 MERGE_NAME(NAME, 2) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 2), unCoord2);	\
	DBufferType3 MERGE_NAME(NAME, 3) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 3), unCoord2).xy; // Caution: We do .xy to match #define DBufferType3 float2 and avoid a shader warning

#define ENCODE_INTO_DBUFFER(DECAL_SURFACE_DATA, NAME) EncodeIntoDBuffer(DECAL_SURFACE_DATA, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2), MERGE_NAME(NAME,3))
#define DECODE_FROM_DBUFFER(NAME, DECAL_SURFACE_DATA) DecodeFromDBuffer(MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2), MERGE_NAME(NAME,3), DECAL_SURFACE_DATA)

#else

#define OUTPUT_DBUFFER(NAME)							\
	out DBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,	\
	out DBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,	\
	out DBufferType2 MERGE_NAME(NAME, 2) : SV_Target2

#define DECLARE_DBUFFER_TEXTURE(NAME)	\
	TEXTURE2D(MERGE_NAME(NAME, 0));		\
	TEXTURE2D(MERGE_NAME(NAME, 1));		\
	TEXTURE2D(MERGE_NAME(NAME, 2));

#define FETCH_DBUFFER(NAME, TEX, unCoord2)												\
	DBufferType0 MERGE_NAME(NAME, 0) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 0), unCoord2);	\
	DBufferType1 MERGE_NAME(NAME, 1) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 1), unCoord2);	\
	DBufferType2 MERGE_NAME(NAME, 2) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 2), unCoord2);

#define ENCODE_INTO_DBUFFER(DECAL_SURFACE_DATA, NAME) EncodeIntoDBuffer(DECAL_SURFACE_DATA, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2))
#define DECODE_FROM_DBUFFER(NAME, DECAL_SURFACE_DATA) DecodeFromDBuffer(MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2), DECAL_SURFACE_DATA)

#endif

// Must be in sync with RT declared in HDRenderPipeline.cs ::Rebuild
void EncodeIntoDBuffer( DecalSurfaceData surfaceData
                        , out DBufferType0 outDBuffer0
                        , out DBufferType1 outDBuffer1
                        , out DBufferType2 outDBuffer2
#ifdef DECALS_4RT
						, out DBufferType3 outDBuffer3
#endif
                        )
{
    outDBuffer0 = surfaceData.baseColor;
    outDBuffer1 = surfaceData.normalWS;
    outDBuffer2 = surfaceData.mask;
#ifdef DECALS_4RT
	outDBuffer3 = surfaceData.MAOSBlend;
#endif
}

void DecodeFromDBuffer(
    DBufferType0 inDBuffer0
    , DBufferType1 inDBuffer1
    , DBufferType2 inDBuffer2
#ifdef DECALS_4RT
	, DBufferType3 inDBuffer3
#endif
    , out DecalSurfaceData surfaceData
)
{
    ZERO_INITIALIZE(DecalSurfaceData, surfaceData);
    surfaceData.baseColor = inDBuffer0;
    surfaceData.normalWS.xyz = inDBuffer1.xyz * 2.0 - 1.0;
    surfaceData.normalWS.w = inDBuffer1.w;
    surfaceData.mask = inDBuffer2;
#ifdef DECALS_4RT
	surfaceData.MAOSBlend = inDBuffer3;
#else
	surfaceData.MAOSBlend = float2(surfaceData.mask.w, surfaceData.mask.w);
#endif
}
