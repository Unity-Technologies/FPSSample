using System;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class UpgradeMenuItems
    {
        // Remove a set of variables from the text file target by path
        static void UpdateMaterialFile_RemoveLines(string path, string[] variableNames)
        {
            string[] readText = File.ReadAllLines(path);
            List<string> writeText = new List<string>();

            foreach (string line in readText)
            {
                bool found = false;

                foreach (string str in variableNames)
                {
                    if (line.Contains(str))
                    {
                        found = true;
                    }
                }

                if (!found)
                    writeText.Add(line);
            }

            File.WriteAllLines(path, writeText.ToArray());

            return;
        }

        // It is a pity but if we call mat.GetFloat("_hdrpVersion"), this return the default value
        // if the _hdrpVersion was not written. So older material that haven't been updated can't be detected.
        // so for now we must check for _hdrpVersion in the .txt
        // maybe in a far future we can rely on just mat.GetFloat("_hdrpVersion")
        static float UpdateMaterial_GetVersion(string path, Material mat)
        {
            // Find the missing property in the file and update EmissiveColor
            string[] readText = File.ReadAllLines(path);

            foreach (string line in readText)
            {
                if (line.Contains("_HdrpVersion:"))
                {
                    int startPos = line.IndexOf(":") + 1;
                    string sub = line.Substring(startPos);
                    return float.Parse(sub);
                }
            }

            // When _HdrpVersion don't exist we MUST create it, otherwise next call to
            // mat.SetFloat("_HdrpVersion", value) will just put the default value instead of the value we pass!
            // a call to GetFloat("_HdrpVersion") solve this.
#pragma warning disable 219 // Silent warning
            float unused = mat.GetFloat("_HdrpVersion");
#pragma warning restore 219

            return 0.0f;
        }

        // Version 1

        // Update EmissiveColor after we remove EmissiveIntensity from all shaders in 2018.2
        // Now EmissiveColor is HDR and it must be update to the value new EmissiveColor = old EmissiveColor * EmissiveIntensity
        static bool UpdateMaterial_EmissiveColor_1(string path, Material mat)
        {
            // Find the missing property in the file and update EmissiveColor
            string[] readText = File.ReadAllLines(path);

            foreach (string line in readText)
            {
                if (line.Contains("_EmissiveIntensity:"))
                {
                    int startPos = line.IndexOf(":") + 1;
                    string sub = line.Substring(startPos);
                    float emissiveIntensity = float.Parse(sub);

                    Color emissiveColor = Color.black;
                    if (mat.HasProperty("_EmissiveColor"))
                    {
                        emissiveColor = mat.GetColor("_EmissiveColor");
                    }

                    emissiveColor *= emissiveIntensity;
                    emissiveColor.a = 1.0f;
                    mat.SetColor("_EmissiveColor", emissiveColor);
                    // Also fix EmissionColor if needed (Allow to let HD handle GI, if black GI is disabled by legacy)
                    mat.SetColor("_EmissionColor", Color.white);

                    return true;
                }
            }

            return false;
        }

        static void UpdateMaterialFile_EmissiveColor_1(string path)
        {
            string[] variablesNames = new string[1];
            variablesNames[0] = "_EmissiveIntensity:";
            UpdateMaterialFile_RemoveLines(path, variablesNames);
        }

        // Version 2

        // Update decal material after we added AO and metal selection. It was default to 0 and need to default to 4 now.
        // Also we have rename _SupportDBuffer to _SupportDecals
        static bool UpdateMaterial_Decals_2(string path, Material mat)
        {
            bool dirty = false;

            if (mat.shader.name == "HDRenderPipeline/Decal")
            {
                float maskBlendMode = mat.GetFloat("_MaskBlendMode");

                if (maskBlendMode == 0.0f)
                {
                    mat.SetFloat("_MaskBlendMode", (float)Decal.MaskBlendFlags.Smoothness);
                    dirty = true;
                }
            }
            else
            {
                // Find the missing property in the file and update EmissiveColor
                string[] readText = File.ReadAllLines(path);

                foreach (string line in readText)
                {
                    if (line.Contains("_SupportDBuffer:"))
                    {
                        int startPos = line.IndexOf(":") + 1;
                        string sub = line.Substring(startPos);
                        float enableDecal = float.Parse(sub);
                        mat.SetFloat("_SupportDecals", enableDecal);

                        // Decal need to also update keywords _DISABLE_DECALS
                        HDEditorUtils.ResetMaterialKeywords(mat);

                        dirty = true;
                    }
                }
            }

            return dirty;
        }

        static void UpdateMaterialFile_Decals_2(string path)
        {
            string[] variablesNames = new string[1];
            variablesNames[0] = "_SupportDBuffer:";
            UpdateMaterialFile_RemoveLines(path, variablesNames);
        }

        delegate bool UpdateMaterial(string path, Material mat);
        delegate void UpdateMaterialFile(string path);

        static void UpdateMaterialToNewerVersion(string caption, float scriptVersion, UpdateMaterial updateMaterial, UpdateMaterialFile updateMaterialFile = null)
        {
            bool VCSEnabled = (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive);
            var matIds = AssetDatabase.FindAssets("t:Material");
            List<string> materialFiles = new List<string>(); // Contain the list dirty files

            try
            {
                for (int i = 0, length = matIds.Length; i < length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(matIds[i]);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                    EditorUtility.DisplayProgressBar(
                        "Update material to new version " + caption + "...",
                        string.Format("{0} / {1} materials updated.", i, length),
                        i / (float)(length - 1));

                    if (mat.shader.name == "HDRenderPipeline/LitTessellation" ||
                        mat.shader.name == "HDRenderPipeline/Lit" ||
                        mat.shader.name == "HDRenderPipeline/LayeredLit" ||
                        mat.shader.name == "HDRenderPipeline/LayeredLitTessellation" ||
                        mat.shader.name == "HDRenderPipeline/StackLit" ||
                        mat.shader.name == "HDRenderPipeline/Unlit" ||
                        mat.shader.name == "HDRenderPipeline/Fabric" ||
                        mat.shader.name == "HDRenderPipeline/Decal" ||
                        mat.shader.name == "HDRenderPipeline/TerrainLit"
                         )
                    {
                        // We don't handle embed material as we can't rewrite fbx files
                        if (Path.GetExtension(path).ToLower() == ".fbx")
                        {
                            continue;
                        }

                        // Get current version
                        float materialVersion = UpdateMaterial_GetVersion(path, mat);

                        if (materialVersion < scriptVersion)
                        {
                            updateMaterial(path, mat);

                            // Update version number to script number (so next script can upgrade correctly)
                            mat.SetFloat("_HdrpVersion", scriptVersion);
                            

                            // Checkout the file and tag it as dirty
                            CoreEditorUtils.CheckOutFile(VCSEnabled, mat);
                            EditorUtility.SetDirty(mat);
                            materialFiles.Add(path);
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                // Save all dirty assets
                AssetDatabase.SaveAssets();
            }

            if (updateMaterialFile == null)
                return;

            // Now that all the asset have been modified and save, we can safely update the .mat file and remove removed property
            try
            {
                for (int i = 0, length = materialFiles.Count; i < length; i++)
                {
                    string path = materialFiles[i];

                    EditorUtility.DisplayProgressBar(
                        "Update .mat files...",
                        string.Format("{0} / {1} materials .mat file updated.", i, length),
                        i / (float)(length - 1));

                    // Note: The file is supposed to be checkout by the previous loop
                    updateMaterialFile(path);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                // No need to save in this case
            }
        }

        [MenuItem("Edit/Render Pipeline/Upgrade all Materials to newer version", priority = CoreUtils.editMenuPriority3)]
        static public void UpdateMaterialToNewerVersion()
        {
            // TODO: We need to handle material that are embed inside scene! + How to handle embed material in fbx?

            // Add here all the material upgrade functions
            // Note: This is a slow path as we go through all files for each script + update the version number after each script execution,
            // but it is the safest way to do it currently for incremental upgrade
            // Caution: When calling SaveAsset, Unity will update the material with latest addition at the same time, so for example
            // unity can add a supportDecal when executing script for version 1 whereas they only appear in version 2 because it is now part
            // of the shader. Most of the time this have no consequence, but we never know.
            UpdateMaterialToNewerVersion("(EmissiveColor_1)", 1.0f, UpdateMaterial_EmissiveColor_1, UpdateMaterialFile_EmissiveColor_1);
            UpdateMaterialToNewerVersion("(Decals_2)", 2.0f, UpdateMaterial_Decals_2, UpdateMaterialFile_Decals_2);

            // Caution: Version of latest script and default version in all HDRP shader must match 
        }
    }
}
