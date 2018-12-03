using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroType", menuName = "FPS Sample/Hero/HeroType")]
public class HeroTypeAsset : ScriptableObjectRegistryEntry
{
    [Serializable]
    public class ItemEntry
    {
        public ItemTypeDefinition itemType;
    }

    [Serializable]
    public class SprintCameraSettings
    {
        public float FOVFactor = 0.93f; 
        public float FOVInceraetSpeed = 1.0f;
        public float FOVDecreaseSpeed = 0.2f;
    }
    
    public float health = 100;
    public SprintCameraSettings sprintCameraSettings = new SprintCameraSettings();
    public float eyeHeight = 1.8f;
    public CharacterMoveQuery.Settings characterMovementSettings;
    
    public ReplicatedEntityFactory behaviorsController;
    
    public CharacterTypeDefinition character;
    public ItemEntry[] items;
}

#if UNITY_EDITOR
[CustomEditor(typeof(HeroTypeAsset))]
public class HeroTypeAssetEditor : ScriptableObjectRegistryEntryEditor<HeroTypeRegistry, HeroTypeAsset>
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawDefaultInspector();
    }
}
#endif