using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [InitializeOnLoad]
    public class HDRPVersion
    {
        static public int hdrpVersion = 1;

        static public int GetCurrentHDRPProjectVersion()
        {
            string[] version = new string[1];

            try
            {
                version = File.ReadAllLines("ProjectSettings/HDRPProjectVersion.txt");
            }
            catch
            {
                // Don't display warning
                //Debug.LogWarning("Unable to read from ProjectSettings/HDRPProjectVersion.txt - Assign default version value");

                // When we don't find HDRPProjectVersion file we return the current value. Because this happen when you create new project.
                return hdrpVersion;
            }

            return int.Parse(version[0]);
        }

        static public void WriteCurrentHDRPProjectVersion()
        {
            string[] newVersion = new string[1];
            newVersion[0] = hdrpVersion.ToString();

            try
            {
                File.WriteAllLines("ProjectSettings/HDRPProjectVersion.txt", newVersion);
            }
            catch
            {
                Debug.LogWarning("Unable to write ProjectSettings/HDRPProjectVersion.txt");
            }
        }

        static HDRPVersion()
        {
            /*
            // Compare project version with current version - Trigger an upgrade if user ask for it
            if (false) //GetCurrentHDRPProjectVersion() < hdrpVersion) // TODO: Disable for now as it doesn't work correctly
            {
                if (EditorUtility.DisplayDialog("A newer version of HDRP has been detected",
                                                "Do you want to upgrade your materials to newer version?\n You can also upgrade manually materials in 'Edit -> Render Pipeline' submenu", "Yes", "No"))
                {
                    UpgradeMenuItems.UpdateMaterialToNewerVersion();
                }
            }
            */
        }
    }

    public class FileModificationWarning : UnityEditor.AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string path in paths)
            {
                // Detect when we save project and write our HDRP version at this time.
                if (path == "ProjectSettings/ProjectSettings.asset")
                {
                    // Update current project version with HDRP version
                    HDRPVersion.WriteCurrentHDRPProjectVersion();
                }
            }
            return paths;
        }
    }
}
