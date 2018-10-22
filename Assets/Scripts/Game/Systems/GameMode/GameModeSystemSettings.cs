using UnityEngine;

[CreateAssetMenu(fileName = "GameModeSystemSettings", menuName = "FPS Sample/GameMode/GameModeSystemSettings")]
public class GameModeSystemSettings : ScriptableObject
{
    public WeakAssetReference gameModePrefab;
    public WeakAssetReference teamObjectStatePrefab;
}
