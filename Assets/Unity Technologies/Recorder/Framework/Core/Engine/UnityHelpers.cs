using Object = UnityEngine.Object;

namespace UnityEngine.Recorder
{

    /// <summary>
    /// What is this: 
    /// Motivation  : 
    /// Notes: 
    /// </summary>    
    public static class UnityHelpers
    {
        public static void Destroy(Object obj, bool allowDestroyingAssets = false)
        {
            if (obj == null)
                return;
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj, allowDestroyingAssets);
#else
            Object.Destroy(obj);
#endif
            obj = null;
        }

        public static bool IsPlaying()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorApplication.isPlaying;
#else
            return true;
#endif
        }
    }
}
