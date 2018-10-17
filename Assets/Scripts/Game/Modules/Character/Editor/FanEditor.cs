using UnityEditor;

// This is a hack to work around issue with importer inspector, that does not deal well with adding/removing components at import time

[CustomEditor(typeof(Fan))]
public class FanEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}