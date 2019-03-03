using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedHDRenderPipelineAsset
    {
        public SerializedObject serializedObject;

        public SerializedProperty renderPipelineResources;
        public SerializedProperty diffusionProfileSettings;
        public SerializedProperty allowShaderVariantStripping;
        public SerializedProperty enableSRPBatcher;
        public SerializedProperty enableVariantStrippingLog; 
        public SerializedRenderPipelineSettings renderPipelineSettings;
        public SerializedFrameSettings defaultFrameSettings;
        public SerializedFrameSettings defaultBakedOrCustomReflectionFrameSettings;
        public SerializedFrameSettings defaultRealtimeReflectionFrameSettings;

        public SerializedHDRenderPipelineAsset(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            renderPipelineResources = serializedObject.FindProperty("m_RenderPipelineResources");
            diffusionProfileSettings = serializedObject.Find((HDRenderPipelineAsset s) => s.diffusionProfileSettings);
            allowShaderVariantStripping = serializedObject.Find((HDRenderPipelineAsset s) => s.allowShaderVariantStripping);
            enableSRPBatcher = serializedObject.Find((HDRenderPipelineAsset s) => s.enableSRPBatcher);
            enableVariantStrippingLog = serializedObject.Find((HDRenderPipelineAsset s) => s.enableVariantStrippingLog);

            renderPipelineSettings = new SerializedRenderPipelineSettings(serializedObject.Find((HDRenderPipelineAsset a) => a.renderPipelineSettings));
            defaultFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_FrameSettings"));
            defaultBakedOrCustomReflectionFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_BakedOrCustomReflectionFrameSettings"));
            defaultRealtimeReflectionFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_RealtimeReflectionFrameSettings"));
        }

        public void Update()
        {
            serializedObject.Update();
        }

        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
