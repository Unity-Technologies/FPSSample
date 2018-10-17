using UnityEngine;

namespace Unity.Entities
{
#if !UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP
    static class AutomaticWorldBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            DefaultWorldInitialization.Initialize("Default World", false);
        }
    }
#endif
}
