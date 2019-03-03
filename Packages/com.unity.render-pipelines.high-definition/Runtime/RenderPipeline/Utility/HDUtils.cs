using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDUtils
    {
        public const RendererConfiguration k_RendererConfigurationBakedLighting = RendererConfiguration.PerObjectLightProbe | RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbeProxyVolume;
        public const RendererConfiguration k_RendererConfigurationBakedLightingWithShadowMask = k_RendererConfigurationBakedLighting | RendererConfiguration.PerObjectOcclusionProbe | RendererConfiguration.PerObjectOcclusionProbeProxyVolume | RendererConfiguration.PerObjectShadowMask;

        static public HDAdditionalReflectionData s_DefaultHDAdditionalReflectionData { get { return ComponentSingleton<HDAdditionalReflectionData>.instance; } }
        static public HDAdditionalLightData s_DefaultHDAdditionalLightData { get { return ComponentSingleton<HDAdditionalLightData>.instance; } }
        static public HDAdditionalCameraData s_DefaultHDAdditionalCameraData { get { return ComponentSingleton<HDAdditionalCameraData>.instance; } }
        static public AdditionalShadowData s_DefaultAdditionalShadowData { get { return ComponentSingleton<AdditionalShadowData>.instance; } }


        public static Material GetBlitMaterial()
        {
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
            {
                return hdPipeline.GetBlitMaterial();
            }

            return null;
        }

        public static RenderPipelineSettings hdrpSettings
        {
            get
            {
                HDRenderPipelineAsset hdPipelineAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;

                return hdPipelineAsset.renderPipelineSettings;
            }
        }
        public static int debugStep { get { return MousePositionDebug.instance.debugStep; } }

        static MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

        public static List<RenderPipelineMaterial> GetRenderPipelineMaterialList()
        {
            var baseType = typeof(RenderPipelineMaterial);
            var assembly = baseType.Assembly;

            var types = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(baseType))
                .Select(Activator.CreateInstance)
                .Cast<RenderPipelineMaterial>()
                .ToList();

            // Note: If there is a need for an optimization in the future of this function, user can
            // simply fill the materialList manually by commenting the code abode and returning a
            // custom list of materials they use in their game.
            //
            // return new List<RenderPipelineMaterial>
            // {
            //    new Lit(),
            //    new Unlit(),
            //    ...
            // };

            return types;
        }

        public static Matrix4x4 GetViewProjectionMatrix(Matrix4x4 worldToViewMatrix, Matrix4x4 projectionMatrix)
        {
            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(projectionMatrix, false);
            var gpuVP = gpuProj *  worldToViewMatrix * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API.

            return gpuVP;
        }

        // Helper to help to display debug info on screen
        static float s_OverlayLineHeight = -1.0f;
        public static void NextOverlayCoord(ref float x, ref float y, float overlayWidth, float overlayHeight, float width)
        {
            x += overlayWidth;
            s_OverlayLineHeight = Mathf.Max(overlayHeight, s_OverlayLineHeight);
            // Go to next line if it goes outside the screen.
            if (x + overlayWidth > width)
            {
                x = 0;
                y -= s_OverlayLineHeight;
                s_OverlayLineHeight = -1.0f;
            }
        }

        public static Matrix4x4 ComputePixelCoordToWorldSpaceViewDirectionMatrix(float verticalFoV, Vector2 lensShift, Vector4 screenSize, Matrix4x4 worldToViewMatrix, bool renderToCubemap)
        {
            // Compose the view space version first.
            // V = -(X, Y, Z), s.t. Z = 1,
            // X = (2x / resX - 1) * tan(vFoV / 2) * ar = x * [(2 / resX) * tan(vFoV / 2) * ar] + [-tan(vFoV / 2) * ar] = x * [-m00] + [-m20]
            // Y = (2y / resY - 1) * tan(vFoV / 2)      = y * [(2 / resY) * tan(vFoV / 2)]      + [-tan(vFoV / 2)]      = y * [-m11] + [-m21]
            
            float tanHalfVertFoV = Mathf.Tan(0.5f * verticalFoV);
            float aspectRatio    = screenSize.x * screenSize.w;

            // Compose the matrix.
            float m21 = (1.0f - 2.0f * lensShift.y) * tanHalfVertFoV;
            float m11 = -2.0f * screenSize.w * tanHalfVertFoV;

            float m20 = (1.0f - 2.0f * lensShift.x) * tanHalfVertFoV * aspectRatio;
            float m00 = -2.0f * screenSize.z * tanHalfVertFoV * aspectRatio;

            if (renderToCubemap)
            {
                // Flip Y.
                m11 = -m11;
                m21 = -m21;
            }

            var viewSpaceRasterTransform = new Matrix4x4(new Vector4(m00, 0.0f, 0.0f, 0.0f),
                    new Vector4(0.0f, m11, 0.0f, 0.0f),
                    new Vector4(m20, m21, -1.0f, 0.0f),
                    new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            // Remove the translation component.
            var homogeneousZero = new Vector4(0, 0, 0, 1);
            worldToViewMatrix.SetColumn(3, homogeneousZero);

            // Flip the Z to make the coordinate system left-handed.
            worldToViewMatrix.SetRow(2, -worldToViewMatrix.GetRow(2));

            // Transpose for HLSL.
            return Matrix4x4.Transpose(worldToViewMatrix.transpose * viewSpaceRasterTransform);
        }

        public static float ComputZPlaneTexelSpacing(float planeDepth, float verticalFoV, float resolutionY)
        {
            float tanHalfVertFoV = Mathf.Tan(0.5f * verticalFoV);
            return tanHalfVertFoV * (2.0f / resolutionY) * planeDepth;
        }

        private static void SetViewportAndClear(CommandBuffer cmd, HDCamera camera, RTHandleSystem.RTHandle buffer, ClearFlag clearFlag, Color clearColor)
        {
            // Clearing a partial viewport currently does not go through the hardware clear.
            // Instead it goes through a quad rendered with a specific shader.
            // When enabling wireframe mode in the scene view, unfortunately it overrides this shader thus breaking every clears.
            // That's why in the editor we don't set the viewport before clearing (it's set to full screen by the previous SetRenderTarget) but AFTER so that we benefit from un-bugged hardware clear.
            // We consider that the small loss in performance is acceptable in the editor.
            // A refactor of wireframe is needed before we can fix this properly (with not doing anything!)
#if !UNITY_EDITOR
            SetViewport(cmd, camera, buffer);
#endif
            CoreUtils.ClearRenderTarget(cmd, clearFlag, clearColor);
#if UNITY_EDITOR
            SetViewport(cmd, camera, buffer);
#endif
        }

        // This set of RenderTarget management methods is supposed to be used when rendering into a camera dependent render texture.
        // This will automatically set the viewport based on the camera size and the RTHandle scaling info.
        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RTHandleSystem.RTHandle buffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            cmd.SetRenderTarget(buffer, miplevel, cubemapFace, depthSlice);
            SetViewportAndClear(cmd, camera, buffer, clearFlag, clearColor);
        }

        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RTHandleSystem.RTHandle buffer, ClearFlag clearFlag = ClearFlag.None, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            SetRenderTarget(cmd, camera, buffer, clearFlag, CoreUtils.clearColorAllBlack, miplevel, cubemapFace, depthSlice);
        }

        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RTHandleSystem.RTHandle colorBuffer, RTHandleSystem.RTHandle depthBuffer, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            int cw = colorBuffer.rt.width;
            int ch = colorBuffer.rt.height;
            int dw = depthBuffer.rt.width;
            int dh = depthBuffer.rt.height;

            Debug.Assert(cw == dw && ch == dh);

            SetRenderTarget(cmd, camera, colorBuffer, depthBuffer, ClearFlag.None, CoreUtils.clearColorAllBlack, miplevel, cubemapFace, depthSlice);
        }

        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RTHandleSystem.RTHandle colorBuffer, RTHandleSystem.RTHandle depthBuffer, ClearFlag clearFlag, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            int cw = colorBuffer.rt.width;
            int ch = colorBuffer.rt.height;
            int dw = depthBuffer.rt.width;
            int dh = depthBuffer.rt.height;

            Debug.Assert(cw == dw && ch == dh);

            SetRenderTarget(cmd, camera, colorBuffer, depthBuffer, clearFlag, CoreUtils.clearColorAllBlack, miplevel, cubemapFace, depthSlice);
        }

        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RTHandleSystem.RTHandle colorBuffer, RTHandleSystem.RTHandle depthBuffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            int cw = colorBuffer.rt.width;
            int ch = colorBuffer.rt.height;
            int dw = depthBuffer.rt.width;
            int dh = depthBuffer.rt.height;

            Debug.Assert(cw == dw && ch == dh);

            CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer, miplevel, cubemapFace, depthSlice);
            SetViewportAndClear(cmd, camera, colorBuffer, clearFlag, clearColor);
        }

        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RenderTargetIdentifier[] colorBuffers, RTHandleSystem.RTHandle depthBuffer)
        {
            CoreUtils.SetRenderTarget(cmd, colorBuffers, depthBuffer, ClearFlag.None, CoreUtils.clearColorAllBlack);
            SetViewport(cmd, camera, depthBuffer);
        }

        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RenderTargetIdentifier[] colorBuffers, RTHandleSystem.RTHandle depthBuffer, ClearFlag clearFlag = ClearFlag.None)
        {
            CoreUtils.SetRenderTarget(cmd, colorBuffers, depthBuffer); // Don't clear here, viewport needs to be set before we do.
            SetViewportAndClear(cmd, camera, depthBuffer, clearFlag, CoreUtils.clearColorAllBlack);
        }

        public static void SetRenderTarget(CommandBuffer cmd, HDCamera camera, RenderTargetIdentifier[] colorBuffers, RTHandleSystem.RTHandle depthBuffer, ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(colorBuffers, depthBuffer);
            SetViewportAndClear(cmd, camera, depthBuffer, clearFlag, clearColor);
        }

        // Scaling viewport is done for auto-scaling render targets.
        // In the context of HDRP, every auto-scaled RT is scaled against the maximum RTHandles reference size (that can only grow).
        // When we render using a camera whose viewport is smaller than the RTHandles reference size (and thus smaller than the RT actual size), we need to set it explicitly (otherwise, native code will set the viewport at the size of the RT)
        // For auto-scaled RTs (like for example a half-resolution RT), we need to scale this viewport accordingly.
        // For non scaled RTs we just do nothing, the native code will set the viewport at the size of the RT anyway.
        public static void SetViewport(CommandBuffer cmd, HDCamera camera, RTHandleSystem.RTHandle target)
        {
            if (target.useScaling)
            {
                Debug.Assert(camera != null, "Missing HDCamera when setting up Render Target with auto-scale and Viewport.");
                Vector2Int scaledViewportSize = target.GetScaledSize(new Vector2Int(camera.actualWidth, camera.actualHeight));
                cmd.SetViewport(new Rect(0.0f, 0.0f, scaledViewportSize.x, scaledViewportSize.y));
            }
        }

        public static void BlitQuad(CommandBuffer cmd, Texture source, Vector4 scaleBiasTex, Vector4 scaleBiasRT, int mipLevelTex, bool bilinear)
        {
            s_PropertyBlock.SetTexture(HDShaderIDs._BlitTexture, source);
            s_PropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, scaleBiasTex);
            s_PropertyBlock.SetVector(HDShaderIDs._BlitScaleBiasRt, scaleBiasRT);
            s_PropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, mipLevelTex);
            cmd.DrawProcedural(Matrix4x4.identity, GetBlitMaterial(), bilinear ? 3 : 2, MeshTopology.Quads, 4, 1, s_PropertyBlock);
        }

        public static void BlitTexture(CommandBuffer cmd, RTHandleSystem.RTHandle source, RTHandleSystem.RTHandle destination, Vector4 scaleBias, float mipLevel, bool bilinear)
        {
            s_PropertyBlock.SetTexture(HDShaderIDs._BlitTexture, source);
            s_PropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, scaleBias);
            s_PropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, mipLevel);
            cmd.DrawProcedural(Matrix4x4.identity, GetBlitMaterial(), bilinear ? 1 : 0, MeshTopology.Triangles, 3, 1, s_PropertyBlock);
        }

        // In the context of HDRP, the internal render targets used during the render loop are the same for all cameras, no matter the size of the camera.
        // It means that we can end up rendering inside a partial viewport for one of these "camera space" rendering.
        // In this case, we need to make sure than when we blit from one such camera texture to another, we only blit the necessary portion corresponding to the camera viewport.
        // Here, both source and destination are camera-scaled.
        public static void BlitCameraTexture(CommandBuffer cmd, HDCamera camera, RTHandleSystem.RTHandle source, RTHandleSystem.RTHandle destination, float mipLevel = 0.0f, bool bilinear = false)
        {
            // Will set the correct camera viewport as well.
            SetRenderTarget(cmd, camera, destination);
            BlitTexture(cmd, source, destination, camera.viewportScale, mipLevel, bilinear);
        }

        // This case, both source and destination are camera-scaled but we want to override the scale/bias parameter.
        public static void BlitCameraTexture(CommandBuffer cmd, HDCamera camera, RTHandleSystem.RTHandle source, RTHandleSystem.RTHandle destination, Vector4 scaleBias, float mipLevel = 0.0f, bool bilinear = false)
        {
            // Will set the correct camera viewport as well.
            SetRenderTarget(cmd, camera, destination);
            BlitTexture(cmd, source, destination, scaleBias, mipLevel, bilinear);
        }

        public static void BlitCameraTexture(CommandBuffer cmd, HDCamera camera, RTHandleSystem.RTHandle source, RTHandleSystem.RTHandle destination, Rect destViewport, float mipLevel = 0.0f, bool bilinear = false)
        {
            SetRenderTarget(cmd, camera, destination);
            cmd.SetViewport(destViewport);
            BlitTexture(cmd, source, destination, camera.viewportScale, mipLevel, bilinear);
        }

        // This particular case is for blitting a camera-scaled texture into a non scaling texture. So we setup the full viewport (implicit in cmd.Blit) but have to scale the input UVs.
        public static void BlitCameraTexture(CommandBuffer cmd, HDCamera camera, RTHandleSystem.RTHandle source, RenderTargetIdentifier destination)
        {
            cmd.Blit(source, destination, new Vector2(camera.viewportScale.x, camera.viewportScale.y), Vector2.zero);
        }

        // This particular case is for blitting a non-scaled texture into a scaled texture. So we setup the partial viewport but don't scale the input UVs.
        public static void BlitCameraTexture(CommandBuffer cmd, HDCamera camera, RenderTargetIdentifier source, RTHandleSystem.RTHandle destination)
        {
            // Will set the correct camera viewport as well.
            SetRenderTarget(cmd, camera, destination);

            cmd.SetGlobalTexture(HDShaderIDs._BlitTexture, source);
            cmd.SetGlobalVector(HDShaderIDs._BlitScaleBias, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
            cmd.SetGlobalFloat(HDShaderIDs._BlitMipLevel, 0.0f);
            // Wanted to make things clean and not use SetGlobalXXX APIs but can't use MaterialPropertyBlock with RenderTargetIdentifier so YEY
            //s_PropertyBlock.SetTexture(HDShaderIDs._BlitTexture, source);
            //s_PropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, camera.scaleBias);
            cmd.DrawProcedural(Matrix4x4.identity, GetBlitMaterial(), 0, MeshTopology.Triangles, 3, 1);
        }

        // These method should be used to render full screen triangles sampling auto-scaling RTs.
        // This will set the proper viewport and UV scale.
        public static void DrawFullScreen(CommandBuffer commandBuffer, HDCamera camera, Material material,
            RTHandleSystem.RTHandle colorBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            HDUtils.SetRenderTarget(commandBuffer, camera, colorBuffer);
            commandBuffer.SetGlobalVector(HDShaderIDs._ScreenToTargetScale, camera.doubleBufferedViewportScale);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        public static void DrawFullScreen(CommandBuffer commandBuffer, HDCamera camera, Material material,
            RTHandleSystem.RTHandle colorBuffer, RTHandleSystem.RTHandle depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            HDUtils.SetRenderTarget(commandBuffer, camera, colorBuffer, depthStencilBuffer);
            commandBuffer.SetGlobalVector(HDShaderIDs._ScreenToTargetScale, camera.doubleBufferedViewportScale);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        public static void DrawFullScreen(CommandBuffer commandBuffer, HDCamera camera, Material material,
            RenderTargetIdentifier[] colorBuffers, RTHandleSystem.RTHandle depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            HDUtils.SetRenderTarget(commandBuffer, camera, colorBuffers, depthStencilBuffer);
            commandBuffer.SetGlobalVector(HDShaderIDs._ScreenToTargetScale, camera.doubleBufferedViewportScale);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        public static void DrawFullScreen(CommandBuffer commandBuffer, HDCamera camera, Material material,
            RenderTargetIdentifier colorBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            CoreUtils.SetRenderTarget(commandBuffer, colorBuffer);
            commandBuffer.SetGlobalVector(HDShaderIDs._ScreenToTargetScale, camera.doubleBufferedViewportScale);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        // Returns mouse coordinates: (x,y) in pixels and (z,w) normalized inside the render target (not the viewport)
        public static Vector4 GetMouseCoordinates(HDCamera camera)
        {
            // We request the mouse post based on the type of the camera
            Vector2 mousePixelCoord = MousePositionDebug.instance.GetMousePosition(camera.screenSize.y, camera.camera.cameraType == CameraType.SceneView);
            return new Vector4(mousePixelCoord.x, mousePixelCoord.y, camera.viewportScale.x * mousePixelCoord.x / camera.screenSize.x, camera.viewportScale.y * mousePixelCoord.y / camera.screenSize.y);
        }

        // Returns mouse click coordinates: (x,y) in pixels and (z,w) normalized inside the render target (not the viewport)
        public static Vector4 GetMouseClickCoordinates(HDCamera camera)
        {
            Vector2 mousePixelCoord = MousePositionDebug.instance.GetMouseClickPosition(camera.screenSize.y);
            return new Vector4(mousePixelCoord.x, mousePixelCoord.y, camera.viewportScale.x * mousePixelCoord.x / camera.screenSize.x, camera.viewportScale.y * mousePixelCoord.y / camera.screenSize.y);
        }

        // This function check if camera is a CameraPreview, then check if this preview is a regular preview (i.e not a preview from the camera editor)
        public static bool IsRegularPreviewCamera(Camera camera)
        {
            var additionalCameraData = camera.GetComponent<HDAdditionalCameraData>();
            return camera.cameraType == CameraType.Preview && ((additionalCameraData == null) || (additionalCameraData && !additionalCameraData.isEditorCameraPreview));
        }

        // Post-processing misc
        public static bool IsPostProcessingActive(PostProcessLayer layer)
        {
            return layer != null
                && layer.enabled;
        }

        public static bool IsTemporalAntialiasingActive(PostProcessLayer layer)
        {
            return IsPostProcessingActive(layer)
                && layer.antialiasingMode == PostProcessLayer.Antialiasing.TemporalAntialiasing
                && layer.temporalAntialiasing.IsSupported();
        }

        // We need these at runtime for RenderPipelineResources upgrade
        public static string GetHDRenderPipelinePath()
        {
            return "Packages/com.unity.render-pipelines.high-definition/";
        }

        public static string GetPostProcessingPath()
        {
            return "Packages/com.unity.postprocessing/";
        }

        public static string GetCorePath()
        {
            return "Packages/com.unity.render-pipelines.core/";
        }

        public struct PackedMipChainInfo
        {
            public Vector2Int   textureSize;
            public int          mipLevelCount;
            public Vector2Int[] mipLevelSizes;
            public Vector2Int[] mipLevelOffsets;

            private bool        m_OffsetBufferWillNeedUpdate;

            public void Allocate()
            {
                mipLevelOffsets = new Vector2Int[15];
                mipLevelSizes   = new Vector2Int[15];
                m_OffsetBufferWillNeedUpdate = true;
            }

            // We pack all MIP levels into the top MIP level to avoid the Pow2 MIP chain restriction.
            // We compute the required size iteratively.
            // This function is NOT fast, but it is illustrative, and can be optimized later.
            public void ComputePackedMipChainInfo(Vector2Int viewportSize)
            {
                textureSize        = viewportSize;
                mipLevelSizes[0]   = viewportSize;
                mipLevelOffsets[0] = Vector2Int.zero;

                int        mipLevel = 0;
                Vector2Int mipSize  = viewportSize;

                do
                {
                    mipLevel++;

                    // Round up.
                    mipSize.x = Math.Max(1, (mipSize.x + 1) >> 1);
                    mipSize.y = Math.Max(1, (mipSize.y + 1) >> 1);

                    mipLevelSizes[mipLevel] = mipSize;

                    Vector2Int prevMipBegin = mipLevelOffsets[mipLevel - 1];
                    Vector2Int prevMipEnd   = prevMipBegin + mipLevelSizes[mipLevel - 1];

                    Vector2Int mipBegin = new Vector2Int();

                    if ((mipLevel & 1) != 0) // Odd
                    {
                        mipBegin.x = prevMipBegin.x;
                        mipBegin.y = prevMipEnd.y;
                    }
                    else // Even
                    {
                        mipBegin.x = prevMipEnd.x;
                        mipBegin.y = prevMipBegin.y;
                    }

                    mipLevelOffsets[mipLevel] = mipBegin;

                    textureSize.x = Math.Max(textureSize.x, mipBegin.x + mipSize.x);
                    textureSize.y = Math.Max(textureSize.y, mipBegin.y + mipSize.y);

                } while ((mipSize.x > 1) || (mipSize.y > 1));

                mipLevelCount = mipLevel + 1;
                m_OffsetBufferWillNeedUpdate = true;
            }

            public ComputeBuffer GetOffsetBufferData(ComputeBuffer mipLevelOffsetsBuffer)
            {

                if (m_OffsetBufferWillNeedUpdate)
                {
                    mipLevelOffsetsBuffer.SetData(mipLevelOffsets);
                    m_OffsetBufferWillNeedUpdate = false;
                }

                return mipLevelOffsetsBuffer;
            }
        }

        public static int DivRoundUp(int x, int y)
        {
            return (x + y - 1) / y;
        }

        // Note: If you add new platform in this function, think about adding support in IsSupportedBuildTarget() function below
        public static bool IsSupportedGraphicDevice(GraphicsDeviceType graphicDevice)
        {
            return (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12 ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStation4 ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.XboxOne ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.XboxOneD3D12 ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan ||
                    SystemInfo.graphicsDeviceType == (GraphicsDeviceType)22 /*GraphicsDeviceType.Switch*/);
        }

        public static void CheckRTCreated(RenderTexture rt)
        {
            // In some cases when loading a project for the first time in the editor, the internal resource is destroyed.
            // When used as render target, the C++ code will re-create the resource automatically. Since here it's used directly as an UAV, we need to check manually
            if (!rt.IsCreated())
                rt.Create();
        }

        public static Vector4 ComputeUvScaleAndLimit(Vector2Int viewportResolution, Vector2Int bufferSize)
        {
            Vector2 rcpBufferSize = new Vector2(1.0f / bufferSize.x, 1.0f / bufferSize.y);

            // vp_scale = vp_dim / tex_dim.
            Vector2 uvScale = new Vector2(viewportResolution.x * rcpBufferSize.x,
                                          viewportResolution.y * rcpBufferSize.y);

            // clamp to (vp_dim - 0.5) / tex_dim.
            Vector2 uvLimit = new Vector2((viewportResolution.x - 0.5f) * rcpBufferSize.x,
                                          (viewportResolution.y - 0.5f) * rcpBufferSize.y);

            return new Vector4(uvScale.x, uvScale.y, uvLimit.x, uvLimit.y);
        }

#if UNITY_EDITOR
        // This function can't be in HDEditorUtils because we need it in HDRenderPipeline.cs (and HDEditorUtils is in an editor asmdef)
        public static bool IsSupportedBuildTarget(UnityEditor.BuildTarget buildTarget)
        {
            return (buildTarget == UnityEditor.BuildTarget.StandaloneWindows ||
                    buildTarget == UnityEditor.BuildTarget.StandaloneWindows64 ||
                    buildTarget == UnityEditor.BuildTarget.StandaloneLinux64 ||
                    buildTarget == UnityEditor.BuildTarget.StandaloneLinuxUniversal ||
                    buildTarget == UnityEditor.BuildTarget.StandaloneOSX ||
                    buildTarget == UnityEditor.BuildTarget.WSAPlayer ||
                    buildTarget == UnityEditor.BuildTarget.XboxOne ||
                    buildTarget == UnityEditor.BuildTarget.PS4 ||
                    buildTarget == UnityEditor.BuildTarget.Switch);
        }
#endif

        public static bool IsOperatingSystemSupported(string os)
        {
            // Metal support depends on OS version:
            // macOS 10.11.x doesn't have tessellation / earlydepthstencil support, early driver versions were buggy in general
            // macOS 10.12.x should usually work with AMD, but issues with Intel/Nvidia GPUs. Regardless of the GPU, there are issues with MTLCompilerService crashing with some shaders
            // macOS 10.13.x is expected to work, and if it's a driver/shader compiler issue, there's still hope on getting it fixed to next shipping OS patch release
            //
            // Has worked experimentally with iOS in the past, but it's not currently supported
            //

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
            {
                if (os.StartsWith("Mac"))
                {
                    // TODO: Expose in C# version number, for now assume "Mac OS X 10.10.4" format with version 10 at least
                    int startIndex = os.LastIndexOf(" ");
                    var parts = os.Substring(startIndex + 1).Split('.');
                    int a = Convert.ToInt32(parts[0]);
                    int b = Convert.ToInt32(parts[1]);
                    // In case in the future there's a need to disable specific patch releases
                    // int c = Convert.ToInt32(parts[2]);

                    if (a < 10 || b < 13)
                        return false;
                }
            }

            return true;
        }
    }
}
