using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Profiling;

public class BuildWindow : EditorWindow
{
    enum QuickstartMode
    {
        Singleplayer,
        Multiplayer,
    }

    enum GameLoopMode
    {
        Serve,
        Client,
        Preview,
        Undefined,
    }

    enum EditorRole
    {
        Unused,
        Client,
        Server,
        Mixed,
    }

    [Serializable]
    class QuickstartData
    {
        public QuickstartMode mode = QuickstartMode.Multiplayer;
        public int levelIndex = 0;
        public EditorRole editorRole;
        public int clientCount = 1;
        public bool headlessServer = true;
        public string defaultArguments = "";
        public List<QuickstartEntry> entries = new List<QuickstartEntry>();
    }


    [Serializable]
    class QuickstartEntry
    {
        public GameLoopMode gameLoopMode = GameLoopMode.Client;

        public bool runInEditor;
        public bool headless;

        //        public Vector2 windowPos;
        //        public bool useWindowPos;

        public string GetArguments(string levelname, string defaultArguments)
        {
            var arguments = "";

            switch (gameLoopMode)
            {
                case GameLoopMode.Serve:
                    arguments += " +serve " + levelname;
                    break;
                case GameLoopMode.Client:
                    arguments += " +client 127.0.0.1 ";
                    break;
                case GameLoopMode.Preview:
                    arguments += " +preview " + levelname;
                    break;
            }

            if (headless)
                arguments += " -batchmode -nographics";

            arguments += " " + defaultArguments;
            return arguments;
        }
    }


    enum BuildAction
    {
        None,
        BuildBundles,
        ForceBuildBundles,
        StartBuild,
        BuildAndRun,
        Run,
        OpenBuildFolder,
    }

    const string quickStartDataKey = "QuickStartData";

    private void OnEnable()
    {
        var str = EditorPrefs.GetString(quickStartDataKey, "");
        if (str != "")
            quickstartData = JsonUtility.FromJson<QuickstartData>(str);
        else
            quickstartData = new QuickstartData();
    }

    [MenuItem("FPS Sample/Windows/Project Tools")]
    public static void ShowWindow()
    {
        GetWindow<BuildWindow>(false, "Project Tools", true);
    }

    void OnGUI()
    {
        Profiler.BeginSample("BuildWindow.OnGUI");

        // We keep levelinfos in a member variable. This forces a reference to the LevelInfos. Otherwise they will be nuked by OpenScene()
        if (m_LevelInfos == null)
            m_LevelInfos = BuildTools.LoadLevelInfos();

        // Verify levelinfos
        bool loadLevelInfos = false;
        foreach (var levelInfo in m_LevelInfos)
        {
            if (levelInfo == null)
            {
                loadLevelInfos = true;
                break;
            }
        }
        if(loadLevelInfos)
            m_LevelInfos = BuildTools.LoadLevelInfos();

        
        
        m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos);

        GUILayout.Label("Project", EditorStyles.boldLabel);

        GUILayout.TextArea(Application.dataPath.BeforeLast("Assets"));

        DrawLevelSelect();

        GUILayout.Space(10.0f);

        DrawBuildTools();

        GUILayout.Space(10.0f);

        DrawQuickStart();

        GUILayout.EndScrollView();

