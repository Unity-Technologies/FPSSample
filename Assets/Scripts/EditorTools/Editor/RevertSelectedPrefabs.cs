using UnityEditor;

public class RevertSelectedPrefabs
{
    [MenuItem("fps.sample/Revert Selected Prefabs")]
    static void Execute()
    {
        foreach(var gameObject in Selection.gameObjects)
            PrefabUtility.RevertPrefabInstance(gameObject);
    }
}