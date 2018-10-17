using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class HDAdditionalShadowData
    {
        public static void InitDefaultHDAdditionalShadowData(AdditionalShadowData shadowData)
        {
            // Update bias control for HD

            // bias control default value based on empirical experiment
            shadowData.viewBiasMin          = 0.2f;
            shadowData.viewBiasMax          = 100.0f; // Not used, high value to have no effect
            shadowData.viewBiasScale        = 1.0f;
            shadowData.normalBiasMin        = 0.5f;
            shadowData.normalBiasMax        = 0.5f;
            shadowData.normalBiasScale      = 1.0f;
            shadowData.sampleBiasScale      = false;
            shadowData.edgeLeakFixup        = true;
            shadowData.edgeToleranceNormal  = true;
            shadowData.edgeTolerance        = 1.0f;
        }
    }
}