        Profiler.EndSample();
    }

    void DrawLevelSelect()
    {
        Profiler.BeginSample("DrawLevelSelect");

        GUILayout.Label("Levels", EditorStyles.boldLabel);
        GUILayout.BeginVertical(EditorStyles.textArea);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Bootstrapper", GUILayout.ExpandWidth(true));
        GUILayout.BeginHorizontal(GUILayout.Width(100));
        if (GUILayout.Button("Open"))
        {
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                var scene = UnityEditor.EditorBuildSettings.scenes[0];
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene.path, UnityEditor.SceneManagement.OpenSceneMode.Single);
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.EndHorizontal();

        
        
        
        
        
        
        LevelInfo openLevel = null;
        foreach (var levelInfo in m_LevelInfos)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(levelInfo.name, GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal(GUILayout.Width(100));
            if (GUILayout.Button("Open"))
            {
                if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    openLevel = levelInfo;
                }
            }
            if (GUILayout.Button("Serve"))
            {
                RunBuild("+serve " + levelInfo.name + " -batchmode -nographics");
            }
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        Profiler.EndSample();

        if (openLevel != null)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(openLevel.main_scene), UnityEditor.SceneManagement.OpenSceneMode.Single);
        }
    }

    static string GetBuildPath(BuildTarget buildTarget)
    {
        if (buildTarget == BuildTarget.PS4)
            return "AutoBuildPS4";
        else
            return "AutoBuild";
    }

    static string GetBuildExeName(BuildTarget buildTarget)
    {
        if (buildTarget == BuildTarget.PS4)
            return "AutoBuild";
        else
            return "AutoBuild.exe";
    }

    static string GetBuildExe(BuildTarget buildTarget)
    {
        if (buildTarget == BuildTarget.PS4)
            return "AutoBuild/AutoBuild.bat";
        else
            return "AutoBuild.exe";
    }

    static string GetBundlePath(BuildTarget buildTarget)
    {
        // On PS4 we copy to "Assets/StreamingAssets" later
        return GetBuildPath(buildTarget);
    }

    static bool s_SingleLevelBuilding = false;
    static bool s_ForceBuildBundles = true;
    void DrawBuildTools()
    {
        var action = BuildAction.None;

        GUILayout.Label("Bundles (" + PrettyPrintTimeStamp(TimeLastBuildBundles()) + ")", EditorStyles.boldLabel);

        var buildBundledLevels = false;
        var buildBundledAssets = false;
        List<LevelInfo> buildOnlyLevels = null;

        GUILayout.BeginHorizontal();
        s_SingleLevelBuilding = EditorGUILayout.Toggle("Single level building", s_SingleLevelBuilding);
        
        // TODO (mogensh) We always force bundle build until we are sure non-forced works         
        // s_ForceBuildBundles = EditorGUILayout.Toggle("Force Build Bundles", s_ForceBuildBundles);

        GUILayout.EndHorizontal();

        if (s_SingleLevelBuilding)
        {
            GUILayout.BeginVertical();
            foreach (var l in m_LevelInfos)
            {
                if (GUILayout.Button("Build only: " + l.name + (s_ForceBuildBundles ? " [force]" : "")))
                {
                    buildBundledLevels = true;
                    buildOnlyLevels = new List<LevelInfo>();
                    buildOnlyLevels.Add(l);
                    break;
                }
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Levels" + (s_ForceBuildBundles ? " [force]" : "")))
        {
            buildBundledLevels = true;
        }
        if (GUILayout.Button("Assets" + (s_ForceBuildBundles ? " [force]" : "")))
        {
            buildBundledAssets = true;
        }
        if (GUILayout.Button("All" + (s_ForceBuildBundles ? " [force]" : "")))
        {
            buildBundledLevels = true;
            buildBundledAssets = true;
        }
        GUILayout.EndHorizontal();

        var buildTarget = EditorUserBuildSettings.activeBuildTarget;    // BuildTarget.StandaloneWindows64
        if (buildBundledLevels || buildBundledAssets)
        {
            BuildTools.BuildBundles(GetBundlePath(buildTarget), buildTarget, buildBundledAssets, buildBundledLevels, s_ForceBuildBundles, buildOnlyLevels);
            if (buildTarget == BuildTarget.PS4)
            {
                // Copy the asset bundles into the PS4 game folder too
                var bundlePathSrc = GetBundlePath(buildTarget) + "/" + SimpleBundleManager.assetBundleFolder;
                var bundlePathDst = GetBuildPath(buildTarget) + "/" + GetBuildExeName(buildTarget) + "/Media/StreamingAssets/" + SimpleBundleManager.assetBundleFolder;
                BuildTools.CopyDirectory(bundlePathSrc, bundlePathDst);
            }
            GUIUtility.ExitGUI();
        }

        GUILayout.Space(10.0f);
        GUILayout.Label("Game (" + PrettyPrintTimeStamp(TimeLastBuildGame()) + ")", EditorStyles.boldLabel);
        GUILayout.Space(1.0f);

        GUILayout.Label("Building for: " + buildTarget.ToString() + " use normal build window to change.", GUILayout.ExpandWidth(false));

        GUILayout.BeginHorizontal();
        m_BuildDevelopment = EditorGUILayout.Toggle("Development build", m_BuildDevelopment);
        GUI.enabled = m_BuildDevelopment;
        m_ConnectProfiler = EditorGUILayout.Toggle("Connect profiler", m_ConnectProfiler);
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        m_IL2CPP = EditorGUILayout.Toggle("IL2CPP", m_IL2CPP);
        m_AllowDebugging = EditorGUILayout.Toggle("Allow debugging", m_AllowDebugging);

        var m_RunArguments = EditorPrefTextField("Arguments", "RunArguments");

        GUILayout.BeginHorizontal();
        var buildGame = false;
        var buildOnlyScripts = false;
        if (GUILayout.Button("Build game"))
        {
            buildGame = true;
        }
        if (GUILayout.Button("Build ONLY scripts"))
        {
            buildOnlyScripts = true;
        }
        if (buildGame || buildOnlyScripts)
        {
            StopAll();

            var buildOptions = m_AllowDebugging ? BuildOptions.AllowDebugging : BuildOptions.None;
            if (buildOnlyScripts)
                buildOptions |= BuildOptions.BuildScriptsOnly;

            if (m_BuildDevelopment)
            {
                buildOptions |= BuildOptions.Development;
                if (m_ConnectProfiler)
                    buildOptions |= BuildOptions.ConnectWithProfiler;
            }

            BuildTools.BuildGame(GetBuildPath(buildTarget), GetBuildExeName(buildTarget), buildTarget, buildOptions, "AutoBuild", m_IL2CPP);

            if (action == BuildAction.BuildAndRun)
                RunBuild("");
            GUIUtility.ExitGUI(); // prevent warnings from gui about unmatched layouts
        }
        if (GUILayout.Button("Run"))
        {
            RunBuild(m_RunArguments);
            GUIUtility.ExitGUI(); // prevent warnings from gui about unmatched layouts
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        var path = Application.dataPath.BeforeLast("Assets") + GetBuildPath(buildTarget);
        var windowsPath = path.Replace("/", "\\");
        if (GUILayout.Button("Open build folder"))
        {
            if (Directory.Exists(windowsPath))
            {
                var p = new System.Diagnostics.Process();
                p.StartInfo = new System.Diagnostics.ProcessStartInfo("explorer.exe", windowsPath);
                p.Start();
            }
            else
            {
                EditorUtility.DisplayDialog("Folder missing", string.Format("Folder {0} doesn't exist yet", windowsPath), "Ok");
            }
        }
        GUILayout.EndHorizontal();
    }

    void DrawQuickStart()
    {
        Profiler.BeginSample("DrawQuickStartMenu");

        GUILayout.BeginVertical();

        var defaultGUIBackgrounColor = GUI.backgroundColor;

        if (m_LevelInfos.Count == 0)
        {
            GUILayout.Label("Quick Start Disabled. No scenes defined");
            return;
        }

        GUILayout.Label("Quick Start", EditorStyles.boldLabel);

        var entryCount = quickstartData.mode != QuickstartMode.Singleplayer ? quickstartData.clientCount + 1 : 1;
        quickstartData.levelIndex = Math.Min(quickstartData.levelIndex, m_LevelInfos.Count - 1);
        var levelInfo = m_LevelInfos[quickstartData.levelIndex];

        // Make sure we have enough entries
        var minEntryCount = math.max(entryCount, 2);
        while (minEntryCount > quickstartData.entries.Count())
            quickstartData.entries.Add(new QuickstartEntry());

        var str = m_LevelInfos[quickstartData.levelIndex].name + " - ";

        str += "Server";
        if (quickstartData.editorRole == EditorRole.Server)
        {
            str += "(Editor)";
        }
        else
        {
            str += quickstartData.entries[0].headless ? "(Headless)" : "";
        }

        str += " & " + quickstartData.clientCount + " clients";
        if (quickstartData.editorRole == EditorRole.Client)
        {
            str += "(1 in editor)";
        }

        GUILayout.Label(str, EditorStyles.boldLabel);

        // Quick start buttons
        GUILayout.BeginHorizontal();
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Start"))
            {
                for (var i = 0; i < entryCount; i++)
                {
                    StartEntry(quickstartData.entries[i], levelInfo.name, quickstartData.defaultArguments);
                }
            }
            GUI.backgroundColor = defaultGUIBackgrounColor;

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Stop All"))
            {
                StopAll();
            }
            GUI.backgroundColor = defaultGUIBackgrounColor;
        }
        GUILayout.EndHorizontal();

        // Settings
        EditorGUI.BeginChangeCheck();

        quickstartData.mode = (QuickstartMode)EditorGUILayout.EnumPopup("Mode", quickstartData.mode);

        var levelNames = m_LevelInfos.Select(item => item.name).ToArray();
        quickstartData.levelIndex = EditorGUILayout.Popup("Level", quickstartData.levelIndex, levelNames);

        GUI.enabled = quickstartData.mode != QuickstartMode.Singleplayer;
        quickstartData.clientCount = EditorGUILayout.IntField("Clients", quickstartData.clientCount);
        quickstartData.headlessServer = EditorGUILayout.Toggle("Headless server", quickstartData.headlessServer);
        GUI.enabled = true;

        quickstartData.editorRole = (EditorRole)EditorGUILayout.EnumPopup("Use Editor as", quickstartData.editorRole);

        quickstartData.defaultArguments = EditorGUILayout.TextField("Default args", quickstartData.defaultArguments);


        quickstartData.entries[0].gameLoopMode = quickstartData.mode == QuickstartMode.Singleplayer ? GameLoopMode.Preview : GameLoopMode.Serve;
        quickstartData.entries[0].headless = quickstartData.headlessServer;

        quickstartData.entries[0].runInEditor = quickstartData.editorRole == EditorRole.Server || quickstartData.editorRole == EditorRole.Mixed;
        quickstartData.entries[1].runInEditor = quickstartData.editorRole == EditorRole.Client || quickstartData.editorRole == EditorRole.Mixed;

        for (var i = 1; i < entryCount; i++)
        {
            quickstartData.entries[i].gameLoopMode = GameLoopMode.Client;
            quickstartData.entries[i].headless = false;
        }

        // Make sure only one run in editor
        var runInEditorCount = 0;
        for (var i = 0; i < entryCount; i++)
        {
            if (quickstartData.entries[i].runInEditor)
            {
                runInEditorCount++;
                quickstartData.entries[i].runInEditor = runInEditorCount <= 1;
            }
        }

        // Draw entries
        GUILayout.Label("Started processes:");
        for (var i = 0; i < entryCount; i++)
        {
            var entry = quickstartData.entries[i];
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(10);

                    GUILayout.Label(entry.runInEditor ? "Editor" : "S.Alone", GUILayout.Width(50));

                    EditorGUILayout.SelectableLabel(entry.GetArguments(levelInfo.name, quickstartData.defaultArguments), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        if (EditorGUI.EndChangeCheck())
        {
            var json = JsonUtility.ToJson(quickstartData);
            EditorPrefs.SetString(quickStartDataKey, json);
        }

        GUILayout.EndVertical();

        Profiler.EndSample();
    }

    static void StartEntry(QuickstartEntry entry, string levelname, string defaultArguments)
    {
        var args = entry.GetArguments(levelname, defaultArguments);
        if (entry.runInEditor)
            EditorLevelManager.StartGameInEditor(args);
        else
        {
            RunBuild(args);
        }
    }

    static string PrettyPrintTimeStamp(DateTime time)
    {
        var span = DateTime.Now - time;
        if (span.TotalMinutes < 60)
            return span.Minutes + " mins ago";
        if (DateTime.Now.Date == time.Date)
            return time.ToShortTimeString() + " today";
        if (DateTime.Now.Date.AddDays(-1) == time.Date)
            return time.ToShortTimeString() + " yesterday";
        return "" + time;
    }

    static void StopAll()
    {
        KillAllProcesses();
        EditorApplication.isPlaying = false;
    }

    GUIContent TooltipContent(string text, string tooltip)
    {
        var content = new GUIContent
        {
            text = text,
            tooltip = tooltip,
        };
        return content;
    }

    static string EditorPrefTextField(string label, string editorPrefKey)
    {
        var str = EditorPrefs.GetString(editorPrefKey);
        str = EditorGUILayout.TextField(label, str);
        str = str.Trim();
        EditorPrefs.SetString(editorPrefKey, str);
        return str;
    }

    public static void RunBuild(string args)
    {
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        var buildPath = GetBuildPath(buildTarget);
        var buildExe = GetBuildExe(buildTarget);
        Debug.Log("Starting " + buildPath + "/" + buildExe + " " + args);
        var process = new System.Diagnostics.Process();
        process.StartInfo.UseShellExecute = args.Contains("-batchmode");
        process.StartInfo.FileName = Application.dataPath + "/../" + buildPath + "/" + buildExe;    // mogensh: for some reason we now need to specify project path
        process.StartInfo.Arguments = args;
        process.StartInfo.WorkingDirectory = buildPath;
        process.Start();
    }

    static bool IsPlaying()
    {
        if (Application.isPlaying)
            return true;

        var buildExe = GetBuildExe(EditorUserBuildSettings.activeBuildTarget);

        var processName = Path.GetFileNameWithoutExtension(buildExe);
        var processes = System.Diagnostics.Process.GetProcesses();
        foreach (var process in processes)
        {
            if (process.HasExited)
                continue;

            if (process.ProcessName == processName)
            {
                return true;
            }
        }

        return false;
    }

    static void KillAllProcesses()
    {
        var buildExe = GetBuildExe(EditorUserBuildSettings.activeBuildTarget);

        var processName = Path.GetFileNameWithoutExtension(buildExe);
        var processes = System.Diagnostics.Process.GetProcesses();
        foreach (var process in processes)
        {
            if (process.HasExited)
                continue;

            try
            {
                if (process.ProcessName != null && process.ProcessName == processName)
                {
                    process.Kill();
                }
            }
            catch (InvalidOperationException)
            {

            }
        }
    }

    static string GetAssetBundleFolder()
    {
        return GetBundlePath(EditorUserBuildSettings.activeBuildTarget) + "/" + SimpleBundleManager.assetBundleFolder;
    }

    static DateTime TimeLastBuildBundles()
    {
        return Directory.GetLastWriteTime(GetAssetBundleFolder());
    }
    
    

    static DateTime TimeLastBuildGame()
    {
        return Directory.GetLastWriteTime(GetBuildPath(EditorUserBuildSettings.activeBuildTarget));
    }

    bool m_BuildDevelopment = false;
    bool m_ConnectProfiler = false;

    bool m_IL2CPP = false;
    bool m_AllowDebugging = false;
    Vector2 m_ScrollPos;
    List<LevelInfo> m_LevelInfos;

    QuickstartData quickstartData;
}

public class BuildWindowProgress : EditorWindow
{
    static List<string> logs = new List<string>();

    static GUIStyle style;
    public static void Open(string heading)
    {
        BuildWindowProgress window = GetWindow<BuildWindowProgress>(false);
        window.position = new Rect(200, 200, 800, 500);// new Rect(Screen.width / 2, Screen.height / 2, 800, 350);
        window.ShowPopup();
        window.heading = heading;
        logs = new List<string>();
        Application.logMessageReceived += Msg;
    }


    private static void Msg(string condition, string stackTrace, LogType type)
    {
        logs.Add(condition);
    }

    public string heading = "Heading";

    private void OnDestroy()
    {
        Application.logMessageReceived -= Msg;
    }

    Vector2 scroll;
    int lastLogCount = 0;
    string text = "";
    void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            Font f = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Medium.ttf");
            if (f != null)
            {
                style.font = f;
                style.fontSize = 10;
            }
            style.richText = true;
        }
        if(lastLogCount != logs.Count)
        {
            scroll = new Vector2(0, 100000);
            if (logs.Count > 1000)
                logs = logs.GetRange(logs.Count - 1000, logs.Count);
            lastLogCount = logs.Count;
            text = string.Join("\n", logs);
        }
        EditorGUILayout.LabelField(heading, EditorStyles.boldLabel);
        GUILayout.Space(20);
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label(text, style);
        GUILayout.EndScrollView();
    }
    


}

