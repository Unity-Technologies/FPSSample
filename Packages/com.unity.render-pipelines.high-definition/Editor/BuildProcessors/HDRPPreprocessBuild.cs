using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class HDRPPreprocessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            // Don't execute the preprocess if we are not HDRenderPipeline
            HDRenderPipelineAsset hdPipelineAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if (hdPipelineAsset == null)
                return;

            // If platform is supported all good
            if (HDUtils.IsSupportedBuildTarget(report.summary.platform) && HDUtils.IsOperatingSystemSupported(SystemInfo.operatingSystem))
                return;

            string msg = "The platform " + report.summary.platform.ToString() + " is not supported with High Definition Render Pipeline";

            // Throw an exception to stop the build
            throw new BuildFailedException(msg);
        }
    }
}
