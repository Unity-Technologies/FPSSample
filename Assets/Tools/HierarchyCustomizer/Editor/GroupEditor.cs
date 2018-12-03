using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;

[InitializeOnLoad]
public class GroupEditor
{
    private const string s_fileName = "GroupEditor.cs"; 
    
    static string[] s_PrefabModificationSkips;

    static GroupEditor()
    {
        const string deathstarIconPath = "DeathStarIcon.png";
        const string prefabIconPath = "PreFabIcon.png";
        const string prefabIconDisconnectedPath = "PreFabIcon_Broken.png";
        const string prefabIconOverriddenPath = "PreFabIcon_Bold.png";
        const string prefabIconMissingPath = "PreFabIcon.png";

        s_PrefabModificationSkips = new string[]
        {
             "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w", "m_RootOrder",
             "m_AnchoredPosition.x", "m_AnchoredPosition.y", "m_SizeDelta.x", "m_SizeDelta.y", "m_AnchorMin.x", "m_AnchorMin.y", "m_AnchorMax.x", "m_AnchorMax.y", "m_Pivot.x", "m_Pivot.y",
             "m_Name"
        };

        var scripts = System.IO.Directory.GetFiles(Application.dataPath, s_fileName, SearchOption.AllDirectories);
        if (scripts.Length != 1)
        {
            Debug.LogError("There should only be one script in project of type:" + s_fileName);
            return;
        }
        
        var path = scripts[0].Replace(s_fileName, "").Replace("\\", "/");
        path = path.Substring(path.IndexOf("Assets/"));
        
        s_IconDeathStar = LoadTexture2D(path + deathstarIconPath);
        s_IconPrefab = LoadTexture2D(path + prefabIconPath);
        s_IconPrefabDisconnected = LoadTexture2D(path + prefabIconDisconnectedPath);
        s_IconPrefabOverridden = LoadTexture2D(path + prefabIconOverriddenPath);
        s_IconPrefabMissing = LoadTexture2D(path + prefabIconMissingPath);

        EditorApplication.hierarchyWindowItemOnGUI += HierarchyWinwodItemOnGui;
    }

    static void HierarchyWinwodItemOnGui(int instanceID, Rect selectionRect)
    {
        var target = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (target == null)
        {
            return;
        }

        int indent = 0;

        if (CutAndPaste.objectsSelectedForCut != null && CutAndPaste.objectsSelectedForCut.Contains(target))
        {
            var rect = new Rect(selectionRect.xMax - selectionRect.height - 2 - indent, selectionRect.yMin, selectionRect.height, selectionRect.height);
            GUI.DrawTexture(rect, s_IconDeathStar);
            indent += 18;
        }
        
        var prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(target);
        if(prefabInstanceStatus != PrefabInstanceStatus.NotAPrefab && PrefabUtility.GetOutermostPrefabInstanceRoot(target) == target)
        {
            var icon = s_IconPrefab;
            if (prefabInstanceStatus == PrefabInstanceStatus.Disconnected)
                icon = s_IconPrefabDisconnected;
            else if (prefabInstanceStatus == PrefabInstanceStatus.MissingAsset)
            {
                icon = s_IconPrefabMissing;
            }
            else
            {
                // Check if prefab has modifications where it matters!
                bool modified = false;
                var mods = PrefabUtility.GetPropertyModifications(target);
                foreach (var m in mods)
                {
                    foreach (var s in s_PrefabModificationSkips)
                    {
                        if(m.propertyPath.PrefixMatch(s) == s.Length)
                        //if (m.propertyPath.StartsWith(s))
                            goto nextModification;
                    }
                    modified = true;
                    break;
                    nextModification:;
                }
                if (modified)
                    icon = s_IconPrefabOverridden;
            }
            var rect = new Rect(selectionRect.xMax - selectionRect.height - 2 - indent, selectionRect.yMin, selectionRect.height, selectionRect.height);
            GUI.DrawTexture(rect, icon);
            indent += 18;
        }
    }

    static Texture2D LoadTexture2D(string path)
    {
        var tex = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
        if (tex == null)
        {
            Debug.LogWarning("Unable to load texture at " + path);
        }
        return tex;
    }

    static Texture2D s_IconDeathStar;
    static Texture2D s_IconPrefab;
    static Texture2D s_IconPrefabDisconnected;
    static Texture2D s_IconPrefabOverridden;
    static Texture2D s_IconPrefabMissing;
}
