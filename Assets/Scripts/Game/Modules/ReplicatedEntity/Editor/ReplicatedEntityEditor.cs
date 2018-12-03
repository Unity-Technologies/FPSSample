using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;


[CustomEditor(typeof(ReplicatedEntity))]
public class ReplicatedEntityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var registry = ReplicatedEntityRegistry.GetReplicatedEntityRegistry();
        if (registry == null)
        {
            EditorGUILayout.HelpBox("Make sure you have a ReplicatedEntityRegistry in project", MessageType.Error);
            return;
        }

        var replicatedEntity = target as ReplicatedEntity;

        var guid = "";
        var stage = PrefabStageUtility.GetPrefabStage(replicatedEntity.gameObject);
        if (stage != null)
        {
            guid = AssetDatabase.AssetPathToGUID(stage.prefabAssetPath);
        }
        else
        {
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(replicatedEntity.gameObject) && !Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Replicated entity must be placed on root of prefab", MessageType.Error);
                return;
            }

            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(replicatedEntity.gameObject);
            guid = AssetDatabase.AssetPathToGUID(path);
        }

        var registryIndex = registry != null ? registry.GetId(guid) : -1;
        
        GUILayout.Label("Entity registry index:" + replicatedEntity.registryId);

        if (Application.isPlaying)
            return;
        
        GUILayout.Label("Registry index:" + registryIndex);

        if (registryIndex != replicatedEntity.registryId)
            EditorGUILayout.HelpBox("Local index does not match client registry index", MessageType.Error);

        if (registryIndex != -1 || replicatedEntity.registryId != -1)
        {
            if (GUILayout.Button("Unregister"))
            {
                if (registryIndex != -1)
                    registry.ClearAtId(registryIndex);
                replicatedEntity.registryId = -1;

                PrefabUtility.SaveAsPrefabAsset(replicatedEntity.gameObject, stage.prefabAssetPath);
//                EditorUtility.SetDirty(replicatedEntity);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("NOT REGISTERED!", MessageType.Error);
            
            if (GUILayout.Button("Register"))
            {
                registryIndex = registry.FindFreeId();


                registry.SetPrefab(registryIndex, guid);
                
                replicatedEntity.registryId = registryIndex;
                PrefabUtility.SaveAsPrefabAsset(replicatedEntity.gameObject, stage.prefabAssetPath);
//                EditorUtility.SetDirty(replicatedEntity);
            }
        }
    }
}
