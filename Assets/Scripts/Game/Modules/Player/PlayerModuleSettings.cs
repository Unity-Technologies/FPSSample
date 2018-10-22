using UnityEngine;

[CreateAssetMenu(fileName = "PlayerModuleSettings", menuName = "FPS Sample/Player/PlayerSystemSettings")]
public class PlayerModuleSettings : ScriptableObject
{
    public WeakAssetReference playerStatePrefab;
}
