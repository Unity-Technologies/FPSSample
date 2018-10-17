using UnityEngine;

[CreateAssetMenu(fileName = "GameModeSystemSettings", menuName = "SampleGame/GameMode/GameModeSystemSettings")]
public class GameModeSystemSettings : ScriptableObject
{
    public WeakAssetReference gameModePrefab;
    public WeakAssetReference teamObjectStatePrefab;
}
