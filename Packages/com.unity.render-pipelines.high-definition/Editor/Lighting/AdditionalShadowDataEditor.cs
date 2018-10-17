using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    // Disable HDRP custom editor to display full shadow settings (only for dev purpose, reset for pr)
    [CustomEditor(typeof(AdditionalShadowData))]
    class AdditionalShadowDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }
    }
}
