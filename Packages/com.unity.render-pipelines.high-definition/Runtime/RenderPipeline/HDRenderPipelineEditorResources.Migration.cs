#if UNITY_EDITOR //file must be in realtime assembly folder to be found in HDRPAsset
using System;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class HDRenderPipelineEditorResources
    {
        enum Version
        {
            None
        }

        [HideInInspector, SerializeField]
        Version m_Version;

        //Note: nothing to migrate at the moment.
        // If any, it must be done at deserialisation time on this component due to lazy init and disk access conflict when rebuilding library folder
    }
}
#endif
