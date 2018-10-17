using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedGlobalDecalSettings
    {
        public SerializedProperty root;

        public SerializedProperty drawDistance;
        public SerializedProperty atlasWidth;
        public SerializedProperty atlasHeight;
        public SerializedProperty perChannelMask;

        public SerializedGlobalDecalSettings(SerializedProperty root)
        {
            this.root = root;

            drawDistance = root.Find((GlobalDecalSettings s) => s.drawDistance);
            atlasWidth = root.Find((GlobalDecalSettings s) => s.atlasWidth);
            atlasHeight = root.Find((GlobalDecalSettings s) => s.atlasHeight);
            perChannelMask = root.Find((GlobalDecalSettings s) => s.perChannelMask);
        }
    }
}
