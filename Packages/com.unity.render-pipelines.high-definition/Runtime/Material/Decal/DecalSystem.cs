using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class DecalSystem
    {
        public const int kInvalidIndex = -1;
        public const int kNullMaterialIndex = int.MaxValue;
        public class DecalHandle
        {
            public DecalHandle(int index, int materialID)
            {
                m_MaterialID = materialID;
                m_Index = index;
            }

            public static bool IsValid(DecalHandle handle)
            {
                if (handle == null)
                    return false;
                if (handle.m_Index == kInvalidIndex)
                    return false;
                return true;
            }

            public int m_MaterialID;    // identifies decal set
            public int m_Index;         // identifies decal within the set
        }

        static DecalSystem m_Instance;
        static public DecalSystem instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new DecalSystem();
                return m_Instance;
            }
        }

        private const int kDefaultDrawDistance = 1000;
        public int DrawDistance
        {
            get
            {
                HDRenderPipelineAsset hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                if (hdrp != null)
                {
                    return hdrp.renderPipelineSettings.decalSettings.drawDistance;
                }
                return kDefaultDrawDistance;
            }
        }

        public bool perChannelMask
        {
            get
            {
                HDRenderPipelineAsset hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                if (hdrp != null)
                {
                    return hdrp.renderPipelineSettings.decalSettings.perChannelMask;
                }
                return false;
            }
        }

        public Camera CurrentCamera
        {
            get
            {
                return m_Camera;
            }
            set
            {
                m_Camera = value;
            }
        }

        private static MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        private const int kDecalBlockSize = 128;

        // to work on Vulkan Mobile?
        // Core\CoreRP\ShaderLibrary\UnityInstancing.hlsl
        // #if defined(SHADER_API_VULKAN) && defined(SHADER_API_MOBILE)
        //      #define UNITY_INSTANCED_ARRAY_SIZE  250
        private const int kDrawIndexedBatchSize = 250;

        // cube mesh bounds for decal
        static Vector4 kMin = new Vector4(-0.5f, -0.5f, -0.5f, 1.0f);
        static Vector4 kMax = new Vector4(0.5f,  0.5f,  0.5f, 1.0f);

        static public Mesh m_DecalMesh = null;

        // clustered draw data
        static public DecalData[] m_DecalDatas = new DecalData[kDecalBlockSize];
        static public SFiniteLightBound[] m_Bounds = new SFiniteLightBound[kDecalBlockSize];
        static public LightVolumeData[] m_LightVolumes = new LightVolumeData[kDecalBlockSize];
        static public TextureScaleBias[] m_DiffuseTextureScaleBias = new TextureScaleBias[kDecalBlockSize];
        static public TextureScaleBias[] m_NormalTextureScaleBias = new TextureScaleBias[kDecalBlockSize];
        static public TextureScaleBias[] m_MaskTextureScaleBias = new TextureScaleBias[kDecalBlockSize];
        static public Vector4[] m_BaseColor = new Vector4[kDecalBlockSize];

        static public int m_DecalDatasCount = 0;

        static public float[] m_BoundingDistances = new float[1];

        private Dictionary<int, DecalSet> m_DecalSets = new Dictionary<int, DecalSet>();
        private List<DecalSet> m_DecalSetsRenderList = new List<DecalSet>(); // list of visible decalsets sorted by material draw order

        // current camera
        private Camera m_Camera;

        static public int m_DecalsVisibleThisFrame = 0;

        private Texture2DAtlas m_Atlas = null;
        public bool m_AllocationSuccess = true;
        public bool m_PrevAllocationSuccess = true;

        public Texture2DAtlas Atlas
        {
            get
            {
                if (m_Atlas == null)
                {
                    m_Atlas = new Texture2DAtlas(HDUtils.hdrpSettings.decalSettings.atlasWidth, HDUtils.hdrpSettings.decalSettings.atlasHeight, RenderTextureFormat.ARGB32);
                }
                return m_Atlas;
            }
        }

        public class TextureScaleBias : IComparable
        {
            public Texture m_Texture = null;
            public Vector4 m_ScaleBias = Vector4.zero;
            public int CompareTo(object obj)
            {
                TextureScaleBias other = obj as TextureScaleBias;
                int size = m_Texture.width * m_Texture.height;
                int otherSize = other.m_Texture.width * other.m_Texture.height;
                if (size > otherSize)
                {
                    return -1;
                }
                else if (size < otherSize)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }

            public void Initialize(Texture texture, Vector4 scaleBias)
            {
                m_Texture = texture;
                m_ScaleBias = scaleBias;
            }
        }

        private List<TextureScaleBias> m_TextureList = new List<TextureScaleBias>();

        private class DecalSet
        {
            public void InitializeMaterialValues()
            {
                if (m_Material == null)
                    return;
                m_Diffuse.Initialize(m_Material.GetTexture("_BaseColorMap"), Vector4.zero); 
                m_Normal.Initialize(m_Material.GetTexture("_NormalMap"), Vector4.zero); 
                m_Mask.Initialize(m_Material.GetTexture("_MaskMap"), Vector4.zero);
                m_Blend = m_Material.GetFloat("_DecalBlend");
                m_AlbedoContribution = m_Material.GetFloat("_AlbedoMode");
                m_BaseColor = m_Material.GetVector("_BaseColor");
                m_BlendParams = new Vector3(m_Material.GetFloat("_NormalBlendSrc"), m_Material.GetFloat("_MaskBlendSrc"), m_Material.GetFloat("_MaskBlendMode"));                
            }

            public DecalSet(Material material)
            {
                m_Material = material;              
                InitializeMaterialValues();
            }


            private BoundingSphere GetDecalProjectBoundingSphere(Matrix4x4 decalToWorld)
            {
                Vector4 min = new Vector4();
                Vector4 max = new Vector4();
                min = decalToWorld * kMin;
                max = decalToWorld * kMax;
                BoundingSphere res = new BoundingSphere();
                res.position = (max + min) / 2;
                res.radius = ((Vector3)(max - min)).magnitude / 2;
                return res;
            }

            public void UpdateCachedData(Matrix4x4 localToWorld, Quaternion rotation, Matrix4x4 sizeOffset, float drawDistance, float fadeScale, Vector4 uvScaleBias, bool affectsTransparency, DecalHandle handle)
            {
                int index = handle.m_Index;
                m_CachedDecalToWorld[index] = localToWorld * sizeOffset;
                Matrix4x4 decalRotation = Matrix4x4.Rotate(rotation);

                // z/y axis swap for normal to decal space, Unity is column major
                float y0 = decalRotation.m01;
                float y1 = decalRotation.m11;
                float y2 = decalRotation.m21;
                decalRotation.m01 = decalRotation.m02;
                decalRotation.m11 = decalRotation.m12;
                decalRotation.m21 = decalRotation.m22;
                decalRotation.m02 = y0;
                decalRotation.m12 = y1;
                decalRotation.m22 = y2;

                m_CachedNormalToWorld[index] = decalRotation;
                // draw distance can't be more than global draw distance
                m_CachedDrawDistances[index].x = drawDistance < instance.DrawDistance
                    ? drawDistance
                    : instance.DrawDistance;
                m_CachedDrawDistances[index].y = fadeScale;
                m_CachedUVScaleBias[index] = uvScaleBias;
                m_CachedAffectsTransparency[index] = affectsTransparency;
                m_BoundingSpheres[index] = GetDecalProjectBoundingSphere(m_CachedDecalToWorld[index]);
            }

            public void UpdateCachedData(Transform transform, Matrix4x4 sizeOffset, float drawDistance, float fadeScale, Vector4 uvScaleBias, bool affectsTransparency, DecalHandle handle)
            {
                if (m_Material == null)
                    return;
                UpdateCachedData(transform.localToWorldMatrix, transform.rotation, sizeOffset, drawDistance, fadeScale, uvScaleBias, affectsTransparency, handle);                
            }

            public DecalHandle AddDecal(Matrix4x4 localToWorld, Quaternion rotation, Matrix4x4 sizeOffset, float drawDistance, float fadeScale, Vector4 uvScaleBias, bool affectsTransparency, int materialID)
            {
                // increase array size if no space left
                if (m_DecalsCount == m_Handles.Length)
                {
                    DecalHandle[] newHandles = new DecalHandle[m_DecalsCount + kDecalBlockSize];
                    BoundingSphere[] newSpheres = new BoundingSphere[m_DecalsCount + kDecalBlockSize];
                    Matrix4x4[] newCachedTransforms = new Matrix4x4[m_DecalsCount + kDecalBlockSize];
                    Matrix4x4[] newCachedNormalToWorld = new Matrix4x4[m_DecalsCount + kDecalBlockSize];
                    Vector2[] newCachedDrawDistances = new Vector2[m_DecalsCount + kDecalBlockSize];
                    Vector4[] newCachedUVScaleBias = new Vector4[m_DecalsCount + kDecalBlockSize];
                    bool[] newCachedAffectsTransparency = new bool[m_DecalsCount + kDecalBlockSize];
                    m_ResultIndices = new int[m_DecalsCount + kDecalBlockSize];

                    m_Handles.CopyTo(newHandles, 0);
                    m_BoundingSpheres.CopyTo(newSpheres, 0);
                    m_CachedDecalToWorld.CopyTo(newCachedTransforms, 0);
                    m_CachedNormalToWorld.CopyTo(newCachedNormalToWorld, 0);
                    m_CachedDrawDistances.CopyTo(newCachedDrawDistances, 0);
                    m_CachedUVScaleBias.CopyTo(newCachedUVScaleBias, 0);
                    m_CachedAffectsTransparency.CopyTo(newCachedAffectsTransparency, 0);

                    m_Handles = newHandles;
                    m_BoundingSpheres = newSpheres;
                    m_CachedDecalToWorld = newCachedTransforms;
                    m_CachedNormalToWorld = newCachedNormalToWorld;
                    m_CachedDrawDistances = newCachedDrawDistances;
                    m_CachedUVScaleBias = newCachedUVScaleBias;
                    m_CachedAffectsTransparency = newCachedAffectsTransparency;
                }

                DecalHandle decalHandle = new DecalHandle(m_DecalsCount, materialID);
                m_Handles[m_DecalsCount] = decalHandle;
                UpdateCachedData(localToWorld, rotation, sizeOffset, drawDistance, fadeScale, uvScaleBias, affectsTransparency, decalHandle);
                m_DecalsCount++;
                return decalHandle;
            }

            public void RemoveDecal(DecalHandle handle)
            {
                int removeAtIndex = handle.m_Index;
                // replace with last decal in the list and update index
                m_Handles[removeAtIndex] = m_Handles[m_DecalsCount - 1]; // move the last decal in list
                m_Handles[removeAtIndex].m_Index = removeAtIndex;
                m_Handles[m_DecalsCount - 1] = null;

                // update cached data
                m_BoundingSpheres[removeAtIndex] = m_BoundingSpheres[m_DecalsCount - 1];
                m_CachedDecalToWorld[removeAtIndex] = m_CachedDecalToWorld[m_DecalsCount - 1];
                m_CachedNormalToWorld[removeAtIndex] = m_CachedNormalToWorld[m_DecalsCount - 1];
                m_CachedDrawDistances[removeAtIndex] = m_CachedDrawDistances[m_DecalsCount - 1];
                m_CachedUVScaleBias[removeAtIndex] = m_CachedUVScaleBias[m_DecalsCount - 1];
                m_CachedAffectsTransparency[removeAtIndex] = m_CachedAffectsTransparency[m_DecalsCount - 1];
                m_DecalsCount--;
                handle.m_Index = kInvalidIndex;
            }

            public void BeginCull()
            {
                if (m_Material == null)
                    return;
                if (m_CullingGroup != null)
                {
                    Debug.LogError("Begin/EndCull() called out of sequence for decal projectors.");
                }

                // let the culling group code do some of the heavy lifting for global draw distance
                m_BoundingDistances[0] = DecalSystem.instance.DrawDistance;
                m_NumResults = 0;
                m_CullingGroup = CullingGroupManager.instance.Alloc();
                m_CullingGroup.targetCamera = instance.CurrentCamera;
                m_CullingGroup.SetDistanceReferencePoint(m_CullingGroup.targetCamera.transform.position);
                m_CullingGroup.SetBoundingDistances(m_BoundingDistances);
                m_CullingGroup.SetBoundingSpheres(m_BoundingSpheres);
                m_CullingGroup.SetBoundingSphereCount(m_DecalsCount);
            }

            public int QueryCullResults()
            {
                if (m_Material == null)
                    return 0;
                m_NumResults = m_CullingGroup.QueryIndices(true, m_ResultIndices, 0);
                return m_NumResults;
            }

            private void GetDecalVolumeDataAndBound(Matrix4x4 decalToWorld, Matrix4x4 worldToView)
            {
                var influenceX = decalToWorld.GetColumn(0) * 0.5f;
                var influenceY = decalToWorld.GetColumn(1) * 0.5f;
                var influenceZ = decalToWorld.GetColumn(2) * 0.5f;
                var pos = decalToWorld.GetColumn(3);

                Vector3 influenceExtents = new Vector3();
                influenceExtents.x = influenceX.magnitude;
                influenceExtents.y = influenceY.magnitude;
                influenceExtents.z = influenceZ.magnitude;

                // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                var influenceRightVS = worldToView.MultiplyVector(influenceX / influenceExtents.x);
                var influenceUpVS = worldToView.MultiplyVector(influenceY / influenceExtents.y);
                var influenceForwardVS = worldToView.MultiplyVector(influenceZ / influenceExtents.z);
                var influencePositionVS = worldToView.MultiplyPoint(pos); // place the mesh pivot in the center

                m_Bounds[m_DecalDatasCount].center = influencePositionVS;
                m_Bounds[m_DecalDatasCount].boxAxisX = influenceRightVS * influenceExtents.x;
                m_Bounds[m_DecalDatasCount].boxAxisY = influenceUpVS * influenceExtents.y;
                m_Bounds[m_DecalDatasCount].boxAxisZ = influenceForwardVS * influenceExtents.z;
                m_Bounds[m_DecalDatasCount].scaleXY.Set(1.0f, 1.0f);
                m_Bounds[m_DecalDatasCount].radius = influenceExtents.magnitude;

                // The culling system culls pixels that are further
                //   than a threshold to the box influence extents.
                // So we use an arbitrary threshold here (k_BoxCullingExtentOffset)
                m_LightVolumes[m_DecalDatasCount].lightCategory = (uint)LightCategory.Decal;
                m_LightVolumes[m_DecalDatasCount].lightVolume = (uint)LightVolumeType.Box;
                m_LightVolumes[m_DecalDatasCount].featureFlags = (uint)LightFeatureFlags.Env;
                m_LightVolumes[m_DecalDatasCount].lightPos = influencePositionVS;
                m_LightVolumes[m_DecalDatasCount].lightAxisX = influenceRightVS;
                m_LightVolumes[m_DecalDatasCount].lightAxisY = influenceUpVS;
                m_LightVolumes[m_DecalDatasCount].lightAxisZ = influenceForwardVS;
                m_LightVolumes[m_DecalDatasCount].boxInnerDist = influenceExtents - LightLoop.k_BoxCullingExtentThreshold;
                m_LightVolumes[m_DecalDatasCount].boxInvRange.Set(1.0f / LightLoop.k_BoxCullingExtentThreshold.x, 1.0f / LightLoop.k_BoxCullingExtentThreshold.y, 1.0f / LightLoop.k_BoxCullingExtentThreshold.z);
            }

            private void AssignCurrentBatches(ref Matrix4x4[] decalToWorldBatch, ref Matrix4x4[] normalToWorldBatch, int batchCount)
            {
                if (m_DecalToWorld.Count == batchCount)
                {
                    decalToWorldBatch = new Matrix4x4[kDrawIndexedBatchSize];
                    m_DecalToWorld.Add(decalToWorldBatch);
                    normalToWorldBatch = new Matrix4x4[kDrawIndexedBatchSize];
                    m_NormalToWorld.Add(normalToWorldBatch);
                }
                else
                {
                    decalToWorldBatch = m_DecalToWorld[batchCount];
                    normalToWorldBatch = m_NormalToWorld[batchCount];
                }
            }

            public bool IsDrawn()
            {
                return ((m_Material != null) && (m_NumResults > 0));
            }

            public void CreateDrawData()
            {

                int instanceCount = 0;
                int batchCount = 0;
                Matrix4x4[] decalToWorldBatch = null;
                Matrix4x4[] normalToWorldBatch = null;
                bool anyAffectTransparency = false;

                AssignCurrentBatches(ref decalToWorldBatch, ref normalToWorldBatch, batchCount);

                Vector3 cameraPos = instance.CurrentCamera.transform.position;
                Matrix4x4 worldToView = LightLoop.WorldToCamera(instance.CurrentCamera);
                bool perChannelMask = instance.perChannelMask;
                for (int resultIndex = 0; resultIndex < m_NumResults; resultIndex++)
                {
                    int decalIndex = m_ResultIndices[resultIndex];
                    // do additional culling based on individual decal draw distances
                    float distanceToDecal = (cameraPos - m_BoundingSpheres[decalIndex].position).magnitude;
                    float cullDistance = m_CachedDrawDistances[decalIndex].x + m_BoundingSpheres[decalIndex].radius;
                    if (distanceToDecal < cullDistance)
                    {
                        // d-buffer data
                        decalToWorldBatch[instanceCount] = m_CachedDecalToWorld[decalIndex];
                        normalToWorldBatch[instanceCount] = m_CachedNormalToWorld[decalIndex];
                        float fadeFactor = Mathf.Clamp((cullDistance - distanceToDecal) / (cullDistance * (1.0f - m_CachedDrawDistances[decalIndex].y)), 0.0f, 1.0f);
                        normalToWorldBatch[instanceCount].m03 = fadeFactor * m_Blend;   // vector3 rotation matrix so bottom row and last column can be used for other data to save space
                        normalToWorldBatch[instanceCount].m13 = m_AlbedoContribution;
                        normalToWorldBatch[instanceCount].SetRow(3, m_CachedUVScaleBias[decalIndex]);

                        // clustered forward data
                        if(m_CachedAffectsTransparency[decalIndex])
                        { 
                            m_DecalDatas[m_DecalDatasCount].worldToDecal = decalToWorldBatch[instanceCount].inverse;
                            m_DecalDatas[m_DecalDatasCount].normalToWorld = normalToWorldBatch[instanceCount];
                            m_DecalDatas[m_DecalDatasCount].baseColor = m_BaseColor;
                            m_DecalDatas[m_DecalDatasCount].blendParams = m_BlendParams;
                            if(!perChannelMask)
                            {
                                m_DecalDatas[m_DecalDatasCount].blendParams.z = (float)Decal.MaskBlendFlags.Smoothness; 
                            }
                                                        
                            // we have not allocated the textures in atlas yet, so only store references to them
                            m_DiffuseTextureScaleBias[m_DecalDatasCount] = m_Diffuse;
                            m_NormalTextureScaleBias[m_DecalDatasCount] = m_Normal;
                            m_MaskTextureScaleBias[m_DecalDatasCount] = m_Mask;

                            GetDecalVolumeDataAndBound(decalToWorldBatch[instanceCount], worldToView);
                            m_DecalDatasCount++;
                            anyAffectTransparency = true;
                        }

                        instanceCount++;
                        if (instanceCount == kDrawIndexedBatchSize)
                        {
                            instanceCount = 0;
                            batchCount++;
                            AssignCurrentBatches(ref decalToWorldBatch, ref normalToWorldBatch, batchCount);
                        }
                    }
                }

                // only add if any projectors in this decal set affect transparency, doesn't actually allocate textures in the atlas yet, this is because we want all the textures in the list so we can optimize the packing
                if( anyAffectTransparency)
                { 
                    AddToTextureList(ref instance.m_TextureList);
                }
            }

            public void EndCull()
            {
                if (m_Material == null)
                    return;
                if (m_CullingGroup == null)
                {
                    Debug.LogError("Begin/EndCull() called out of sequence for decal projectors.");
                }
                else
                {
                    CullingGroupManager.instance.Free(m_CullingGroup);
                    m_CullingGroup = null;
                }
            }

            public void AddToTextureList(ref List<TextureScaleBias> textureList)
            {
                if (m_Diffuse.m_Texture != null)
                {
                    textureList.Add(m_Diffuse);
                }
                if (m_Normal.m_Texture != null)
                {
                    textureList.Add(m_Normal);
                }
                if (m_Mask.m_Texture != null)
                {
                    textureList.Add(m_Mask);
                }
            }

            public void RenderIntoDBuffer(CommandBuffer cmd)
            {
                if (m_Material == null)
                    return;
                if (m_NumResults == 0)
                    return;
                int batchIndex = 0;
                int totalToDraw = m_NumResults;
                HDRenderPipelineAsset hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                bool perChannelMask = hdrp.renderPipelineSettings.decalSettings.perChannelMask;
                int shaderPass = perChannelMask ? (int)m_Material.GetFloat("_MaskBlendMode") : (int)Decal.MaskBlendFlags.Smoothness; // relies on the order shader passes are declared in decal.shader and decalUI.cs
              
                for (; batchIndex < m_NumResults / kDrawIndexedBatchSize; batchIndex++)
                {
                    m_PropertyBlock.SetMatrixArray(HDShaderIDs._NormalToWorldID, m_NormalToWorld[batchIndex]);
                    cmd.DrawMeshInstanced(m_DecalMesh, 0, m_Material, shaderPass, m_DecalToWorld[batchIndex], kDrawIndexedBatchSize, m_PropertyBlock);
                    totalToDraw -= kDrawIndexedBatchSize;
                }

                if (totalToDraw > 0)
                {
                    m_PropertyBlock.SetMatrixArray(HDShaderIDs._NormalToWorldID, m_NormalToWorld[batchIndex]);
                    cmd.DrawMeshInstanced(m_DecalMesh, 0, m_Material, shaderPass, m_DecalToWorld[batchIndex], totalToDraw, m_PropertyBlock);
                }
            }

            public void Cleanup()
            {
                if(m_CullingGroup != null)
                {
                    CullingGroupManager.instance.Free(m_CullingGroup);
                }
            }

            public Material KeyMaterial
            {
                get
                {
                    return this.m_Material;
                }
            }

            public int Count
            {
                get
                {
                    return this.m_DecalsCount;
                }
            }

            public int DrawOrder
            {
                get
                {
                    return this.m_Material.GetInt("_DrawOrder");
                }
            }

            private List<Matrix4x4[]> m_DecalToWorld = new List<Matrix4x4[]>();
            private List<Matrix4x4[]> m_NormalToWorld = new List<Matrix4x4[]>();

            private CullingGroup m_CullingGroup = null;
            private BoundingSphere[] m_BoundingSpheres = new BoundingSphere[kDecalBlockSize];
            private DecalHandle[] m_Handles = new DecalHandle[kDecalBlockSize];
            private int[] m_ResultIndices = new int[kDecalBlockSize];
            private int m_NumResults = 0;
            private int m_DecalsCount = 0;
            private Matrix4x4[] m_CachedDecalToWorld = new Matrix4x4[kDecalBlockSize];
            private Matrix4x4[] m_CachedNormalToWorld = new Matrix4x4[kDecalBlockSize];
            private Vector2[] m_CachedDrawDistances = new Vector2[kDecalBlockSize]; // x - draw distance, y - fade scale
            private Vector4[] m_CachedUVScaleBias = new Vector4[kDecalBlockSize]; // xy - scale, zw bias
            private bool[] m_CachedAffectsTransparency = new bool[kDecalBlockSize];
            private Material m_Material;
            private float m_Blend = 0;
            private float m_AlbedoContribution = 0;
            private Vector4 m_BaseColor;
            private Vector3 m_BlendParams;
            
            TextureScaleBias m_Diffuse = new TextureScaleBias();
            TextureScaleBias m_Normal = new TextureScaleBias();
            TextureScaleBias m_Mask = new TextureScaleBias();
        }

        DecalHandle AddDecal(Matrix4x4 localToWorld, Quaternion rotation, Matrix4x4 sizeOffset, float drawDistance, float fadeScale, Vector4 uvScaleBias, bool affectsTransparency, Material material)
        {
            DecalSet decalSet = null;
            int key = material != null ? material.GetInstanceID() : kNullMaterialIndex;
            if (!m_DecalSets.TryGetValue(key, out decalSet))
            {
                decalSet = new DecalSet(material);
                m_DecalSets.Add(key, decalSet);
            }
            return decalSet.AddDecal(localToWorld, rotation, sizeOffset, drawDistance, fadeScale, uvScaleBias, affectsTransparency, key);
        }


        public DecalHandle AddDecal(Vector3 position, Quaternion rotation, Vector3 scale, Matrix4x4 sizeOffset, float drawDistance, float fadeScale, Vector4 uvScaleBias, bool affectsTransparency, Material material)
        {
            return AddDecal(Matrix4x4.TRS(position, rotation, scale), rotation, sizeOffset, drawDistance, fadeScale, uvScaleBias, affectsTransparency, material);
        }

        public DecalHandle AddDecal(Transform transform, Matrix4x4 sizeOffset, float drawDistance, float fadeScale, Vector4 uvScaleBias, bool affectsTransparency, Material material)
        {
            return AddDecal(transform.localToWorldMatrix, transform.rotation, sizeOffset, drawDistance, fadeScale, uvScaleBias, affectsTransparency, material);
        }

        public void RemoveDecal(DecalHandle handle)
        {
            if (!DecalHandle.IsValid(handle))
                return;

            DecalSet decalSet = null;
            int key = handle.m_MaterialID;
            if (m_DecalSets.TryGetValue(key, out decalSet))
            {
                decalSet.RemoveDecal(handle);
                if (decalSet.Count == 0)
                {
                    m_DecalSets.Remove(key);
                }
            }
        }

        void UpdateCachedData(Matrix4x4 localToWorld, Quaternion rotation, Matrix4x4 sizeOffset, float drawDistance, float fadeScale, Vector4 uvScaleBias, bool affectsTransparency, DecalHandle handle)
        {
            if (!DecalHandle.IsValid(handle))
                return;

            DecalSet decalSet = null;
            int key = handle.m_MaterialID;
            if (m_DecalSets.TryGetValue(key, out decalSet))
            {
                decalSet.UpdateCachedData(localToWorld, rotation, sizeOffset, drawDistance, fadeScale, uvScaleBias, affectsTransparency, handle);
            }
        }

        public void UpdateCachedData(Vector3 position, Quaternion rotation, Vector3 scale, Matrix4x4 sizeOffset, float drawDistance, float fadeScale, Vector4 uvScaleBias, bool affectsTransparency, DecalHandle handle)
        {
             UpdateCachedData(Matrix4x4.TRS(position,  rotation, scale), rotation, sizeOffset, drawDistance, fadeScale, uvScaleBias, affectsTransparency, handle);
        }

        public void UpdateCachedData(Transform transform, Matrix4x4 sizeOffset, float drawDistance, float fadeScale, Vector4 uvScaleBias, bool affectsTransparency, DecalHandle handle)
        {
            UpdateCachedData(transform.localToWorldMatrix, transform.rotation, sizeOffset, drawDistance, fadeScale, uvScaleBias, affectsTransparency, handle);
        }

        public void BeginCull()
        {
            foreach (var pair in m_DecalSets)
            {
                pair.Value.BeginCull();
            }
        }

        private int QueryCullResults()
        {
            int totalVisibleDecals = 0;
            foreach (var pair in m_DecalSets)
            {
                totalVisibleDecals += pair.Value.QueryCullResults();
            }
            return totalVisibleDecals;
        }

        public void EndCull()
        {
            m_DecalsVisibleThisFrame = QueryCullResults();
            foreach (var pair in m_DecalSets)
            {
                pair.Value.EndCull();
            }
        }

        public void RenderIntoDBuffer(CommandBuffer cmd)
        {
            if (m_DecalMesh == null)
                m_DecalMesh = CoreUtils.CreateCubeMesh(kMin, kMax);

            foreach (var decalSet in m_DecalSetsRenderList)
            {
                decalSet.RenderIntoDBuffer(cmd);
            }
        }

        public void SetAtlas(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._DecalAtlas2DID, Atlas.AtlasTexture);
        }

        public void AddTexture(CommandBuffer cmd, TextureScaleBias textureScaleBias)
        {
            if (textureScaleBias.m_Texture != null)
            {
                if (!Atlas.AddTexture(cmd, ref textureScaleBias.m_ScaleBias, textureScaleBias.m_Texture))
                {
                    m_AllocationSuccess = false;
                }
            }
            else
            {
                textureScaleBias.m_ScaleBias = Vector4.zero;
            }
        }

        // updates textures, texture atlas indices and blend value
        public void UpdateCachedMaterialData()
        {
            m_TextureList.Clear();
            foreach (var pair in m_DecalSets)
            {
                pair.Value.InitializeMaterialValues();
            }
        }

        private void UpdateDecalDatasWithAtlasInfo()
        {
            for (int decalDataIndex = 0; decalDataIndex < m_DecalDatasCount; decalDataIndex++)
            {
                m_DecalDatas[decalDataIndex].diffuseScaleBias = m_DiffuseTextureScaleBias[decalDataIndex].m_ScaleBias;
                m_DecalDatas[decalDataIndex].normalScaleBias = m_NormalTextureScaleBias[decalDataIndex].m_ScaleBias;
                m_DecalDatas[decalDataIndex].maskScaleBias = m_MaskTextureScaleBias[decalDataIndex].m_ScaleBias;
            }
        }

        public void UpdateTextureAtlas(CommandBuffer cmd)
        {
            m_AllocationSuccess = true;
            foreach (TextureScaleBias textureScaleBias in m_TextureList)
            {
                AddTexture(cmd, textureScaleBias);
            }

            if (!m_AllocationSuccess) // texture failed to find space in the atlas
            {
                m_TextureList.Sort();   // sort the texture list largest to smallest for better packing
                Atlas.ResetAllocator(); // clear all allocations
                // try again
                m_AllocationSuccess = true;
                foreach (TextureScaleBias textureScaleBias in m_TextureList)
                {
                    AddTexture(cmd, textureScaleBias);
                }

                if (!m_AllocationSuccess && m_PrevAllocationSuccess) // still failed to allocate, decal atlas size needs to increase, debounce so that we don't spam the console with warnings
                {
                    Debug.LogWarning("Decal texture atlas out of space, decals on transparent geometry might not render correctly, atlas size can be changed in HDRenderPipelineAsset");
                }
            }
            m_PrevAllocationSuccess = m_AllocationSuccess;
            // now that textures have been stored in the atlas we can update their location info in decal data
            UpdateDecalDatasWithAtlasInfo();
        }

     
        public void CreateDrawData()
        {
            m_DecalDatasCount = 0;
            // reallocate if needed
            if (m_DecalsVisibleThisFrame > m_DecalDatas.Length)
            {
                int newDecalDatasSize = ((m_DecalsVisibleThisFrame + kDecalBlockSize - 1) / kDecalBlockSize) * kDecalBlockSize;
                m_DecalDatas = new DecalData[newDecalDatasSize];
                m_Bounds = new SFiniteLightBound[newDecalDatasSize];
                m_LightVolumes = new LightVolumeData[newDecalDatasSize];
                m_DiffuseTextureScaleBias = new TextureScaleBias[newDecalDatasSize];
                m_NormalTextureScaleBias = new TextureScaleBias[newDecalDatasSize];
                m_MaskTextureScaleBias = new TextureScaleBias[newDecalDatasSize];
                m_BaseColor = new Vector4[newDecalDatasSize];
            }

            // add any visible decals according to material draw order, avoid using List.Sort() because it uses quicksort, which is an unstable sort.
            m_DecalSetsRenderList.Clear();
            foreach (var pair in m_DecalSets)
            {
                if (pair.Value.IsDrawn())
                {
                    int insertIndex = 0;
                    while((insertIndex < m_DecalSetsRenderList.Count) && (pair.Value.DrawOrder >= m_DecalSetsRenderList[insertIndex].DrawOrder))
                    {
                        insertIndex++;
                    }
                    m_DecalSetsRenderList.Insert(insertIndex, pair.Value);
                }
            }

            foreach(var decalSet in m_DecalSetsRenderList)
            {
                decalSet.CreateDrawData();
            }
        }

        public void Cleanup()
        {
            if (m_Atlas != null)
                m_Atlas.Release();
            foreach (var pair in m_DecalSets)
            {
                pair.Value.Cleanup();
            }
            CoreUtils.Destroy(m_DecalMesh);
            // set to null so that they get recreated
            m_DecalMesh = null;
            m_Atlas = null;
        }

        public void RenderDebugOverlay(HDCamera hdCamera, CommandBuffer cmd, DebugDisplaySettings debugDisplaySettings, ref float x, ref float y, float overlaySize, float width)
        {
            if (debugDisplaySettings.decalsDebugSettings.m_DisplayAtlas)
            {
                using (new ProfilingSample(cmd, "Display Decal Atlas", CustomSamplerId.DisplayDebugDecalsAtlas.GetSampler()))
                {
                    cmd.SetViewport(new Rect(x, y, overlaySize, overlaySize));
                    HDUtils.BlitQuad(cmd, Atlas.AtlasTexture, new Vector4(1, 1, 0 , 0), new Vector4(1, 1, 0, 0), (int)debugDisplaySettings.decalsDebugSettings.m_MipLevel, true);
                    HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, hdCamera.actualWidth);
                }
            }
        }
    }
}
