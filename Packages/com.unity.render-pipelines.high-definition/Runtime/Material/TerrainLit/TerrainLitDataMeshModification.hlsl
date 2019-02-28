UNITY_INSTANCING_BUFFER_START(Terrain)
    UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)  // float4(xBase, yBase, skipScale, ~)
UNITY_INSTANCING_BUFFER_END(Terrain)

AttributesMesh ApplyMeshModification(AttributesMesh input)
{
#ifdef UNITY_INSTANCING_ENABLED
    float2 patchVertex = input.positionOS.xy;
    float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);

    float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z; // (xy + float2(xBase,yBase)) * skipScale
    float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));

    input.positionOS.xz = sampleCoords * _TerrainHeightmapScale.xz;
    input.positionOS.y = height * _TerrainHeightmapScale.y;

    #ifdef ATTRIBUTES_NEED_NORMAL
        input.normalOS = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2 - 1;
    #endif

    #if defined(VARYINGS_NEED_TEXCOORD0) || defined(VARYINGS_DS_NEED_TEXCOORD0)
        #ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
            input.uv0 = sampleCoords;
        #else
            input.uv0 = sampleCoords * _TerrainHeightmapRecipSize.zw;
        #endif
    #endif
#endif

#ifdef ATTRIBUTES_NEED_TANGENT
    // Consider a flat terrain.It should have tangent be(1, 0, 0) and bitangent be(0, 0, 1) as the UV of the terrain grid mesh is a scale of the world XZ position.
    // In CreateWorldToTangent function(in SpaceTransform.hlsl), it is cross(normal, tangent) * sgn for the bitangent vector.
    // It is not true in a left - handed coordinate system for the terrain bitangent, if we provide 1 as the tangent.w.It would produce(0, 0, -1) instead of(0, 0, 1).
    // Also terrain's tangent calculation was wrong in a left handed system because I used `cross((0,0,1), terrainNormalOS)`. It points to the wrong direction as negative X.
    // Therefore all the 4 xyzw components of the tangent needs to be flipped to correct the tangent frame.
    // (See TerrainLitData.hlsl - GetSurfaceAndBuiltinData)
    input.tangentOS.xyz = cross(input.normalOS, float3(0, 0, 1));
    input.tangentOS.w = -1;
#endif
    return input;
}

void ApplyVertexModification(AttributesMesh input, float3 normalWS, inout float3 positionWS, float4 time)
{
}
