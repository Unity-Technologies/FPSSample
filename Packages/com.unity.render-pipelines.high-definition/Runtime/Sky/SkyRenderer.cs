namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public abstract class SkyRenderer
    {
        public abstract void Build();
        public abstract void Cleanup();
        public abstract void SetRenderTargets(BuiltinSkyParameters builtinParams);
        // renderForCubemap: When rendering into a cube map, no depth buffer is available so user has to make sure not to use depth testing or the depth texture.
        public abstract void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk);
        public abstract bool IsValid();

        protected float GetExposure(SkySettings skySettings, DebugDisplaySettings debugSettings)
        {
            float debugExposure = 0.0f;
            if (debugSettings != null && debugSettings.DebugNeedsExposure())
            {
                debugExposure = debugSettings.lightingDebugSettings.debugExposure;
            }
            return skySettings.exposure + debugExposure;
        }
    }
}
