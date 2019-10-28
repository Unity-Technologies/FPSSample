/*
Copyright (c) 2014-2018 by Mercer Road Corp

Permission to use, copy, modify or distribute this software in binary or source form
for any purpose is allowed only under explicit prior consent in writing from Mercer Road Corp

THE SOFTWARE IS PROVIDED "AS IS" AND MERCER ROAD CORP DISCLAIMS
ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL MERCER ROAD CORP
BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL
DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR
PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS
ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS
SOFTWARE.
*/

using UnityEngine;
using UnityEditor;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif
using System.IO;

[System.Serializable]
public class VivoxAutoBuilder
{
    #region Constant Properties
    private static bool isFromMenuPress = false;
    private static string RELATIVE_BUILD_PATH;
    public const string CONFIG_BUILD_PATH = "Assets/Vivox/Editor";
    public const string CONFIG_FILE_NAME = "/VivoxBuildConfiguration.asset";
    
    #endregion


    #region Static Properties

    [SerializeField]
    private static VivoxBuildConfiguration BuildConfig;

    #endregion


#region Main Methods

    private static void HandleBuildConfig()
    {
        FindBuildConfig();
        string versionName = Environment.GetEnvironmentVariable("SDK_VERSION");
        if (!string.IsNullOrEmpty(versionName))
        {
            PlayerSettings.bundleVersion = versionName;
        }

        if (BuildConfig == null)
            CreateBuildConfig();

        RELATIVE_BUILD_PATH = Directory.GetCurrentDirectory() + BuildConfig.BasePath;
        Directory.CreateDirectory(RELATIVE_BUILD_PATH);

        UnityEditor.PlayerSettings.runInBackground = false;
    }

    private static void FindBuildConfig()
    {
        if (BuildConfig == null)
            BuildConfig = AssetDatabase.LoadAssetAtPath<VivoxBuildConfiguration>(CONFIG_BUILD_PATH + CONFIG_FILE_NAME);
    }

    [MenuItem("Tools/Vivox/AutoBuilder/Show Build Configuration", priority = 3)]
    private static void ShowBuildConfig()
    {
        HandleBuildConfig();
        Selection.activeObject = BuildConfig;
    }

    [MenuItem("Tools/Vivox/AutoBuilder/Create Build Configuration", priority = 4)]
    private static void CreateBuildConfig()
    {
        BuildConfig = ScriptableObject.CreateInstance<VivoxBuildConfiguration>();

        if (!AssetDatabase.IsValidFolder(CONFIG_BUILD_PATH))
        {
            Debug.LogError("Could not create create Build Configuration at " + CONFIG_BUILD_PATH + " because the location doesn't exist. Please create the directory or manually change CONFIG_BUILD_PATH.");
            return;
        }

        AssetDatabase.CreateAsset(BuildConfig, CONFIG_BUILD_PATH + CONFIG_FILE_NAME);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();

        Selection.activeObject = BuildConfig;
    }

    private static void MenuButtonBuilds(BuildTarget buildTarget)
    {
        var StartBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        var StartBuildTarget = EditorUserBuildSettings.activeBuildTarget;
        isFromMenuPress = true;
        BuildForTarget(buildTarget);

        // Setting the current build target back to what it was
        if (StartBuildTarget != EditorUserBuildSettings.activeBuildTarget)
        {
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(StartBuildTargetGroup, StartBuildTarget);
        }
        isFromMenuPress = false;
    }

    [MenuItem("Tools/Vivox/AutoBuilder/Build Win64")]
    public static void BuildWin64Menu()
    {
        MenuButtonBuilds(BuildTarget.StandaloneWindows64);
    }

    public static void BuildStandaloneWindows64()
    {
        HandleBuildConfig();

        BuildTarget target = BuildTarget.StandaloneWindows64;
        if (!IsTargetGroupSupported(BuildTargetGroup.Standalone, target))
        {
            return;
        }

#if UNITY_2018_1_OR_NEWER
        BuildReport report;
#else
        string report;
#endif
        report = BuildPipeline.BuildPlayer(
            BuildConfig.Levels,
            RELATIVE_BUILD_PATH + BuildConfig.Win64.BuildPath + "/" + PlayerSettings.productName + ".exe",
            target,
            BuildConfig.Win64.shouldRunAfterBuild && isFromMenuPress ? BuildOptions.AutoRunPlayer : BuildOptions.None
        );

        DebugBuild(report, target);
    }

