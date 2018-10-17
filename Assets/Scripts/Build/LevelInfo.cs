using UnityEngine;

[CreateAssetMenu(fileName = "LevelInfo", menuName = "SampleGame/Level/LevelInfo")]
public class LevelInfo : ScriptableObject
{
    public enum LevelType
    {
        Generic,
        Gameplay,
        Menu
    }

    public Object main_scene;

    [Tooltip("The leveltype determines e.g. what happens when you hit play in editor")]
    public LevelType levelType = LevelType.Gameplay;
}
