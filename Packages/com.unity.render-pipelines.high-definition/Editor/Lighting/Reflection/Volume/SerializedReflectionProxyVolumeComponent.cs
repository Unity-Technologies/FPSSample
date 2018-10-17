using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class SerializedReflectionProxyVolumeComponent
    {
        public SerializedObject serializedObject;

        public SerializedProxyVolume proxyVolume;

        public SerializedReflectionProxyVolumeComponent(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            proxyVolume = new SerializedProxyVolume(serializedObject.Find((ReflectionProxyVolumeComponent c) => c.proxyVolume));
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
