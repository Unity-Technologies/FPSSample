using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class EditorLevelManager
{
    static EditorLevelManager()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnUpdate;
    }

    public static bool IsLayeredLevel(string path)
    {
        return path.EndsWith("_Main.unity");
    }

    public static string[] GetLevelLayers(string path)
    {
        var filename = Path.GetFileName(path);
        var dir = Path.GetDirectoryName(path);
        var name = filename.Substring(0, filename.Length - 11);

        var layerPaths = new List<string>();
        for (var i = 0; i < LevelManager.layerNames.Length; ++i)
        {
            var layerPath = Path.Combine(dir, name + "_" + LevelManager.layerNames[i] + ".unity");
            if (File.Exists(layerPath))
                layerPaths.Add(layerPath.ToLower());
        }

        return layerPaths.ToArray();
    }

    public static void StartGameInEditor(string args)
    {
        // If editor already running we just process arguments
        if (EditorApplication.isPlaying)
        {
            Console.ProcessCommandLineArguments(args.Split(' '));
            return;
        }

        // Store command in playerprefs that will be consumed when playmode starts
        var count = PlayerPrefs.GetInt("CustomStartupCommandCount",0);
        PlayerPrefs.SetString(string.Format("CustomStartupCommand{0}",count), args);
        count++;
        PlayerPrefs.SetInt("CustomStartupCommandCount", count);
        EditorApplication.isPlaying = true;
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (mode == OpenSceneMode.Single)
        {
            var path = scene.path;    // Native call and string allocation
            if (IsLayeredLevel(path))
            {
                var layers = GetLevelLayers(path);
                foreach (var layer in layers)
                    EditorSceneManager.OpenScene(layer, OpenSceneMode.Additive);
            }
        }
    }

    static void OnSceneClosed(Scene scene)
    {
        Debug.Log("Scene closed : " + scene.name);
    }

    static void OnPlayModeStateChanged(PlayModeStateChange mode)
    {
        if (mode == PlayModeStateChange.EnteredPlayMode)
        {
            var startCommandCount = PlayerPrefs.GetInt("CustomStartupCommandCount", 0);
            if(startCommandCount > 0)
            {
                if (Game.game == null)
                    SceneManager.LoadScene(0);

                for (int i=0;i< startCommandCount;i++)
                {
                    var key = string.Format("CustomStartupCommand{0}", i);
                    var args = PlayerPrefs.GetString(key, "");
                    Console.ProcessCommandLineArguments(args.Split(' '));
                    PlayerPrefs.DeleteKey(key);
                }

                PlayerPrefs.SetInt("CustomStartupCommandCount", 0);
            }
            else
            {
                // User pressed editor start button
                var info = GetLevelInfoFor(EditorSceneManager.GetSceneAt(0).path);
                if (info != null)
                {
                    switch(info.levelType)
                    {
                        case LevelInfo.LevelType.Generic:
                            break;
                        case LevelInfo.LevelType.Gameplay:
                            //Console.EnqueueCommandNoHistory("preview");
                            Game.game.RequestGameLoop( typeof(PreviewGameLoop), new string[0]);
                            break;
                        case LevelInfo.LevelType.Menu:
                            Console.SetOpen(false);
                            //Console.EnqueueCommandNoHistory("menu");
                            break;
                    }
                }
            }
        }
    }

    static void OnUpdate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (EditorApplication.isPlaying && EditorApplication.isCompiling)
            {
                Debug.Log("Stopped play mode because compilation started.");
                EditorApplication.isPlaying = false;
            }
        }
    }

    public static LevelInfo GetLevelInfoFor(string scenePath)
    {
        foreach(var levelInfo in BuildTools.LoadLevelInfos())
        {
            if(AssetDatabase.GetAssetPath(levelInfo.main_scene) == scenePath)
            {
                return levelInfo;
            }
        }
        return null;
    }
}

