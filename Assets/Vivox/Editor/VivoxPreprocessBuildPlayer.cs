#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
#if UNITY_2018_1_OR_NEWER

using UnityEditor.Build.Reporting;
#endif

using UnityEngine;

class VivoxPreprocessBuildPlayer :
#if UNITY_2018_1_OR_NEWER
    IPreprocessBuildWithReport
# else
    IPreprocessBuild
#endif
{
    // Directory of the TanksUnityGameSample sample.
    private string vivoxSampleDirectory = Application.dataPath + "/Vivox/Examples/TanksUnityGameSample";
    // Path to the AppxManifest.xml file that needs to be bundled with Xbox One builds or the TanksUnityGameSample.
    private string vivoxAppManifestLocation = Application.dataPath + "/Vivox/Plugins/XboxOne/AppxManifest.xml";
    // Path to our audio directory
    private string vivoxAudioDirectory = Application.dataPath + "/Vivox/Examples/TanksUnityGameSample/Audio";
    // Path to StreamingAssets
    private string vivoxStreamingAssetsPath = Application.dataPath + "/StreamingAssets/VivoxAssets";
    // Our audio file used
    private string vivoxAudioFile = "VivoxAudioForInjection.wav";

    public int callbackOrder { get { return 0; } }

#if UNITY_2018_1_OR_NEWER
    public void OnPreprocessBuild(BuildReport report)
    {
#if UNITY_EDITOR_OSX && UNITY_IOS
        CheckMicDescription();
#endif
#if UNITY_EDITOR_WIN
        // If we detect that the TanksUnityGameSample sample isn't present, don't change any build settings, etc,...
        if (Directory.Exists(vivoxSampleDirectory))
        {
            if (report.summary.platform == BuildTarget.XboxOne)
            {
               SetXboxSettings();
            }
        }
#endif
        StreamingAssetsSetup();
    }
#endif

    public void OnPreprocessBuild(BuildTarget target, string path)
    {
#if UNITY_EDITOR_OSX && UNITY_IOS
        CheckMicDescription();
#endif
#if UNITY_EDITOR_WIN
        if (Directory.Exists(vivoxSampleDirectory))
        { 

            if (target == BuildTarget.XboxOne)
            {
                SetXboxSettings();
            }
        }   
#endif
        StreamingAssetsSetup();
    }

#if UNITY_EDITOR_WIN
    private void SetXboxSettings()
    {
        Debug.Log("Building for XB1");
        // The Vivox SDK requires specific permissions and ports to be opened. Our AppxManifest file already has these present, so we stick ours in the build settings to quickly cover these bases.
        if (File.Exists(vivoxAppManifestLocation))
        {
            Debug.Log("AppxManifest.xml was found...checking `PlayerSettings > Publishing Settings > Override Install Manifest File` to make sure it is set.");
            if (PlayerSettings.XboxOne.AppManifestOverridePath != vivoxAppManifestLocation)
            {
                Debug.LogFormat("The `Override Install Manifest File` path is not set or is incorrect...adding it now.", vivoxAppManifestLocation);
                PlayerSettings.XboxOne.AppManifestOverridePath = vivoxAppManifestLocation;
            }
        }
        else
        {
            Debug.LogError("Could not find the AppxManifest.xml file. The UnitySampleApp will not function on Xbox One properly without.");
        }

        // We cannot use the mono script compiler because the Mirror networking APIs will throw errors regarding C# 6.0 not supporting the 'out' keyword.
        if (PlayerSettings.XboxOne.scriptCompiler != ScriptCompiler.Roslyn)
        {
            Debug.Log("The script compiler is not Roslyn...switching this in the build settings");
            PlayerSettings.XboxOne.scriptCompiler = ScriptCompiler.Roslyn;
        }
    }
#endif

    private void StreamingAssetsSetup()
    {
        if (Directory.Exists(vivoxAudioDirectory))
        {
            if (!Directory.Exists(vivoxStreamingAssetsPath))
            {
                Directory.CreateDirectory(vivoxStreamingAssetsPath);
                File.Copy(vivoxAudioDirectory + "/" + vivoxAudioFile, vivoxStreamingAssetsPath + "/" + vivoxAudioFile);
            }
            else
            {
                if (!File.Exists(vivoxStreamingAssetsPath + "/" + vivoxAudioFile))
                {
                    File.Copy(vivoxAudioDirectory + "/" + vivoxAudioFile, vivoxStreamingAssetsPath + "/" + vivoxAudioFile);
                }
            }
        }
    }
#if UNITY_EDITOR_OSX && UNITY_IOS
    private void CheckMicDescription()
    {
        if (string.IsNullOrEmpty(PlayerSettings.iOS.microphoneUsageDescription))
        {
            Debug.LogWarning("If this application requests Microphone Access you must add a description to the `Other Settings > Microphone Usage Description` in Player Settings");
        }
    }
#endif
}
#endif