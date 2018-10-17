namespace UnityEngine.Experimental.Rendering
{
    public class DebugUpdater : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RuntimeInit()
        {
            if (FindObjectOfType<DebugUpdater>() != null)
                return;

            var go = new GameObject { name = "[Debug Updater]" };
            go.AddComponent<DebugUpdater>();
            DontDestroyOnLoad(go);
        }

        void Update()
        {
            DebugManager.instance.UpdateActions();

            if (DebugManager.instance.GetAction(DebugAction.EnableDebugMenu) != 0.0f)
                DebugManager.instance.displayRuntimeUI = !DebugManager.instance.displayRuntimeUI;
        }
    }
}
