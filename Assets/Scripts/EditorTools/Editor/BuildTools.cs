using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Threading;
using UnityEditor.Build.Pipeline;

public class BuildTools
{
    public static void CopyDirectory(string SourcePath, string DestinationPath)
    {
        //Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(SourcePath, "*",
            SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(SourcePath, "*.*",
            SearchOption.AllDirectories))
            File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath), true);
    }

    public static UnityEditor.Build.Reporting.BuildReport BuildGame(string buildPath, string exeName, BuildTarget target,
        BuildOptions opts, string buildId, bool il2cpp)
    {
        var levels = new string[]
        {
            "Assets/Scenes/bootstrapper.unity",
            "Assets/Scenes/empty.unity"
        };

        var exePathName = buildPath + "/" + exeName;

        Debug.Log("Building: " + exePathName);
        Directory.CreateDirectory(buildPath);

        // Set all files to be writeable (As Unity 2017.1 sets them to read only)
        string fullBuildPath = Directory.GetCurrentDirectory() + "/" + buildPath;
        string[] fileNames = Directory.GetFiles(fullBuildPath, "*.*", SearchOption.AllDirectories);

        //Contentpipeline compile player scripts

        foreach (var fileName in fileNames)
        {
            FileAttributes attributes = File.GetAttributes(fileName);
            attributes &= ~FileAttributes.ReadOnly;
            File.SetAttributes(fileName, attributes);
        }

        string bundlePathSrc = buildPath + "/" + SimpleBundleManager.assetBundleFolder;
        string bundlePathDst = "Assets/StreamingAssets/" + SimpleBundleManager.assetBundleFolder;
        if (target == BuildTarget.PS4)
        {
            if (!Directory.Exists(bundlePathSrc))
            {
                EditorUtility.DisplayDialog("No bundles found", "No Asset Bundles found. Please build them first",
                    "Ok");
                return null;
            }

            CopyDirectory(bundlePathSrc, bundlePathDst);
        }

        var monoDirs = Directory.GetDirectories(fullBuildPath).Where(s => s.Contains("MonoBleedingEdge"));
        var il2cppDirs = Directory.GetDirectories(fullBuildPath).Where(s => s.Contains("BackUpThisFolder_ButDontShipItWithYourGame"));
        var clearFolder = (il2cpp && monoDirs.Count() > 0) || (!il2cpp && il2cppDirs.Count() > 0);
        if (clearFolder)
        {
            Debug.Log(" deleting old folders ..");
            foreach(var file in Directory.GetFiles(fullBuildPath))
                File.Delete(file);
            foreach(var dir in monoDirs)
                Directory.Delete(dir,true);
            foreach(var dir in il2cppDirs)
                Directory.Delete(dir,true);
            foreach(var dir in Directory.GetDirectories(fullBuildPath).Where(s => s.EndsWith("_Data")))
                Directory.Delete(dir,true);
        }

        if (il2cpp)
        {
            UnityEditor.PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
            UnityEditor.PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Standalone, Il2CppCompilerConfiguration.Release);
        }
        else
        {
            UnityEditor.PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        }
        
        /// Colossal hack to work around build postprocessing expecting everything to be writable in the unity
        /// installation, but if people have unity in p4 it will be readonly.
        var editorHome = EditorApplication.applicationPath.BeforeLast("/") + "/Data/PlaybackEngines/windowsstandalonesupport";
        Debug.Log("Checking for read/only files in standalone players");
        if (Directory.Exists(editorHome))
        {
            var files = Directory.GetFiles(editorHome, "*.*", SearchOption.AllDirectories);
            foreach(var f in files)
            {
                var attr = File.GetAttributes(f);
                if((attr & FileAttributes.ReadOnly) != 0)
                {
                    attr = attr & ~FileAttributes.ReadOnly;
                    Debug.Log("Setting " + f + " to read/write");
                    File.SetAttributes(f, attr);
                }
            }
        }
        Debug.Log("Done.");
        
        Environment.SetEnvironmentVariable("BUILD_ID", buildId, EnvironmentVariableTarget.Process);
        var result = BuildPipeline.BuildPlayer(levels, exePathName, target, opts);
        Environment.SetEnvironmentVariable("BUILD_ID", "", EnvironmentVariableTarget.Process);

        if (target == BuildTarget.PS4)
        {
            Directory.Delete(bundlePathDst, true);
        }


        Debug.Log(" ==== Build Done =====");


        var stepCount = result.steps.Count();
        Debug.Log(" Steps:"+ stepCount);
        for(var i=0;i<stepCount;i++)
        {
            var step = result.steps[i];
            Debug.Log("-- " + (i+1) + "/" + stepCount + " " + step.name + " " + step.duration.Seconds + "s --");
            foreach (var msg in step.messages)
                Debug.Log(msg.content);
        }

        return result;
    }

    public static List<LevelInfo> LoadLevelInfos()
    {
        return LoadAssetsOfType<LevelInfo>();
    }

    static void AddKeys(Dictionary<string, int> dictionary, string[] keys)
    {
        foreach (var key in keys)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key]++;
            }
            else
            {
                dictionary[key] = 1;
            }
        }
    }

    public static List<T> LoadAssetsOfType<T>() where T : UnityEngine.Object
    {
        var result = new List<T>();
        var assets = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        foreach (var a in assets)
        {
            var path = AssetDatabase.GUIDToAssetPath(a);
            result.Add(AssetDatabase.LoadAssetAtPath<T>(path));
        }
        return result;
    }

    static AssetBundleBuild MakeSceneBundleBuild(UnityEngine.Object mainScene, string name)
    {
        var build = new AssetBundleBuild();
        build.assetBundleName = name;
        build.assetBundleVariant = "";

        var path = AssetDatabase.GetAssetPath(mainScene);
        var scenes = new List<string>();
        scenes.Add(path.ToLower());

        if (EditorLevelManager.IsLayeredLevel(path))
        {
            foreach (var l in EditorLevelManager.GetLevelLayers(path))
            {
                scenes.Add(l.ToLower());
            }
        }

        build.assetNames = scenes.ToArray();
        return build;
    }

    static AssetBundleBuild MakeAssetBundleBuild(List<string> assets, string name)
    {
        var build = new AssetBundleBuild();
        build.assetBundleName = name;
        build.assetBundleVariant = "";
        build.assetNames = assets.ToArray();
        return build;
    }

    static List<string> FindSharedDependencies(List<AssetBundleBuild> builds)
    {
        var dependenciesCount = new Dictionary<string, int>();

        foreach (var build in builds)
        {
            foreach (var asset in build.assetNames)
            {
                var dependencies = AssetDatabase.GetDependencies(asset, true);
                AddKeys(dependenciesCount, dependencies);
            }
        }

        var shared = new List<string>();
        foreach (var dependency in dependenciesCount)
        {
            if (dependency.Key.EndsWith(".unity"))
                continue;
            if (dependency.Key.EndsWith(".cs"))
                continue;

            if (dependency.Value > 1)
                shared.Add(dependency.Key);
        }
        return shared;
    }

    public static void BuildBundles(string bundlePath, BuildTarget target, bool buildBundledAssets, bool buildBundledLevels, bool force = false, List<LevelInfo> buildOnlyLevels = null)
    {
        Debug.Log("Scene cooking started");

        var path = bundlePath + "/" + SimpleBundleManager.assetBundleFolder;

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        BuildAssetBundleOptions assetBundleOptions = BuildAssetBundleOptions.UncompressedAssetBundle;
        if (force)
        {
            Debug.Log("Forcing rebuild");
            assetBundleOptions |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
        }

        if (buildBundledLevels)
            BuildLevelBundles(path, target, assetBundleOptions, buildOnlyLevels);

        if (buildBundledAssets)
            BundledResourceBuilder.BuildBundles(path, target, assetBundleOptions);
    }

    public static void BuildLevelBundles(string path, BuildTarget target, BuildAssetBundleOptions assetBundleOptions, List<LevelInfo> buildOnlyLevels = null)
    {
        var builds = new List<AssetBundleBuild>();
        foreach (var levelInfo in LoadLevelInfos())
        {
            if (buildOnlyLevels != null && !buildOnlyLevels.Contains(levelInfo))
                continue;
            if (!levelInfo.includeInBuild)
                continue;
            Debug.Log(" - adding level: " + AssetDatabase.GetAssetPath(levelInfo.main_scene));
            var build = MakeSceneBundleBuild(levelInfo.main_scene, levelInfo.name);
            builds.Add(build);
        }

        // TODO (petera) enable once we've done proper shared assets support
        //var dependencies = FindSharedDependencies(builds);
        //var sharedbuild = MakeAssetBundleBuild(dependencies, "shared_assets");
        //builds.Add(sharedbuild);

        // TODO (mogensh) Settle on what buildpipeline to use. LegacyBuildPipeline uses SBP internally and is faster.      
        //        LegacyBuildPipeline.BuildAssetBundles(path, builds.ToArray(), assetBundleOptions, EditorUserBuildSettings.activeBuildTarget);
        BuildPipeline.BuildAssetBundles(path, builds.ToArray(), assetBundleOptions, EditorUserBuildSettings.activeBuildTarget);

        // Set write time so tools can show time since build
        Directory.SetLastWriteTime(path, DateTime.Now);

        Debug.Log("Scene cooking done");
    }

    static string GetBuildName()
    {
        var buildNumber = System.Environment.GetEnvironmentVariable("BUILD_NUMBER");
        if (buildNumber == null)
        {
            buildNumber = "Dev";
        }

        var changeSet = System.Environment.GetEnvironmentVariable("P4_CHANGELIST");
        if (changeSet == null)
        {
            changeSet = "0";
        }

        var now = System.DateTime.Now;
        var name = now.ToString("yyyyMMdd") + "." + buildNumber + "." + changeSet;
        return name;
    }

    static string GetLongBuildName(BuildTarget target, string buildName)
    {
        return Application.productName + "_" + target.ToString() + "_" + buildName;
    }

    static string GetBuildPath(BuildTarget target, string buildName)
    {
        return "Builds/" + target.ToString() + "/" + GetLongBuildName(target, buildName);
    }

    static string GetBuildFolderPath(BuildTarget target)
    {
        return "Builds/" + target.ToString();
    }

    [MenuItem("Assets/ResirializeAssets")]
    public static void ReserializeProject()
    {
        if (Selection.assetGUIDs.Length == 0)
            return;

        List<string> paths = new List<string>();
        foreach (var g in Selection.assetGUIDs)
            paths.Add(AssetDatabase.GUIDToAssetPath(g));

        if (EditorUtility.DisplayDialog("Reserialize " + paths.Count + " assets", "Do you want to reserialize " + paths.Count + " assets?", "Yes, I do!"))
        {
            foreach(var p in paths)
            {
                var a = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p);
                EditorUtility.SetDirty(a);
            }
            AssetDatabase.SaveAssets();
        }
    }

    [MenuItem("FPS Sample/BuildSystem/Win64/OpenBuildFolder")]
    public static void OpenBuildFolder()
    {
        var target = BuildTarget.StandaloneWindows64;
        var buildPath = GetBuildFolderPath(target);
        if (Directory.Exists(buildPath))
        {
            Debug.Log("Opening " + buildPath);
            var p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo("explorer.exe", Path.GetFullPath(buildPath));
            p.Start();
        }
        else
        {
            Debug.LogWarning("No build folder found here: " + buildPath);
        }
    }

    [MenuItem("FPS Sample/BuildSystem/Win64/Deploy")]
    public static void Deploy()
    {
        Debug.Log("Window64 Deploying...");
        var target = BuildTarget.StandaloneWindows64;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        //string executableName = Application.productName + ".exe";

        var platform = target.ToString();
        var clientApi = "b5176262e35ba4aa8280a68aae7b0492";

        // TODO: Figure out if it's possible to initialize cloud / unityconnect here instead
        var projectId = CloudProjectSettings.projectId;
        var orgId = CloudProjectSettings.organizationId;
        var projectName = CloudProjectSettings.projectId;
        var accessToken = CloudProjectSettings.accessToken;

        var deploy = new ConnectedGames.Build.DeployTools(OnProgressUpdate, clientApi, projectId, orgId, projectName, accessToken);

        var dstPath = buildName + ".zip";

        Debug.Log("Starting upload src=" + buildPath + " platform=" + platform + " isClient=N/A" +
            " clientApi=" + clientApi + " projectId=" + projectId + " orgId=" + orgId +
            " projectName=" + projectName + " accessToken=" + accessToken);

        deploy.CompressAndUpload(buildPath, buildPath+"/"+dstPath, platform, buildName);
        while (!deploy.Done)
        {
            deploy.UpdateLoop();
            Thread.Sleep(100);
        }
    }

    private static void OnProgressUpdate(string fileName, double progress)
    {
        Debug.Log(fileName + ":" + progress);
        return;
    }

    [MenuItem("FPS Sample/BuildSystem/Win64/PostProcess")]
    public static void PostProcessWindows64()
    {
        Debug.Log("Window64 build postprocessing...");
        var target = BuildTarget.StandaloneWindows64;
        var buildName = GetBuildName();
        var zipName = GetLongBuildName(target, buildName) + ".zip";
        var buildPath = GetBuildPath(target, buildName);
        string executableName = Application.productName + ".exe";

        if (!Directory.Exists(buildPath) || !File.Exists(buildPath + "/" + Application.productName + ".exe"))
        {
            Debug.Log("No build here: " + buildPath);
            return;
        }

        Debug.Log("Writing config files");
        // Build server bat
        var serverBat = new string[]
        {
            "REM start game server on level_01",
            executableName + " -nographics -batchmode -noboot +serve level_01 +game.modename assault"
        };
        File.WriteAllLines(buildPath + "/server.bat", serverBat);
        Debug.Log("  server.bat");

        // Build empty user.cfg
        File.WriteAllLines(buildPath + "/user.cfg", new string[] { });
        Debug.Log("  user.cfg");

        // Build boot.cfg
        var bootCfg = new string[]
        {
           "client",
           "load level_menu"
        };
        File.WriteAllLines(buildPath + "/" + Game.k_BootConfigFilename, bootCfg);
        Debug.Log("  " + Game.k_BootConfigFilename);

        Debug.Log("Writing steam upload bat file.");
        var steamBat = new string[]
        {
@"@echo off",
@"color",
@"echo Verifying Steam SDK...",
@"if not exist c:\steam\sdk\tools\ContentBuilder (",
@"    echo Failed. Steam SDK must be installed at c:\steam",
@"    goto :err",
@")",
@"echo OK. Found SDK at c:\steam",
@"echo Looking for zipped game...",
@"if not exist " + zipName + " (",
@"    echo Failed. Did not locate zip: " + zipName,
@"    goto :err",
@")",
@"echo OK. Found game at "+zipName,
@"echo Removing old stage area",
@"rmdir /s /q c:\steam\stage",
@"echo Extracting build to staging area",
@"""c:\Program Files\7-Zip\7z.exe"" x "+zipName + @" -oc:\steam\stage",
@" cd c:\steam\sdk\tools\ContentBuilder",
@"set /p steamuser=""Enter steam user:""",
@"builder\steamcmd.exe +login %steamuser% +run_app_build_http ..\scripts\app_build_962460.vdf +quit",
@"echo All done!",
@"goto :ok",
@":err",
@"color 4f",
@"goto :done",
@":ok",
@"color 2f",
@":done",
@"pause"
        };
        var steamBatName = "steam_upload_" + buildName + ".bat";
        File.WriteAllLines(buildPath + "/../" + steamBatName, steamBat);
        Debug.Log("  " + steamBatName);


        Debug.Log("Window64 build postprocessing done.");
    }

    [MenuItem("FPS Sample/BuildSystem/Win64/CreateBuildWindows64")]
    public static void CreateBuildWindows64()
    {
        CreateBuildWindows64(false);
    }

    [MenuItem("FPS Sample/BuildSystem/Win64/CreateBuildWindows64-IL2CPP")]
    public static void CreateBuildWindows64IL2CPP()
    {
        CreateBuildWindows64(true);
    }

    static void CreateBuildWindows64(bool useIL2CPP)
    {
        Debug.Log("Window64 build started. (" + (useIL2CPP ? "IL2CPP" : "Mono") + ")");
        var target = BuildTarget.StandaloneWindows64;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        string executableName = Application.productName + ".exe";

        Directory.CreateDirectory(buildPath);

        BuildBundles(buildPath, target, true, true, true);
        var res = BuildGame(buildPath, executableName, target, BuildOptions.None, buildName, useIL2CPP);

        if (!res)
            throw new Exception("BuildPipeline.BuildPlayer failed");
        if (res.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("BuildPipeline.BuildPlayer failed: " + res.ToString());

        Debug.Log("Window64 build completed...");
        PostProcessWindows64();
    }

    [MenuItem("FPS Sample/BuildSystem/PS4/CreateBuildPS4")]
    public static void CreateBuildPS4()
    {
        var target = BuildTarget.PS4;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        string executableName = Application.productName;

        Directory.CreateDirectory(buildPath);
        BuildBundles(buildPath, target, true, true, true);
        var res = BuildGame(buildPath, executableName, target, BuildOptions.None, buildName, false);

        if (!res)
            throw new Exception("BuildPipeline.BuildPlayer failed");
        if (res.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("BuildPipeline.BuildPlayer failed: " + res.ToString());
    }

    [MenuItem("FPS Sample/BuildSystem/Linux64/PostProcess")]
    public static void PostProcessLinux()
    {
        Debug.Log("Linux build postprocessing...");
        var target = BuildTarget.StandaloneLinux64;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        string executableName = "server-linux.x86_64";


        if (!Directory.Exists(buildPath) || !File.Exists(buildPath + "/" + executableName))
        {
            Debug.Log("No build here: " + buildPath);
            return;
        }

        Debug.Log("Writing config files");

        // Build empty user.cfg
        File.WriteAllLines(buildPath + "/user.cfg", new string[] { });
        Debug.Log("  user.cfg");

        // Build boot.cfg
        var bootCfg = new string[]
        {
           "game.modename assault",
           "serve level_01"
        };
        File.WriteAllLines(buildPath + "/" + Game.k_BootConfigFilename, bootCfg);
        Debug.Log("  " + Game.k_BootConfigFilename);

        Debug.Log("Linux build postprocessing done.");
    }

    [MenuItem("FPS Sample/BuildSystem/Linux64/CreateBuildLinux64")]
    public static void CreateBuildLinux64()
    {
        var target = BuildTarget.StandaloneLinux64;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        string executableName = "server-linux.x86_64";

        Directory.CreateDirectory(buildPath);
        BuildBundles(buildPath, target, true, true, true);
        var res = BuildGame(buildPath, executableName, target, BuildOptions.EnableHeadlessMode, buildName, false);

        if (!res)
            throw new Exception("BuildPipeline.BuildPlayer failed");
        if (res.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("BuildPipeline.BuildPlayer failed: " + res.ToString());

        GameDebug.Log("Linux build completed...");
        PostProcessLinux();
    }

    // This is just a little convenience for when iterating on Linux specific code
    // Build a full build and then use this to just build the executable into the same build folder.
    [MenuItem("FPS Sample/BuildSystem/Linux64/CreateBuildLinux64_OnlyExecutable")]
    public static void CreateBuildLinux64_OnlyExecutable()
    {
        var target = BuildTarget.StandaloneLinux64;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, buildName);
        string executableName = "server-linux.x86_64";

        Directory.CreateDirectory(buildPath);
        var res = BuildGame(buildPath, executableName, target, BuildOptions.EnableHeadlessMode, buildName, false);

        if (!res)
            throw new Exception("BuildPipeline.BuildPlayer failed");
        if (res.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("BuildPipeline.BuildPlayer failed: " + res.ToString());

        GameDebug.Log("Linux build completed...");
    }
}
