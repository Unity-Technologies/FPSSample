using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(ReplicatedEntity))]
public class ReplicatedEntityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var replicatedEntity = target as ReplicatedEntity;
        GUILayout.Label("GUID:" + replicatedEntity.Value.assetGuid.GetGuidStr());
    }
}
