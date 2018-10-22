using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;

public class ScenePostProcessor
{
    // TODO : At some point we need to set these when building different configs
    public static LevelManager.BuildType buildType = LevelManager.BuildType.Default;
    public static bool isDevelopmentBuild = false;

    [PostProcessScene(0)]
    public static void OnPostProcessScene()
    {
        // Only strip if building levels or if in preview mode in Editor
        if(BuildPipeline.isBuildingPlayer || Game.game == null)
        {
            LevelManager.StripCode(buildType, isDevelopmentBuild);
        }

        // In editor, we inject the game object to ensure preview mode works
        var info = EditorLevelManager.GetLevelInfoFor(EditorSceneManager.GetSceneAt(0).path);
        if(!BuildPipeline.isBuildingPlayer && Game.game == null && info != null && info.levelType != LevelInfo.LevelType.Generic)
        {
            var gamePrefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Game.prefab", typeof(GameObject));
            PrefabUtility.InstantiatePrefab(gamePrefab);
        }

        AddBuildInfo();
    }

    static void AddBuildInfo()
    {
        // Only if building player and only for bootstrapper scene
        if(BuildPipeline.isBuildingPlayer && EditorSceneManager.GetActiveScene().buildIndex == 0)
        {
            var gameObject = new GameObject("BuildInfo");
            var buildInfo = gameObject.AddComponent<BuildInfo>();
            buildInfo.buildId = System.Environment.GetEnvironmentVariable("BUILD_ID", System.EnvironmentVariableTarget.Process);
        }
    }

}
