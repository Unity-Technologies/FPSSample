using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class DebugLightVolumes
    {
        // Render target that holds the light count in floating points
        RTHandleSystem.RTHandle m_LightCountBuffer = null;
        // Render target that holds the color accumulated value
        RTHandleSystem.RTHandle m_ColorAccumulationBuffer = null;
        // The output texture of the debug
        RTHandleSystem.RTHandle m_DebugLightVolumesTexture = null;
        // Required depth texture given that we render multiple render targets
        RTHandleSystem.RTHandle m_DepthBuffer = null;

        // Material used to blit the output texture into the camera render target
        Material m_Blit;
        // Material used to render the light volumes
        Material m_DebugLightVolumeMaterial;
        // Material to resolve the light volume textures
        ComputeShader m_DebugLightVolumeCompute;
        int m_DebugLightVolumeGradientKernel;
        int m_DebugLightVolumeColorsKernel;

        // Texture used to display the gradient
        Texture2D m_ColorGradientTexture = null;

        // Shader property ids
        public static readonly int _ColorShaderID = Shader.PropertyToID("_Color");
        public static readonly int _OffsetShaderID = Shader.PropertyToID("_Offset");
        public static readonly int _RangeShaderID = Shader.PropertyToID("_Range");
        public static readonly int _DebugLightCountBufferShaderID = Shader.PropertyToID("_DebugLightCountBuffer");
        public static readonly int _DebugColorAccumulationBufferShaderID = Shader.PropertyToID("_DebugColorAccumulationBuffer");
        public static readonly int _DebugLightVolumesTextureShaderID = Shader.PropertyToID("_DebugLightVolumesTexture");
        public static readonly int _ColorGradientTextureShaderID = Shader.PropertyToID("_ColorGradientTexture");
        public static readonly int _MaxDebugLightCountShaderID = Shader.PropertyToID("_MaxDebugLightCount");

        // Render target array for the prepass
        RenderTargetIdentifier[] m_RTIDs = new RenderTargetIdentifier[2];

        MaterialPropertyBlock m_MaterialProperty = new MaterialPropertyBlock();

        public DebugLightVolumes()
        {
        }

        public void InitData(RenderPipelineResources renderPipelineResources)
        {
            m_DebugLightVolumeMaterial = CoreUtils.CreateEngineMaterial(renderPipelineResources.shaders.debugLightVolumePS);
            m_DebugLightVolumeCompute = renderPipelineResources.shaders.debugLightVolumeCS;
            m_DebugLightVolumeGradientKernel = m_DebugLightVolumeCompute.FindKernel("LightVolumeGradient");
            m_DebugLightVolumeColorsKernel = m_DebugLightVolumeCompute.FindKernel("LightVolumeColors");
            m_ColorGradientTexture = renderPipelineResources.textures.colorGradient;

            m_Blit = CoreUtils.CreateEngineMaterial(renderPipelineResources.shaders.blitPS);

            m_LightCountBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.RFloat, sRGB: false, enableRandomWrite: false, useMipMap: false, name: "LightVolumeCount");
            m_ColorAccumulationBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, enableRandomWrite: false, useMipMap: false, name: "LightVolumeColorAccumulation");
            m_DebugLightVolumesTexture = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, enableRandomWrite: true, useMipMap: false, name: "LightVolumeColorAccumulation");
            m_DepthBuffer = RTHandles.Alloc(Vector2.one, depthBufferBits: DepthBits.None, colorFormat: RenderTextureFormat.R8, sRGB: false, filterMode: FilterMode.Point, name: "LightVolumeDepth");
            // Fill the render target array
            m_RTIDs[0] = m_LightCountBuffer;
            m_RTIDs[1] = m_ColorAccumulationBuffer;
        }

        public void ReleaseData()
        {
            CoreUtils.Destroy(m_Blit);

            RTHandles.Release(m_DepthBuffer);
            RTHandles.Release(m_DebugLightVolumesTexture);
            RTHandles.Release(m_ColorAccumulationBuffer);
            RTHandles.Release(m_LightCountBuffer);

            CoreUtils.Destroy(m_DebugLightVolumeMaterial);
        }

        public void RenderLightVolumes(CommandBuffer cmd, HDCamera hdCamera, CullResults cullResults, LightingDebugSettings lightDebugSettings)
        {
            // Clear the buffers
            HDUtils.SetRenderTarget(cmd, hdCamera, m_ColorAccumulationBuffer, ClearFlag.Color, Color.black);
            HDUtils.SetRenderTarget(cmd, hdCamera, m_LightCountBuffer, ClearFlag.Color, Color.black);
            HDUtils.SetRenderTarget(cmd, hdCamera, m_DebugLightVolumesTexture, ClearFlag.Color, Color.black);

            // Set the render target array
            cmd.SetRenderTarget(m_RTIDs, m_DepthBuffer);

            // First of all let's do the regions for the light sources (we only support Punctual and Area)
            int numLights = cullResults.visibleLights.Count;
            for (int lightIdx = 0; lightIdx < numLights; ++lightIdx)
            {
                // Let's build the light's bounding sphere matrix
                Light currentLegacyLight = cullResults.visibleLights[lightIdx].light;
                if (currentLegacyLight == null) continue;
                HDAdditionalLightData currentHDRLight = currentLegacyLight.GetComponent<HDAdditionalLightData>();
                if (currentHDRLight == null) continue;

                Matrix4x4 positionMat = Matrix4x4.Translate(currentLegacyLight.transform.position);

                if(currentLegacyLight.type == LightType.Point || currentLegacyLight.type == LightType.Area)
                {
                    m_MaterialProperty.SetVector(_RangeShaderID, new Vector3(currentLegacyLight.range, currentLegacyLight.range, currentLegacyLight.range));
                    switch (currentHDRLight.lightTypeExtent)
                    {
                        case LightTypeExtent.Punctual:
                            {
                                m_MaterialProperty.SetColor(_ColorShaderID, new Color(0.0f, 0.5f, 0.0f, 1.0f));
                                m_MaterialProperty.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                                cmd.DrawMesh(DebugShapes.instance.RequestSphereMesh(), positionMat, m_DebugLightVolumeMaterial, 0, 0, m_MaterialProperty);
                            }
                            break;
                        case LightTypeExtent.Rectangle:
                            {
                                m_MaterialProperty.SetColor(_ColorShaderID, new Color(0.0f, 1.0f, 1.0f, 1.0f));
                                m_MaterialProperty.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                                cmd.DrawMesh(DebugShapes.instance.RequestSphereMesh(), positionMat, m_DebugLightVolumeMaterial, 0, 0, m_MaterialProperty);
                            }
                            break;
                        case LightTypeExtent.Tube:
                            {
                                m_MaterialProperty.SetColor(_ColorShaderID, new Color(1.0f, 0.0f, 0.5f, 1.0f));
                                m_MaterialProperty.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                                cmd.DrawMesh(DebugShapes.instance.RequestSphereMesh(), positionMat, m_DebugLightVolumeMaterial, 0, 0, m_MaterialProperty);
                            }
                            break;
                        default:
                            break;
                    }
                }
                else if(currentLegacyLight.type == LightType.Spot)
                {
                    if(currentHDRLight.spotLightShape == SpotLightShape.Cone)
                    {
                        float bottomRadius = Mathf.Tan(currentLegacyLight.spotAngle * Mathf.PI / 360.0f) * currentLegacyLight.range;
                        m_MaterialProperty.SetColor(_ColorShaderID, new Color(1.0f, 0.5f, 0.0f, 1.0f));
                        m_MaterialProperty.SetVector(_RangeShaderID, new Vector3(bottomRadius, bottomRadius, currentLegacyLight.range));
                        m_MaterialProperty.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                        cmd.DrawMesh(DebugShapes.instance.RequestConeMesh(), currentLegacyLight.gameObject.transform.localToWorldMatrix, m_DebugLightVolumeMaterial, 0, 0, m_MaterialProperty);
                    }
                    else if(currentHDRLight.spotLightShape == SpotLightShape.Box)
                    {
                        m_MaterialProperty.SetColor(_ColorShaderID, new Color(1.0f, 0.5f, 0.0f, 1.0f));
                        m_MaterialProperty.SetVector(_RangeShaderID, new Vector3(currentHDRLight.shapeWidth, currentHDRLight.shapeHeight, currentLegacyLight.range));
                        m_MaterialProperty.SetVector(_OffsetShaderID, new Vector3(0, 0, currentLegacyLight.range / 2.0f));
                        cmd.DrawMesh(DebugShapes.instance.RequestBoxMesh(), currentLegacyLight.gameObject.transform.localToWorldMatrix, m_DebugLightVolumeMaterial, 0, 0, m_MaterialProperty);
                    }
                    else if (currentHDRLight.spotLightShape == SpotLightShape.Pyramid)
                    {
                        float bottomWidth = Mathf.Tan(currentLegacyLight.spotAngle * Mathf.PI / 360.0f) * currentLegacyLight.range;
                        m_MaterialProperty.SetColor(_ColorShaderID, new Color(1.0f, 0.5f, 0.0f, 1.0f));
                        m_MaterialProperty.SetVector(_RangeShaderID, new Vector3(currentHDRLight.aspectRatio * bottomWidth * 2, bottomWidth * 2 , currentLegacyLight.range));
                        m_MaterialProperty.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                        cmd.DrawMesh(DebugShapes.instance.RequestPyramidMesh(), currentLegacyLight.gameObject.transform.localToWorldMatrix, m_DebugLightVolumeMaterial, 0, 0, m_MaterialProperty);
                    }
                }
            }

            // Now let's do the same but for reflection probes
            int numProbes = cullResults.visibleReflectionProbes.Count;
            for (int probeIdx = 0; probeIdx < numProbes; ++probeIdx)
            {
                // Let's build the light's bounding sphere matrix
                ReflectionProbe currentLegacyProbe = cullResults.visibleReflectionProbes[probeIdx].probe;
                HDAdditionalReflectionData currentHDProbe = currentLegacyProbe.GetComponent<HDAdditionalReflectionData>();

                if (!currentHDProbe)
                    continue;

                MaterialPropertyBlock m_MaterialProperty = new MaterialPropertyBlock();
                Mesh targetMesh = null;
                if (currentHDProbe.influenceVolume.shape == InfluenceShape.Sphere)
                {
                    m_MaterialProperty.SetVector(_RangeShaderID, new Vector3(currentHDProbe.influenceVolume.sphereRadius, currentHDProbe.influenceVolume.sphereRadius, currentHDProbe.influenceVolume.sphereRadius));
                    targetMesh = DebugShapes.instance.RequestSphereMesh();
                }
                else
                {
                    m_MaterialProperty.SetVector(_RangeShaderID, new Vector3(currentHDProbe.influenceVolume.boxSize.x, currentHDProbe.influenceVolume.boxSize.y, currentHDProbe.influenceVolume.boxSize.z));
                    targetMesh = DebugShapes.instance.RequestBoxMesh();
                }

                m_MaterialProperty.SetColor(_ColorShaderID, new Color(1.0f, 1.0f, 0.0f, 1.0f));
                m_MaterialProperty.SetVector(_OffsetShaderID, new Vector3(0, 0, 0));
                Matrix4x4 positionMat = Matrix4x4.Translate(currentLegacyProbe.transform.position);
                cmd.DrawMesh(targetMesh, positionMat, m_DebugLightVolumeMaterial, 0, 0, m_MaterialProperty);
            }

            // Define which kernel to use based on the lightloop options
            int targetKernel = lightDebugSettings.lightVolumeDebugByCategory == LightLoop.LightVolumeDebug.ColorAndEdge ? m_DebugLightVolumeColorsKernel : m_DebugLightVolumeGradientKernel;

            // Set the input params for the compute
            cmd.SetComputeTextureParam(m_DebugLightVolumeCompute, targetKernel, _DebugLightCountBufferShaderID, m_LightCountBuffer);
            cmd.SetComputeTextureParam(m_DebugLightVolumeCompute, targetKernel, _DebugColorAccumulationBufferShaderID, m_ColorAccumulationBuffer);
            cmd.SetComputeTextureParam(m_DebugLightVolumeCompute, targetKernel, _DebugLightVolumesTextureShaderID, m_DebugLightVolumesTexture);
            cmd.SetComputeTextureParam(m_DebugLightVolumeCompute, targetKernel, _ColorGradientTextureShaderID, m_ColorGradientTexture);
            cmd.SetComputeIntParam(m_DebugLightVolumeCompute, _MaxDebugLightCountShaderID, (int)lightDebugSettings.maxDebugLightCount);

            // Texture dimensions
            int texWidth = m_ColorAccumulationBuffer.rt.width;
            int texHeight = m_ColorAccumulationBuffer.rt.width;


            // Dispatch the compute
            int lightVolumesTileSize = 8;
            int numTilesX = (texWidth + (lightVolumesTileSize - 1)) / lightVolumesTileSize;
            int numTilesY = (texHeight + (lightVolumesTileSize - 1)) / lightVolumesTileSize;
            cmd.DispatchCompute(m_DebugLightVolumeCompute, targetKernel, numTilesX, numTilesY, 1);

            // Blit this into the camera target
            cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            m_MaterialProperty.SetTexture(HDShaderIDs._BlitTexture, m_DebugLightVolumesTexture);
            cmd.DrawProcedural(Matrix4x4.identity, m_DebugLightVolumeMaterial, 1, MeshTopology.Triangles, 3, 1, m_MaterialProperty);
        }
    }
}