    [MenuItem("Tools/Vivox/AutoBuilder/Build Win")]
    public static void BuildWinMenu()
    {
        MenuButtonBuilds(BuildTarget.StandaloneWindows);
    }

    public static void BuildStandaloneWindows()
    {
        HandleBuildConfig();

        BuildTarget target = BuildTarget.StandaloneWindows;
        if (!IsTargetGroupSupported(BuildTargetGroup.Standalone, target))
        {
            return;
        }

#if UNITY_2018_1_OR_NEWER
        BuildReport report;
#else
        string report;
#endif
        Debug.Log(BuildConfig.Win.shouldRunAfterBuild ? "Build and run" : "Build only");

report = BuildPipeline.BuildPlayer(
            BuildConfig.Levels,
            RELATIVE_BUILD_PATH + BuildConfig.Win.BuildPath + "/" + PlayerSettings.productName + ".exe",
            target,
            BuildConfig.Win.shouldRunAfterBuild && isFromMenuPress ? BuildOptions.AutoRunPlayer : BuildOptions.None
        );

        DebugBuild(report, target);

    }

    [MenuItem("Tools/Vivox/AutoBuilder/Build MacOS")]
    public static void BuildMacMenu()
    {
        MenuButtonBuilds(BuildTarget.StandaloneOSX);
    }

    public static void BuildStandaloneOSX()
    {
        HandleBuildConfig();

        BuildTarget target = BuildTarget.StandaloneOSX;
        if (!IsTargetGroupSupported(BuildTargetGroup.Standalone, target))
        {
            return;
        }

#if UNITY_2018_1_OR_NEWER
        BuildReport report;
#else
        string report;
#endif
        report = BuildPipeline.BuildPlayer(
            BuildConfig.Levels,
            RELATIVE_BUILD_PATH + BuildConfig.MacOS.BuildPath + "/" + PlayerSettings.productName,
            target,
            BuildConfig.MacOS.shouldRunAfterBuild && isFromMenuPress ? BuildOptions.AutoRunPlayer : BuildOptions.None
        );

        DebugBuild(report, target);
    }

    [MenuItem("Tools/Vivox/AutoBuilder/Build iOS")]
    public static void BuildiOSMenu()
    {
        MenuButtonBuilds(BuildTarget.iOS);
    }

    public static void BuildiOS()
    {
        HandleBuildConfig();

        BuildTarget target = BuildTarget.iOS;
        if (!IsTargetGroupSupported(BuildTargetGroup.iOS, target))
        {
            return;
        }

#if UNITY_2018_1_OR_NEWER
        BuildReport report;
#else
        string report;
#endif
        report = BuildPipeline.BuildPlayer
        (
            BuildConfig.Levels,
            RELATIVE_BUILD_PATH + BuildConfig.IOS.BuildPath + "/" + PlayerSettings.productName,
            target,
            BuildConfig.IOS.shouldRunAfterBuild && isFromMenuPress ? BuildOptions.AutoRunPlayer : BuildOptions.None
        );

        DebugBuild(report, target);
    }

    [MenuItem("Tools/Vivox/AutoBuilder/Build Android")]
    public static void BuildAndroidMenu()
    {
        MenuButtonBuilds(BuildTarget.Android);
    }

    public static void BuildAndroid()
    {
        HandleBuildConfig();

        BuildTarget target = BuildTarget.Android;
        if (!IsTargetGroupSupported(BuildTargetGroup.Android, target))
        {
            return;
        }

        string androidSDKLocation = Environment.GetEnvironmentVariable("ANDROID_HOME");

        // Check if android sdk root is in an environment variable and if it is not set in the editor prefs
        if (!String.IsNullOrEmpty(androidSDKLocation) && string.IsNullOrEmpty(EditorPrefs.GetString("AndroidSdkRoot")))
        {
            Debug.Log("Setting AndroidSdkRoot in editor to: " + androidSDKLocation);
            EditorPrefs.SetString("AndroidSdkRoot", androidSDKLocation);
        }

        string androidNDKLocation = Environment.GetEnvironmentVariable("ANDROID_NDK_HOME");

        // Check if android ndk root is in an environment variable and if it is not set in the editor prefs
        if (!string.IsNullOrEmpty(androidNDKLocation) && string.IsNullOrEmpty(EditorPrefs.GetString("AndroidNdkRoot")))
        {
            Debug.Log("Setting AndroidNdkRoot in editor to: " + androidNDKLocation);
            EditorPrefs.SetString("AndroidNdkRoot", androidNDKLocation);
        }


#if UNITY_2018_1_OR_NEWER
        BuildReport report;
#else
        string report;
#endif
        report = BuildPipeline.BuildPlayer(
            BuildConfig.Levels,
            RELATIVE_BUILD_PATH + BuildConfig.Android.BuildPath + "/" + PlayerSettings.productName + ".apk",
            target,
            BuildConfig.Android.shouldRunAfterBuild && isFromMenuPress ? BuildOptions.AutoRunPlayer : BuildOptions.None
        );

        DebugBuild(report, target);

    }

