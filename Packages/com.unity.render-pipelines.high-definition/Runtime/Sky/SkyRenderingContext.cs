using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    internal class SkyRenderingContext
    {
        IBLFilterGGX            m_IBLFilterGGX;
        RTHandleSystem.RTHandle m_SkyboxCubemapRT;
        RTHandleSystem.RTHandle m_SkyboxGGXCubemapRT;
        RTHandleSystem.RTHandle m_SkyboxMarginalRowCdfRT;
        RTHandleSystem.RTHandle m_SkyboxConditionalCdfRT;
        Vector4                 m_CubemapScreenSize;
        Matrix4x4[]             m_facePixelCoordToViewDirMatrices   = new Matrix4x4[6];
        Matrix4x4[]             m_faceCameraInvViewProjectionMatrix = new Matrix4x4[6];
        bool                    m_SupportsConvolution = false;
        bool                    m_SupportsMIS = false;
        BuiltinSkyParameters    m_BuiltinParameters = new BuiltinSkyParameters();
        bool                    m_NeedUpdate = true;

        public RenderTexture cubemapRT { get { return m_SkyboxCubemapRT; } }
        public Texture reflectionTexture { get { return m_SkyboxGGXCubemapRT; } }


        public SkyRenderingContext(IBLFilterGGX filterGGX, int resolution, bool supportsConvolution)
        {
            m_IBLFilterGGX = filterGGX;
            m_SupportsConvolution = supportsConvolution;

            RebuildTextures(resolution);
        }

        public void RebuildTextures(int resolution)
        {
            bool updateNeeded = m_SkyboxCubemapRT == null || (m_SkyboxCubemapRT.rt.width != resolution);

            // Cleanup first if needed
            if (updateNeeded)
            {
                RTHandles.Release(m_SkyboxCubemapRT);
                RTHandles.Release(m_SkyboxGGXCubemapRT);

                m_SkyboxCubemapRT = null;
                m_SkyboxGGXCubemapRT = null;
            }

            if (!m_SupportsMIS && (m_SkyboxConditionalCdfRT != null))
            {
                RTHandles.Release(m_SkyboxConditionalCdfRT);
                RTHandles.Release(m_SkyboxMarginalRowCdfRT);

                m_SkyboxConditionalCdfRT = null;
                m_SkyboxMarginalRowCdfRT = null;
            }

            // Reallocate everything
            if (m_SkyboxCubemapRT == null)
            {
                m_SkyboxCubemapRT = RTHandles.Alloc(resolution, resolution, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, dimension: TextureDimension.Cube, useMipMap: true, autoGenerateMips: false, filterMode: FilterMode.Trilinear, name: "SkyboxCubemap");
            }

            if (m_SkyboxGGXCubemapRT == null && m_SupportsConvolution)
            {
                m_SkyboxGGXCubemapRT = RTHandles.Alloc(resolution, resolution, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, dimension: TextureDimension.Cube, useMipMap: true, autoGenerateMips: false, filterMode: FilterMode.Trilinear, name: "SkyboxGGXCubemap");
            }

            if (m_SupportsMIS && (m_SkyboxConditionalCdfRT == null))
            {
                // Temporary, it should be dependent on the sky resolution
                int width  = (int)LightSamplingParameters.TextureWidth;
                int height = (int)LightSamplingParameters.TextureHeight;

                // + 1 because we store the value of the integral of the cubemap at the end of the texture.
                m_SkyboxMarginalRowCdfRT = RTHandles.Alloc(height + 1, 1, colorFormat: RenderTextureFormat.RFloat, sRGB: false, useMipMap: false, enableRandomWrite: true, filterMode: FilterMode.Point, name: "SkyboxMarginalRowCdf");

                // TODO: switch the format to R16 (once it's available) to save some bandwidth.
                m_SkyboxMarginalRowCdfRT = RTHandles.Alloc(width, height, colorFormat: RenderTextureFormat.RFloat, sRGB: false, useMipMap: false, enableRandomWrite: true, filterMode: FilterMode.Point, name: "SkyboxMarginalRowCdf");
            }

            m_CubemapScreenSize = new Vector4((float)resolution, (float)resolution, 1.0f / (float)resolution, 1.0f / (float)resolution);

            if (updateNeeded)
            {
                m_NeedUpdate = true; // Special case. Even if update mode is set to OnDemand, we need to regenerate the environment after destroying the texture.
                RebuildSkyMatrices(resolution);
            }
        }

        public void RebuildSkyMatrices(int resolution)
        {
            var cubeProj = Matrix4x4.Perspective(90.0f, 1.0f, 0.01f, 1.0f);

            for (int i = 0; i < 6; ++i)
            {
                var lookAt      = Matrix4x4.LookAt(Vector3.zero, CoreUtils.lookAtList[i], CoreUtils.upVectorList[i]);
                var worldToView = lookAt * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API. ...

                m_facePixelCoordToViewDirMatrices[i] = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(0.5f * Mathf.PI, m_CubemapScreenSize, worldToView, true);
                m_faceCameraInvViewProjectionMatrix[i] = HDUtils.GetViewProjectionMatrix(lookAt, cubeProj).inverse;
            }
        }

        public void Cleanup()
        {
            RTHandles.Release(m_SkyboxCubemapRT);
            RTHandles.Release(m_SkyboxGGXCubemapRT);
            RTHandles.Release(m_SkyboxMarginalRowCdfRT);
            RTHandles.Release(m_SkyboxConditionalCdfRT);
        }

        void RenderSkyToCubemap(SkyUpdateContext skyContext)
        {
            for (int i = 0; i < 6; ++i)
            {
                m_BuiltinParameters.pixelCoordToViewDirMatrix = m_facePixelCoordToViewDirMatrices[i];
                m_BuiltinParameters.invViewProjMatrix = m_faceCameraInvViewProjectionMatrix[i];
                m_BuiltinParameters.colorBuffer = m_SkyboxCubemapRT;
                m_BuiltinParameters.depthBuffer = null;
                m_BuiltinParameters.hdCamera = null;

                CoreUtils.SetRenderTarget(m_BuiltinParameters.commandBuffer, m_SkyboxCubemapRT, ClearFlag.None, 0, (CubemapFace)i);
                skyContext.renderer.RenderSky(m_BuiltinParameters, true, skyContext.skySettings.includeSunInBaking);
            }

            // Generate mipmap for our cubemap
            Debug.Assert(m_SkyboxCubemapRT.rt.autoGenerateMips == false);
            m_BuiltinParameters.commandBuffer.GenerateMips(m_SkyboxCubemapRT);
        }

        void RenderCubemapGGXConvolution(SkyUpdateContext skyContext)
        {
            using (new ProfilingSample(m_BuiltinParameters.commandBuffer, "Update Env: GGX Convolution"))
            {
                if (skyContext.skySettings.useMIS && m_SupportsMIS)
                    m_IBLFilterGGX.FilterCubemapMIS(m_BuiltinParameters.commandBuffer, m_SkyboxCubemapRT, m_SkyboxGGXCubemapRT, m_SkyboxConditionalCdfRT, m_SkyboxMarginalRowCdfRT);
                else
                    m_IBLFilterGGX.FilterCubemap(m_BuiltinParameters.commandBuffer, m_SkyboxCubemapRT, m_SkyboxGGXCubemapRT);
            }
        }

        // We do our own hash here because Unity does not provide correct hash for builtin types
        // Moreover, we don't want to test every single parameters of the light so we filter them here in this specific function.
        int GetSunLightHashCode(Light light)
        {
            HDAdditionalLightData ald = light.GetComponent<HDAdditionalLightData>();
            unchecked
            {
                // Sun could influence the sky (like for procedural sky). We need to handle this possibility. If sun property change, then we need to update the sky
                int hash = 13;
                hash = hash * 23 + (light.GetHashCode() * 23 + light.transform.position.GetHashCode()) * 23 + light.transform.rotation.GetHashCode();
                hash = hash * 23 + light.color.GetHashCode();
                hash = hash * 23 + light.colorTemperature.GetHashCode();
                hash = hash * 23 + light.intensity.GetHashCode();
                // Note: We don't take into account cookie as it doesn't influence GI
                if (ald != null)
                {
                    hash = hash * 23 + ald.lightDimmer.GetHashCode();
                }

                return hash;
            }
        }

        // GC.Alloc
        // VolumeParameter`.op_Equality()
        public bool UpdateEnvironment(SkyUpdateContext skyContext, HDCamera camera, Light sunLight, bool updateRequired, CommandBuffer cmd)
        {
            bool result = false;
            if (skyContext.IsValid())
            {
                skyContext.currentUpdateTime += Time.deltaTime;

                m_BuiltinParameters.commandBuffer = cmd;
                m_BuiltinParameters.sunLight = sunLight;
                m_BuiltinParameters.screenSize = m_CubemapScreenSize;
                m_BuiltinParameters.cameraPosWS = camera.camera.transform.position;
                m_BuiltinParameters.hdCamera = null;
                m_BuiltinParameters.debugSettings = null; // We don't want any debug when updating the environment.

                int sunHash = 0;
                if (sunLight != null)
                    sunHash = GetSunLightHashCode(sunLight);
                int skyHash = sunHash * 23 + skyContext.skySettings.GetHashCode();

                bool forceUpdate = (updateRequired || skyContext.updatedFramesRequired > 0 || m_NeedUpdate);
                if (forceUpdate ||
                    (skyContext.skySettings.updateMode == EnvironementUpdateMode.OnChanged && skyHash != skyContext.skyParametersHash) ||
                    (skyContext.skySettings.updateMode == EnvironementUpdateMode.Realtime && skyContext.currentUpdateTime > skyContext.skySettings.updatePeriod))
                {
                    using (new ProfilingSample(cmd, "Sky Environment Pass"))
                    {
                        using (new ProfilingSample(cmd, "Update Env: Generate Lighting Cubemap"))
                        {
                            RenderSkyToCubemap(skyContext);
                        }

                        if (m_SupportsConvolution)
                        {
                            using (new ProfilingSample(cmd, "Update Env: Convolve Lighting Cubemap"))
                            {
                                RenderCubemapGGXConvolution(skyContext);
                            }
                        }

                        result = true;
                        skyContext.skyParametersHash = skyHash;
                        skyContext.currentUpdateTime = 0.0f;
                        skyContext.updatedFramesRequired--;
                        m_NeedUpdate = false;

#if UNITY_EDITOR
                        // In the editor when we change the sky we want to make the GI dirty so when baking again the new sky is taken into account.
                        // Changing the hash of the rendertarget allow to say that GI is dirty
                        m_SkyboxCubemapRT.rt.imageContentsHash = new Hash128((uint)skyContext.skySettings.GetHashCode(), 0, 0, 0);
#endif
                    }
                }
            }
            else
            {
                if (skyContext.skyParametersHash != 0)
                {
                    using (new ProfilingSample(cmd, "Clear Sky Environment Pass"))
                    {
                        CoreUtils.ClearCubemap(cmd, m_SkyboxCubemapRT, Color.black, true);
                        if (m_SupportsConvolution)
                        {
                            CoreUtils.ClearCubemap(cmd, m_SkyboxGGXCubemapRT, Color.black, true);
                        }
                    }

                    skyContext.skyParametersHash = 0;
                    result = true;
                }
            }

            return result;
        }

        public void RenderSky(SkyUpdateContext skyContext, HDCamera hdCamera, Light sunLight, RTHandleSystem.RTHandle colorBuffer, RTHandleSystem.RTHandle depthBuffer, DebugDisplaySettings debugSettings, CommandBuffer cmd)
        {
            if (skyContext.IsValid() && hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Sky)
            {
                using (new ProfilingSample(cmd, "Sky Pass"))
                {
                    m_BuiltinParameters.commandBuffer = cmd;
                    m_BuiltinParameters.sunLight = sunLight;
                    m_BuiltinParameters.pixelCoordToViewDirMatrix = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(hdCamera.camera.fieldOfView * Mathf.Deg2Rad, hdCamera.screenSize, hdCamera.viewMatrix, false);
                    m_BuiltinParameters.invViewProjMatrix = hdCamera.viewProjMatrix.inverse;
                    m_BuiltinParameters.screenSize = hdCamera.screenSize;
                    m_BuiltinParameters.cameraPosWS = hdCamera.camera.transform.position;
                    m_BuiltinParameters.colorBuffer = colorBuffer;
                    m_BuiltinParameters.depthBuffer = depthBuffer;
                    m_BuiltinParameters.hdCamera = hdCamera;
                    m_BuiltinParameters.debugSettings = debugSettings;

                    skyContext.renderer.SetRenderTargets(m_BuiltinParameters);
                    
                    // If the luxmeter is enabled, we don't render the sky
                    if (debugSettings.lightingDebugSettings.debugLightingMode != DebugLightingMode.LuxMeter)
                    {
                        // When rendering the visual sky for reflection probes, we need to remove the sun disk if skySettings.includeSunInBaking is false.
                        skyContext.renderer.RenderSky(m_BuiltinParameters, false, hdCamera.camera.cameraType != CameraType.Reflection || skyContext.skySettings.includeSunInBaking);
                    }
                }
            }
        }
    }
}
