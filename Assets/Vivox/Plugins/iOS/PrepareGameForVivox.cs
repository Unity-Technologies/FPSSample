
using System.Runtime.InteropServices;
using UnityEngine;

namespace Vivox.Unity 
{
#if UNITY_IOS
    class Prepare
    {
        [DllImport("__Internal")]
        private static extern void PrepareForVivox();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad() 
        {
            // Hack not sure why If UNITY_IOS  && !UNITY_EDITOR is not working here but this is the temporary solution 
            if (Application.platform != RuntimePlatform.IPhonePlayer) return;
            PrepareForVivox();
        }
    }
#endif
}