    [MenuItem("Tools/Vivox/AutoBuilder/Build XboxOne")]
    public static void BuildXboxOneMenu()
    {
        MenuButtonBuilds(BuildTarget.XboxOne);
    }

    public static void BuildXboxOne()
    {
        HandleBuildConfig();

        BuildTarget target = BuildTarget.XboxOne;
        if (!IsTargetGroupSupported(BuildTargetGroup.XboxOne, target))
        {
            return;
        }

#if UNITY_2018_1_OR_NEWER
        BuildReport report;
#else
        string report;
#endif
        report = BuildPipeline.BuildPlayer(
            BuildConfig.Levels,
            RELATIVE_BUILD_PATH + BuildConfig.XboxOne.BuildPath + "/" + PlayerSettings.productName,
            target,
            BuildConfig.XboxOne.shouldRunAfterBuild && isFromMenuPress ? BuildOptions.AutoRunPlayer : BuildOptions.None
        );

        DebugBuild(report, target);
    }

    [MenuItem("Tools/Vivox/AutoBuilder/Build PS4")]
    public static void BuildPS4Menu()
    {
        MenuButtonBuilds(BuildTarget.PS4);
    }

    public static void BuildPS4()
    {
        HandleBuildConfig();

        BuildTarget target = BuildTarget.PS4;
        if (!IsTargetGroupSupported(BuildTargetGroup.PS4, target))
        {
            return;
        }

#if UNITY_2018_1_OR_NEWER
        BuildReport report;
#else
        string report;
#endif
        report = BuildPipeline.BuildPlayer(
            BuildConfig.Levels,
            RELATIVE_BUILD_PATH + BuildConfig.PS4.BuildPath + "/" + PlayerSettings.productName,
            target,
            BuildConfig.PS4.shouldRunAfterBuild && isFromMenuPress ? BuildOptions.AutoRunPlayer : BuildOptions.None
        );

        DebugBuild(report, target);
    }

    [MenuItem("Tools/Vivox/AutoBuilder/Build Switch")]
    public static void BuildSwitchMenu()
    {
        MenuButtonBuilds(BuildTarget.Switch);
    }

    public static void BuildSwitch()
    {
        HandleBuildConfig();

        BuildTarget target = BuildTarget.Switch;
        if (!IsTargetGroupSupported(BuildTargetGroup.Switch, target))
        {
            return;
        }

#if UNITY_2018_1_OR_NEWER
        BuildReport report;
#else
        string report;
#endif
        report = BuildPipeline.BuildPlayer(
            BuildConfig.Levels,
            RELATIVE_BUILD_PATH + BuildConfig.Switch.BuildPath + "/" + PlayerSettings.productName,
            target,
            BuildConfig.Switch.shouldRunAfterBuild && isFromMenuPress ? BuildOptions.AutoRunPlayer : BuildOptions.None
        );

        DebugBuild(report, target);
    }

    private static bool IsTargetGroupSupported(BuildTargetGroup targetGroup, BuildTarget buildTarget)
    {
        if (BuildPipeline.IsBuildTargetSupported(targetGroup, buildTarget))
        {
            return true;
        }
        else
        {
            DebugBuild(
                $"Current project configuration does not support BuildTargetGroup: {targetGroup} or BuildTarget: {buildTarget} in this editor. Skipping Build.",
                buildTarget);
            return false;
        }
    }

