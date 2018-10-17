using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Internal
{
    class ReflectionSystemInternal
    {
        static Camera s_RenderCamera = null;
        static HDAdditionalCameraData s_RenderCameraData;
        static int frame = Time.frameCount;

        HashSet<HDAdditionalReflectionData> m_AdditionalDataReflectionProbes;
        HashSet<HDAdditionalReflectionData> m_AdditionalDataReflectionProbe_RealtimeUpdate;
        HashSet<HDAdditionalReflectionData> m_AdditionalDataReflectionProbe_RequestRealtimeRender;
        HDAdditionalReflectionData[] m_AdditionalDataReflectionProbe_RealtimeUpdate_WorkArray;

        // GC.Alloc
        // HashSet`1.IntersectWith()
        // HashSet`1.UnionWith()
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbes;
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbe_DirtyBounds;
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbe_RequestRealtimeRender;
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbe_RealtimeUpdate;
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbe_PerCamera_RealtimeUpdate;
        PlanarReflectionProbe[] m_PlanarReflectionProbe_RealtimeUpdate_WorkArray;

        Dictionary<PlanarReflectionProbe, BoundingSphere> m_PlanarReflectionProbeBounds;
        PlanarReflectionProbe[] m_PlanarReflectionProbesArray;
        BoundingSphere[] m_PlanarReflectionProbeBoundsArray;

        ReflectionSystemParameters m_Parameters;

        public ReflectionSystemInternal(ReflectionSystemParameters parameters, ReflectionSystemInternal previous)
        {
            m_Parameters = parameters;

            // Runtime collections
            m_PlanarReflectionProbeBounds = new Dictionary<PlanarReflectionProbe, BoundingSphere>(parameters.maxActivePlanarReflectionProbe);
            m_PlanarReflectionProbesArray = new PlanarReflectionProbe[parameters.maxActivePlanarReflectionProbe];
            m_PlanarReflectionProbeBoundsArray = new BoundingSphere[parameters.maxActivePlanarReflectionProbe];
            m_PlanarReflectionProbe_RealtimeUpdate_WorkArray = new PlanarReflectionProbe[parameters.maxPlanarReflectionProbePerCamera];
            m_AdditionalDataReflectionProbe_RealtimeUpdate_WorkArray = new HDAdditionalReflectionData[parameters.maxActiveReflectionProbe]; ;

            // Persistent collections
            m_AdditionalDataReflectionProbes = new HashSet<HDAdditionalReflectionData>();
            m_AdditionalDataReflectionProbe_RealtimeUpdate = new HashSet<HDAdditionalReflectionData>();
            m_AdditionalDataReflectionProbe_RequestRealtimeRender = new HashSet<HDAdditionalReflectionData>();
            m_PlanarReflectionProbes = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_DirtyBounds = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_RequestRealtimeRender = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_RealtimeUpdate = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbe_PerCamera_RealtimeUpdate = new HashSet<PlanarReflectionProbe>();

            if (previous != null)
            {
                m_AdditionalDataReflectionProbes.UnionWith(previous.m_AdditionalDataReflectionProbes);
                m_AdditionalDataReflectionProbe_RequestRealtimeRender.UnionWith(previous.m_AdditionalDataReflectionProbe_RequestRealtimeRender);
                m_AdditionalDataReflectionProbe_RealtimeUpdate.UnionWith(previous.m_AdditionalDataReflectionProbe_RealtimeUpdate);
                m_PlanarReflectionProbes.UnionWith(previous.m_PlanarReflectionProbes);
                m_PlanarReflectionProbe_DirtyBounds.UnionWith(m_PlanarReflectionProbes);
                m_PlanarReflectionProbe_RequestRealtimeRender.UnionWith(previous.m_PlanarReflectionProbe_RequestRealtimeRender);
                m_PlanarReflectionProbe_RealtimeUpdate.UnionWith(previous.m_PlanarReflectionProbe_RealtimeUpdate);
                m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.UnionWith(previous.m_PlanarReflectionProbe_PerCamera_RealtimeUpdate);
            }
        }

        public void RegisterProbe(PlanarReflectionProbe planarProbe)
        {
            m_PlanarReflectionProbes.Add(planarProbe);
            SetProbeBoundsDirty(planarProbe);

            if (planarProbe.mode == ReflectionProbeMode.Realtime)
            {
                switch (planarProbe.refreshMode)
                {
                    case ReflectionProbeRefreshMode.OnAwake:
                        m_PlanarReflectionProbe_RequestRealtimeRender.Add(planarProbe);
                        break;
                    case ReflectionProbeRefreshMode.EveryFrame:
                    {
                        switch (planarProbe.capturePositionMode)
                        {
                            case PlanarReflectionProbe.CapturePositionMode.Static:
                                m_PlanarReflectionProbe_RealtimeUpdate.Add(planarProbe);
                                break;
                            case PlanarReflectionProbe.CapturePositionMode.MirrorCamera:
                                m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.Add(planarProbe);
                                break;
                        }
                        break;
                    }
                }
            }
        }

        public void RegisterProbe(HDAdditionalReflectionData additional)
        {
            m_AdditionalDataReflectionProbes.Add(additional);
            //SetProbeBoundsDirty(probe);

            if (additional.mode == ReflectionProbeMode.Realtime)
            {
                //switch (additional.refreshMode)
                //{
                //    case ReflectionProbeRefreshMode.OnAwake:
                //        m_AdditionalDataReflectionProbe_RequestRealtimeRender.Add(additional);
                //        break;
                //    case ReflectionProbeRefreshMode.EveryFrame:
                        m_AdditionalDataReflectionProbe_RealtimeUpdate.Add(additional);
                //        break;
                //}
            }
        }

        public void UnregisterProbe(PlanarReflectionProbe planarProbe)
        {
            m_PlanarReflectionProbes.Remove(planarProbe);
            m_PlanarReflectionProbeBounds.Remove(planarProbe);
            m_PlanarReflectionProbe_DirtyBounds.Remove(planarProbe);
            m_PlanarReflectionProbe_RequestRealtimeRender.Remove(planarProbe);
            m_PlanarReflectionProbe_RealtimeUpdate.Remove(planarProbe);
            m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.Remove(planarProbe);
        }

        public void UnregisterProbe(HDAdditionalReflectionData additional)
        {
            m_AdditionalDataReflectionProbes.Remove(additional);
            m_AdditionalDataReflectionProbe_RequestRealtimeRender.Remove(additional);
            m_AdditionalDataReflectionProbe_RealtimeUpdate.Remove(additional);
        }

        public void PrepareCull(Camera camera, ReflectionProbeCullResults results)
        {
            UpdateAllPlanarReflectionProbeBounds();
           
            var cullingGroup = CullingGroupManager.instance.Alloc();            
            cullingGroup.targetCamera = camera;
            cullingGroup.SetBoundingSpheres(m_PlanarReflectionProbeBoundsArray);
            cullingGroup.SetBoundingSphereCount(Mathf.Min(m_PlanarReflectionProbeBounds.Count, m_PlanarReflectionProbeBoundsArray.Length));

            results.PrepareCull(cullingGroup, m_PlanarReflectionProbesArray);
        }

        public void RenderAllRealtimeProbesFor(ReflectionProbeType probeType, Camera viewerCamera)
        {
            if ((probeType & ReflectionProbeType.PlanarReflection) != 0)
            {
                var length = Mathf.Min(m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.Count, m_PlanarReflectionProbe_RealtimeUpdate_WorkArray.Length);
                var index = 0;
                foreach (var p in m_PlanarReflectionProbe_PerCamera_RealtimeUpdate)
                {
                    m_PlanarReflectionProbe_RealtimeUpdate_WorkArray[index] = p;
                    if (++index >= length)
                        break;
                }
#if DEBUG
                var discarded = m_PlanarReflectionProbe_PerCamera_RealtimeUpdate.Count - length;
                if (discarded > 0)
                    Debug.LogWarningFormat("There are more planar probe than supported in a single rendering, {0} probes discardeds", discarded);
#endif

                // 1. Allocate if necessary target texture
                for (var i = 0; i < length; i++)
                {
                    var probe = m_PlanarReflectionProbe_RealtimeUpdate_WorkArray[i];

                    if (!IsRealtimeTextureValid(probe.realtimeTexture, true))
                    {
                        if (probe.realtimeTexture != null)
                            probe.realtimeTexture.Release();
                        probe.realtimeTexture = NewRenderTarget(probe);
                    }
                }

                // 2. Render
                for (var i = 0; i < length; i++)
                {
                    var probe = m_PlanarReflectionProbe_RealtimeUpdate_WorkArray[i];
                    Render(probe, probe.realtimeTexture, viewerCamera);
                }
            }
        }

        public void RenderAllRealtimeProbes()
        {
            if (frame != Time.frameCount)
            {
                //do only one per frame
                frame = Time.frameCount;

                // Discard disabled probes in requested render probes
                m_PlanarReflectionProbe_RequestRealtimeRender.IntersectWith(m_PlanarReflectionProbes);
                // Include all realtime probe modes
                m_PlanarReflectionProbe_RequestRealtimeRender.UnionWith(m_PlanarReflectionProbe_RealtimeUpdate);
                var length = Mathf.Min(m_PlanarReflectionProbe_RequestRealtimeRender.Count, m_PlanarReflectionProbe_RealtimeUpdate_WorkArray.Length);
                m_PlanarReflectionProbe_RequestRealtimeRender.CopyTo(m_PlanarReflectionProbe_RealtimeUpdate_WorkArray);
                m_PlanarReflectionProbe_RequestRealtimeRender.Clear();

                // 1. Allocate if necessary target texture
                for (var i = 0; i < length; i++)
                {
                    var probe = m_PlanarReflectionProbe_RealtimeUpdate_WorkArray[i];

                    if (!IsRealtimeTextureValid(probe.realtimeTexture, true))
                    {
                        if (probe.realtimeTexture != null)
                            probe.realtimeTexture.Release();
                        probe.realtimeTexture = NewRenderTarget(probe);
                    }
                }

                // 2. Render
                for (var i = 0; i < length; i++)
                {
                    var probe = m_PlanarReflectionProbe_RealtimeUpdate_WorkArray[i];
                    Render(probe, probe.realtimeTexture);
                }


                // Discard disabled probes in requested render probes
                m_AdditionalDataReflectionProbe_RequestRealtimeRender.IntersectWith(m_AdditionalDataReflectionProbes);
                // Include all realtime probe modes
                m_AdditionalDataReflectionProbe_RequestRealtimeRender.UnionWith(m_AdditionalDataReflectionProbe_RealtimeUpdate);
                
                length = Mathf.Min(m_AdditionalDataReflectionProbe_RequestRealtimeRender.Count, m_AdditionalDataReflectionProbe_RealtimeUpdate_WorkArray.Length);
                m_AdditionalDataReflectionProbe_RequestRealtimeRender.CopyTo(m_AdditionalDataReflectionProbe_RealtimeUpdate_WorkArray);
                m_AdditionalDataReflectionProbe_RequestRealtimeRender.Clear();

                // 1. Allocate if necessary target texture
                for (var i = 0; i < length; i++)
                {
                    var additional = m_AdditionalDataReflectionProbe_RealtimeUpdate_WorkArray[i];

                    if (!IsRealtimeTextureValid(additional.realtimeTexture, false))
                    {
                        if (additional.realtimeTexture != null)
                            additional.realtimeTexture.Release();
                        additional.realtimeTexture = NewRenderTarget(additional);
                    }
                }

                // 2. Render
                for (var i = 0; i < length; i++)
                {
                    var additional = m_AdditionalDataReflectionProbe_RealtimeUpdate_WorkArray[i];
                    Render(additional, additional.realtimeTexture);
                }
            }
        }

        public RenderTexture NewRenderTarget(PlanarReflectionProbe probe)
        {
            var rt = new RenderTexture(m_Parameters.planarReflectionProbeSize, m_Parameters.planarReflectionProbeSize, 0, RenderTextureFormat.ARGBHalf);
            // No hide and don't save for this one
            rt.useMipMap = true;
            rt.autoGenerateMips = false;
            rt.name = CoreUtils.GetRenderTargetAutoName(m_Parameters.planarReflectionProbeSize, m_Parameters.planarReflectionProbeSize, 1, RenderTextureFormat.ARGBHalf, "PlanarProbeRT");
            rt.Create();
            return rt;
        }

        public RenderTexture NewRenderTarget(HDAdditionalReflectionData probe)
        {
            var rt = new RenderTexture(m_Parameters.reflectionProbeSize, m_Parameters.reflectionProbeSize, 0, RenderTextureFormat.ARGBHalf);
            // No hide and don't save for this one
            rt.useMipMap = true;
            rt.autoGenerateMips = false;
            rt.name = CoreUtils.GetRenderTargetAutoName(m_Parameters.reflectionProbeSize, m_Parameters.reflectionProbeSize, 1, RenderTextureFormat.ARGBHalf, "ProbeRT");
            rt.dimension = TextureDimension.Cube;
            rt.Create();
            return rt;
        }

        //public float GetCaptureCameraFOVFor(PlanarReflectionProbe probe, Camera viewerCamera)
        //{
        //    switch (probe.influenceVolume.shapeType)
        //    {
        //        case ShapeType.Box:
        //        {
        //            var captureToWorld = probe.GetCaptureToWorld(viewerCamera);
        //            var influenceToWorld = Matrix4x4.TRS(probe.transform.TransformPoint(probe.influenceVolume.boxBaseOffset), probe.transform.rotation, Vector3.one);
        //            var influenceToCapture = captureToWorld.inverse * influenceToWorld;
        //            var min = influenceToCapture.MultiplyPoint(-probe.influenceVolume.boxBaseSize * 0.5f);
        //            var max = influenceToCapture.MultiplyPoint(probe.influenceVolume.boxBaseSize * 0.5f);
        //            var minAngle = Mathf.Atan2(Mathf.Sqrt(min.x * min.x + min.y * min.y), min.z) * Mathf.Rad2Deg;
        //            var maxAngle = Mathf.Atan2(Mathf.Sqrt(max.x * max.x + max.y * max.y), max.z) * Mathf.Rad2Deg;
        //            return Mathf.Max(minAngle, maxAngle) * 2;
        //        }
        //        default:
        //            throw new NotImplementedException();
        //    }
        //}

        bool IsRealtimeTextureValid(RenderTexture renderTexture, bool isPlanar)
        {
            if(isPlanar)
                return renderTexture != null
                    && renderTexture.width == m_Parameters.planarReflectionProbeSize
                    && renderTexture.height == m_Parameters.planarReflectionProbeSize
                    && renderTexture.format == RenderTextureFormat.ARGBHalf
                    && renderTexture.useMipMap;
            else
                return renderTexture != null
                    && renderTexture.width == m_Parameters.reflectionProbeSize
                    && renderTexture.height == m_Parameters.reflectionProbeSize
                    && renderTexture.format == RenderTextureFormat.ARGBHalf
                    && renderTexture.useMipMap;
        }

        public void RequestRealtimeRender(PlanarReflectionProbe probe)
        {
            m_PlanarReflectionProbe_RequestRealtimeRender.Add(probe);
        }

        public void Render(PlanarReflectionProbe probe, RenderTexture target, Camera viewerCamera = null)
        {
            var renderCamera = GetRenderCamera();
            var renderCameraAdditionalData = GetRenderCameraAdditionalData();

            // Copy current frameSettings of this probe to the HDAdditionalData of the render camera
            probe.frameSettings.CopyTo(s_RenderCameraData.GetFrameSettings());

            renderCamera.targetTexture = target;

            SetupCameraForRender(renderCamera, renderCameraAdditionalData, probe, viewerCamera);
            GL.invertCulling = IsProbeCaptureMirrored(probe, viewerCamera);
            renderCamera.Render();
            GL.invertCulling = false;
            renderCamera.targetTexture = null;
            target.IncrementUpdateCount();
        }

        public void Render(HDAdditionalReflectionData additional, RenderTexture target)
        {
            var renderCamera = GetRenderCamera();
            var renderCameraAdditionalData = GetRenderCameraAdditionalData();

            // Copy current frameSettings of this probe to the HDAdditionalData of the render camera
            //probe.frameSettings.CopyTo(s_RenderCameraData.GetFrameSettings());
            
            SetupCameraForRender(renderCamera, renderCameraAdditionalData, additional);
            renderCamera.RenderToCubemap(target);
            target.IncrementUpdateCount();
        }

        void SetProbeBoundsDirty(PlanarReflectionProbe planarProbe)
        {
            m_PlanarReflectionProbe_DirtyBounds.Add(planarProbe);
        }

        void UpdateAllPlanarReflectionProbeBounds()
        {
            if (m_PlanarReflectionProbe_DirtyBounds.Count > 0)
            {
                m_PlanarReflectionProbe_DirtyBounds.IntersectWith(m_PlanarReflectionProbes);
                foreach (var planarReflectionProbe in m_PlanarReflectionProbe_DirtyBounds)
                    UpdatePlanarReflectionProbeBounds(planarReflectionProbe);

                var length = m_PlanarReflectionProbeBoundsArray.Length;
                var index = 0;
                foreach (var k in m_PlanarReflectionProbeBounds)
                {
                    m_PlanarReflectionProbeBoundsArray[index] = k.Value;
                    m_PlanarReflectionProbesArray[index] = k.Key;
                    if (++index >= length)
                        break;
                }
            }
        }

        void UpdatePlanarReflectionProbeBounds(PlanarReflectionProbe planarReflectionProbe)
        {
            m_PlanarReflectionProbeBounds[planarReflectionProbe] = planarReflectionProbe.boundingSphere;
        }

        static void SetupCameraForRender(Camera camera, HDAdditionalCameraData additionalData, PlanarReflectionProbe probe, Camera viewerCamera = null)
        {
            float nearClipPlane, farClipPlane, aspect, fov;
            Color backgroundColor;
            CameraClearFlags clearFlags;
            Vector3 capturePosition;
            Quaternion captureRotation;
            Matrix4x4 worldToCamera, projection;

            CalculateCaptureCameraProperties(probe,
                out nearClipPlane, out farClipPlane,
                out aspect, out fov, out clearFlags, out backgroundColor,
                out worldToCamera, out projection,
                out capturePosition, out captureRotation, viewerCamera);

            camera.clearFlags = clearFlags;
            camera.backgroundColor = backgroundColor;
            camera.aspect = aspect;

            additionalData.backgroundColorHDR = probe.captureSettings.backgroundColorHDR;
            additionalData.clearColorMode = probe.captureSettings.clearColorMode;
            additionalData.clearDepth = probe.captureSettings.clearDepth;
            camera.cullingMask = probe.captureSettings.cullingMask;
            additionalData.volumeLayerMask = probe.captureSettings.volumeLayerMask;
            additionalData.volumeAnchorOverride = viewerCamera != null ? viewerCamera.transform : probe.captureSettings.volumeAnchorOverride;
            camera.useOcclusionCulling = probe.captureSettings.useOcclusionCulling;
            camera.orthographic = probe.captureSettings.projection == CameraProjection.Orthographic;
            camera.farClipPlane = farClipPlane;
            camera.nearClipPlane = nearClipPlane;
            camera.fieldOfView = fov;
            camera.orthographicSize = probe.captureSettings.orthographicSize;

            //additionalData.aperture = additional.captureSettings.aperture;
            //additionalData.shutterspeed = additional.captureSettings.shutterspeed;
            //additionalData.iso = additional.captureSettings.iso;

            additionalData.renderingPath = probe.captureSettings.renderingPath;

            SetupFrameSettings(additionalData, probe);

            camera.projectionMatrix = projection;
            camera.worldToCameraMatrix = worldToCamera;

            var ctr = camera.transform;
            ctr.position = capturePosition;
            ctr.rotation = captureRotation;
        }

        static void SetupCameraForRender(Camera camera, HDAdditionalCameraData additionalData, HDAdditionalReflectionData additional)
        {
            Vector3 capturePosition = additional.capturePosition;
            Quaternion captureRotation = Quaternion.identity;
            float nearClipPlane = additional.captureSettings.nearClipPlane;
            float farClipPlane = additional.captureSettings.farClipPlane;
            float aspect = 1f;
            float fov = additional.captureSettings.fieldOfView;

            camera.clearFlags = CameraClearFlags.Nothing;
            camera.backgroundColor = Color.white;
            camera.aspect = 1f;

            additionalData.backgroundColorHDR = additional.captureSettings.backgroundColorHDR;
            additionalData.clearColorMode = additional.captureSettings.clearColorMode;
            additionalData.clearDepth = additional.captureSettings.clearDepth;
            camera.cullingMask = additional.captureSettings.cullingMask;
            additionalData.volumeLayerMask = additional.captureSettings.volumeLayerMask;
            additionalData.volumeAnchorOverride = additional.captureSettings.volumeAnchorOverride;
            camera.useOcclusionCulling = additional.captureSettings.useOcclusionCulling;
            camera.orthographic = additional.captureSettings.projection == CameraProjection.Orthographic;
            camera.farClipPlane = farClipPlane;
            camera.nearClipPlane = nearClipPlane;
            camera.fieldOfView = fov;
            camera.orthographicSize = additional.captureSettings.orthographicSize;

            //additionalData.aperture = additional.captureSettings.aperture;
            //additionalData.shutterspeed = additional.captureSettings.shutterspeed;
            //additionalData.iso = additional.captureSettings.iso;

            additionalData.renderingPath = additional.captureSettings.renderingPath;
            
            SetupFrameSettings(additionalData, additional);

            camera.projectionMatrix = Matrix4x4.Perspective(fov, aspect, nearClipPlane, farClipPlane);
            camera.worldToCameraMatrix = GeometryUtils.CalculateWorldToCameraMatrixRHS(capturePosition, captureRotation);

            var ctr = camera.transform;
            ctr.position = capturePosition;
            ctr.rotation = captureRotation;
        }

        static void SetupFrameSettings(HDAdditionalCameraData additionalData, HDProbe probe)
        {
            HDRenderPipelineAsset hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if (probe.mode == ReflectionProbeMode.Realtime)
            {
                hdrp.GetRealtimeReflectionFrameSettings().CopyTo(additionalData.GetFrameSettings());
            }
            else
            {
                hdrp.GetBakedOrCustomReflectionFrameSettings().CopyTo(additionalData.GetFrameSettings());
            }
            if (probe.captureSettings.renderingPath == HDAdditionalCameraData.RenderingPath.Custom)
                probe.frameSettings.Override(additionalData.GetFrameSettings()).CopyTo(additionalData.GetFrameSettings());
        }

        public static void CalculateCaptureCameraProperties(PlanarReflectionProbe probe, out float nearClipPlane, out float farClipPlane, out float aspect, out float fov, out CameraClearFlags clearFlags, out Color backgroundColor, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation, Camera viewerCamera = null)
        {
            if (viewerCamera != null
                && probe.mode == ReflectionProbeMode.Realtime
                && probe.refreshMode == ReflectionProbeRefreshMode.EveryFrame
                && probe.capturePositionMode == PlanarReflectionProbe.CapturePositionMode.MirrorCamera)
                CalculateMirroredCaptureCameraProperties(probe, viewerCamera, out nearClipPlane, out farClipPlane, out aspect, out fov, out clearFlags, out backgroundColor, out worldToCamera, out projection, out capturePosition, out captureRotation);
            else
                CalculateStaticCaptureCameraProperties(probe, out nearClipPlane, out farClipPlane, out aspect, out fov, out clearFlags, out backgroundColor, out worldToCamera, out projection, out capturePosition, out captureRotation);
        }

        static bool IsProbeCaptureMirrored(PlanarReflectionProbe probe, Camera viewerCamera)
        {
            return viewerCamera != null
                && probe.mode == ReflectionProbeMode.Realtime
                && probe.refreshMode == ReflectionProbeRefreshMode.EveryFrame
                && probe.capturePositionMode == PlanarReflectionProbe.CapturePositionMode.MirrorCamera;
        }

        static void CalculateStaticCaptureCameraProperties(PlanarReflectionProbe probe, out float nearClipPlane, out float farClipPlane, out float aspect, out float fov, out CameraClearFlags clearFlags, out Color backgroundColor, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation)
        {
            nearClipPlane = probe.captureSettings.nearClipPlane;
            farClipPlane = probe.captureSettings.farClipPlane;
            aspect = 1f;
            fov = (probe.captureSettings.overrides & CaptureSettingsOverrides.FieldOfview) > 0
                ? probe.captureSettings.fieldOfView
                : 90f;
            clearFlags = CameraClearFlags.Nothing;
            backgroundColor = Color.white;

            capturePosition = probe.capturePosition;
            captureRotation = Quaternion.LookRotation((Vector3)probe.influenceToWorld.GetColumn(3) - capturePosition, probe.transform.up);

            worldToCamera = GeometryUtils.CalculateWorldToCameraMatrixRHS(capturePosition, captureRotation);
            var clipPlane = GeometryUtils.CameraSpacePlane(worldToCamera, probe.captureMirrorPlanePosition, probe.captureMirrorPlaneNormal);
            projection = Matrix4x4.Perspective(fov, aspect, nearClipPlane, farClipPlane);
            projection = GeometryUtils.CalculateObliqueMatrix(projection, clipPlane);
        }

        static void CalculateMirroredCaptureCameraProperties(PlanarReflectionProbe probe, Camera viewerCamera, out float nearClipPlane, out float farClipPlane, out float aspect, out float fov, out CameraClearFlags clearFlags, out Color backgroundColor, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation)
        {
            nearClipPlane = probe.captureSettings.nearClipPlane;
            farClipPlane = probe.captureSettings.farClipPlane;
            aspect = 1;
            fov = (probe.captureSettings.overrides & CaptureSettingsOverrides.FieldOfview) > 0
                ? probe.captureSettings.fieldOfView
                : Mathf.Max(viewerCamera.fieldOfView, viewerCamera.fieldOfView * viewerCamera.aspect);
            clearFlags = viewerCamera.clearFlags;
            backgroundColor = viewerCamera.backgroundColor;

            var worldToCapture = GeometryUtils.CalculateWorldToCameraMatrixRHS(viewerCamera.transform);
            var reflectionMatrix = GeometryUtils.CalculateReflectionMatrix(probe.captureMirrorPlanePosition, probe.captureMirrorPlaneNormal);
            worldToCamera = worldToCapture * reflectionMatrix;

            var clipPlane = GeometryUtils.CameraSpacePlane(worldToCamera, probe.captureMirrorPlanePosition, probe.captureMirrorPlaneNormal);

            //not supported at the moment
            //if(probe.captureSettings.projection == CameraProjection.Perspective)
            //{
                var sourceProj = Matrix4x4.Perspective(fov, aspect, nearClipPlane, farClipPlane);
                projection = GeometryUtils.CalculateObliqueMatrix(sourceProj, clipPlane);
            //}
            //else
            //{
            //    projection = Matrix4x4.Ortho(probe.captureSettings.orthographicSize, probe.captureSettings.orthographicSize, probe.captureSettings.orthographicSize, probe.captureSettings.orthographicSize, nearClipPlane, farClipPlane);
            //}

            capturePosition = reflectionMatrix.MultiplyPoint(viewerCamera.transform.position);

            var forward = reflectionMatrix.MultiplyVector(viewerCamera.transform.forward);
            var up = reflectionMatrix.MultiplyVector(viewerCamera.transform.up);
            captureRotation = Quaternion.LookRotation(forward, up);
        }

        static Camera GetRenderCamera()
        {
            if (s_RenderCamera == null)
            {
                var go = GameObject.Find("__Probe Render Camera") ?? new GameObject("__Probe Render Camera");
                go.hideFlags = HideFlags.HideAndDontSave;

                s_RenderCamera = go.GetComponent<Camera>();
                if (s_RenderCamera == null || s_RenderCamera.Equals(null))
                    s_RenderCamera = go.AddComponent<Camera>();

                // We need to setup cameraType before adding additional camera
                s_RenderCamera.cameraType = CameraType.Reflection;

                s_RenderCameraData = go.GetComponent<HDAdditionalCameraData>();
                if (s_RenderCameraData == null || s_RenderCameraData.Equals(null))
                    s_RenderCameraData = go.AddComponent<HDAdditionalCameraData>();

                go.SetActive(false);
            }

            return s_RenderCamera;
        }

        static HDAdditionalCameraData GetRenderCameraAdditionalData()
        {
            if (s_RenderCameraData == null)
            {
                GetRenderCamera();
            }

            return s_RenderCameraData;
        }
    }
}
