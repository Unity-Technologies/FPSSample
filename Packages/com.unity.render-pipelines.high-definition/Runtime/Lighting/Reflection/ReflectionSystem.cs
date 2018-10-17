using UnityEngine.Experimental.Rendering.HDPipeline.Internal;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public static class ReflectionSystem
    {
        static ReflectionSystemInternal s_Instance = new ReflectionSystemInternal(ReflectionSystemParameters.Default, null);

        public static void SetParameters(ReflectionSystemParameters parameters)
        {
            s_Instance = new ReflectionSystemInternal(parameters, s_Instance);
        }

        public static void RegisterProbe(PlanarReflectionProbe planarProbe)
        {
            s_Instance.RegisterProbe(planarProbe);
        }

        public static void RegisterProbe(HDAdditionalReflectionData probe)
        {
            s_Instance.RegisterProbe(probe);
        }

        public static void UnregisterProbe(PlanarReflectionProbe planarProbe)
        {
            s_Instance.UnregisterProbe(planarProbe);
        }

        public static void UnregisterProbe(HDAdditionalReflectionData probe)
        {
            s_Instance.UnregisterProbe(probe);
        }

        public static void RequestRealtimeRender(PlanarReflectionProbe probe)
        {
            s_Instance.RequestRealtimeRender(probe);
        }

        public static void RenderAllRealtimeProbes()
        {
            s_Instance.RenderAllRealtimeProbes();
        }

        public static RenderTexture NewRenderTarget(PlanarReflectionProbe probe)
        {
            return s_Instance.NewRenderTarget(probe);
        }

        public static void Render(PlanarReflectionProbe probe, RenderTexture target)
        {
            s_Instance.Render(probe, target);
        }

        public static void PrepareCull(Camera camera, ReflectionProbeCullResults results)
        {
            s_Instance.PrepareCull(camera, results);
        }

        public static void RenderAllRealtimeViewerDependentProbesFor(ReflectionProbeType probeType, Camera camera)
        {
            s_Instance.RenderAllRealtimeProbesFor(probeType, camera);
        }

        public static void CalculateCaptureCameraProperties(PlanarReflectionProbe probe, out float nearClipPlane, out float farClipPlane, out float aspect, out float fov, out CameraClearFlags clearFlags, out Color backgroundColor, out Matrix4x4 worldToCamera, out Matrix4x4 projection, out Vector3 capturePosition, out Quaternion captureRotation, Camera viewerCamera = null)
        {
            ReflectionSystemInternal.CalculateCaptureCameraProperties(
                probe,
                out nearClipPlane, out farClipPlane,
                out aspect, out fov, out clearFlags, out backgroundColor,
                out worldToCamera, out projection, out capturePosition, out captureRotation,
                viewerCamera);
        }
    }
}