    [MenuItem("Tools/Vivox/AutoBuilder/Build All", false, 0)]
    public static void BuildAll()
    {
        HandleBuildConfig();
        var StartBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        var StartBuildTarget = EditorUserBuildSettings.activeBuildTarget;
        isFromMenuPress = true;

        var enabledBuilds = BuildConfig.BuildList.Where(b => b.IncludedInBuildAll == true)
            .OrderByDescending(bi => bi.BuildTarget == StartBuildTarget).ThenByDescending(bi => bi.OverrideGroup);

        foreach (var buildItem in enabledBuilds)
        {
            Debug.Log($"BuildTargetGroup: {buildItem.BuildTargetGroup} and build target: {buildItem.BuildTarget}");
            BuildForTarget(buildItem.BuildTarget);
        }
        Debug.Log($"Finished build all switching back to: {StartBuildTarget}");
        EditorUserBuildSettings.SwitchActiveBuildTargetAsync(StartBuildTargetGroup,StartBuildTarget);
        isFromMenuPress = false;
    }

    private static void BuildForTarget(BuildTarget buildTarget)
    {
        switch (buildTarget)
        {
            case BuildTarget.StandaloneWindows64:
                BuildStandaloneWindows64();
                break;
            case BuildTarget.StandaloneWindows:
                BuildStandaloneWindows();
                break;
            case BuildTarget.StandaloneOSX:
                BuildStandaloneOSX();
                break;
            case BuildTarget.iOS:
                BuildiOS();
                break;
            case BuildTarget.Android:
                BuildAndroid();
                break;
            case BuildTarget.XboxOne:
                BuildXboxOne();
                break;
            case BuildTarget.PS4:
                BuildPS4();
                break;
            case BuildTarget.Switch:
                BuildSwitch();
                break;
            default:
                Debug.Log($"Trying to build a platform not supported {buildTarget}");
                break;
        }
    }

    [MenuItem("Tools/Vivox/AutoBuilder/Show Builds", false, 1)]
    public static void ShowBuild()
    {
        HandleBuildConfig();
        OpenInWinFileBrowser(RELATIVE_BUILD_PATH);
        OpenInMacFileBrowser(RELATIVE_BUILD_PATH);
    }

#endregion



#region Utility Methods

    private static void ShowInWindowsExplorer(string path)
    {
        path = path.Replace(@"/", @"\");
        System.Diagnostics.Process.Start("explorer.exe", "/select, " + path);
    }

#if UNITY_2018_1_OR_NEWER
    private static void DebugBuild(BuildReport report, BuildTarget target = BuildTarget.NoTarget)
    {
        BuildSummary summary = report.summary;
        switch (summary.result)
        {
            case BuildResult.Succeeded:
                Debug.Log("(" + summary.totalTime + ") Build succeeded: [ " + summary.platform.ToString() + " ] " + summary.totalSize + " bytes\n in directory " + summary.outputPath);
                break;
            case BuildResult.Cancelled:
                Debug.Log("(" + summary.totalTime + ") Build canceled: [ " + summary.platform.ToString() + " ] ");
                break;
            case BuildResult.Unknown:
                Debug.Log("(" + summary.totalTime + ") Build in an unknown state: [ " + summary.platform.ToString() + " ] ");
                break;
            case BuildResult.Failed:
                Debug.Log("(" + summary.totalTime + ") Build Failed: [ " + summary.platform.ToString() + " ]  (" + summary.totalErrors + ") total errors\n" + summary.ToString());
                break;
        }
    }
#endif

    private static void DebugBuild(string message, BuildTarget target)
    {
        if (string.IsNullOrEmpty(message))
            Debug.Log(target.ToString() + " build complete.");
        else
            Debug.LogWarning("Error building " + target.ToString() + ":\n" + message);
    }

    public static void OpenInMacFileBrowser(string path)
    {
        bool openInsidesOfFolder = false;

        // try mac
        string macPath = path.TrimEnd(new[] { '\\', '/' }); // Mac doesn't like trailing slash

        if (Directory.Exists(macPath)) // if path requested is a folder, automatically open insides of that folder
        {
            openInsidesOfFolder = true;
        }
        string arguments = (openInsidesOfFolder ? "" : "-R ") + macPath;
        try
        {
            System.Diagnostics.Process.Start("open", arguments);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // tried to open mac finder in windows
            // just silently skip error
        }
    }

    public static void OpenInWinFileBrowser(string path)
    {
        bool openInsidesOfFolder = false;

        // try windows
        string winPath = path.Replace("/", "\\"); // windows explorer doesn't like forward slashes

        if (Directory.Exists(winPath)) // if path requested is a folder, automatically open insides of that folder
        {
            openInsidesOfFolder = true;
        }
        try
        {
            System.Diagnostics.Process.Start("explorer.exe", (openInsidesOfFolder ? "/root," : "/select,") + winPath);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // tried to open win explorer in mac
            // just silently skip error
        }
    }

#endregion
}