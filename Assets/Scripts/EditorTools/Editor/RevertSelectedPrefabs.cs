using UnityEditor;

public class RevertSelectedPrefabs
{
    [MenuItem("FPS Sample/Revert Selected Prefabs")]
    static void Execute()
    {
        foreach(var gameObject in Selection.gameObjects)
            PrefabUtility.RevertPrefabInstance(gameObject);
    }
}