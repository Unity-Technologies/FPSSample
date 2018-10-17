using UnityEditor;

// TO USE: Drop this in a folder called Editor somewhere.
// Then Replace FooScript in code with whatever you want to remove

static public class RemoveProjectLODLightmaps
{
    [MenuItem("Tools/RemoveProjectLODLightmaps")]
    static public void RemoveComponents()
    {
        for (var s = 0; s < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; s++)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(s);
            foreach (var o in scene.GetRootGameObjects())
            {
                var comps = o.GetComponentsInChildren<ProjectLODLightmaps>();
                foreach (var c in comps)
                {
                    var go = c.gameObject;
                    Undo.DestroyObjectImmediate(c);
                    EditorUtility.SetDirty(go);
                }
            }
        }
    }
}