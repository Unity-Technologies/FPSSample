using UnityEngine;

[CreateAssetMenu(fileName = "PlayerModuleSettings", menuName = "SampleGame/Player/PlayerSystemSettings")]
public class PlayerModuleSettings : ScriptableObject
{
    public WeakAssetReference playerStatePrefab;
}
