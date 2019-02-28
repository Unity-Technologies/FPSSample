using UnityEngine;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [DisallowMultipleComponent, ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class HDRayTracingFilter : MonoBehaviour
    {
        [HideInInspector]
        const int currentVersion = 1;

#if ENABLE_RAYTRACING
        // Culling mask that defines the layers that this acceleration structure should handle
        public LayerMask layermask = -1;
    #endif
    }
}
