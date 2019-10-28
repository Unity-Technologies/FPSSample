using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_EDITOR_OSX && UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
public class VivoxPostprocessBuildPlayer
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
#if UNITY_EDITOR_OSX && UNITY_IOS
        // Vivox related build requrements for ios
        if (target == BuildTarget.iOS)
        {
            // Get project
            string projectFile = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
            PBXProject proj = new PBXProject();
            proj.ReadFromString(File.ReadAllText(projectFile));
            string targetGUID = proj.TargetGuidByName("Unity-iPhone");
         
            // Security.framework
            proj.AddFrameworkToProject(targetGUID, "Security.framework", false);

            // -lresolv
            proj.AddBuildProperty(targetGUID, "OTHER_LDFLAGS", "-lresolv");

            // Save Changes
            File.WriteAllText(projectFile, proj.WriteToString());
        }
#endif
    }
}
