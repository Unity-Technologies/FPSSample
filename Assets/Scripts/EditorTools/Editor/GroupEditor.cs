using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;

[InitializeOnLoad]
public class GroupEditor
{
    static string[] s_PrefabModificationSkips;

    static GroupEditor()
    {
        const string deathstarIconPath = "Assets/Scripts/EditorTools/Editor/DeathStarIcon.png";
        const string prefabIconPath = "Assets/Scripts/EditorTools/Editor/PreFabIcon.png";
        const string prefabIconDisconnectedPath = "Assets/Scripts/EditorTools/Editor/PreFabIcon_Broken.png";
        const string prefabIconOverriddenPath = "Assets/Scripts/EditorTools/Editor/PreFabIcon_Bold.png";
        const string prefabIconMissingPath = "Assets/Scripts/EditorTools/Editor/PreFabIcon.png";

        s_PrefabModificationSkips = new string[]
        {
             "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w", "m_RootOrder",
             "m_AnchoredPosition.x", "m_AnchoredPosition.y", "m_SizeDelta.x", "m_SizeDelta.y", "m_AnchorMin.x", "m_AnchorMin.y", "m_AnchorMax.x", "m_AnchorMax.y", "m_Pivot.x", "m_Pivot.y",
             "m_Name"
        };

        s_IconDeathStar = LoadTexture2D(deathstarIconPath);
        s_IconPrefab = LoadTexture2D(prefabIconPath);
        s_IconPrefabDisconnected = LoadTexture2D(prefabIconDisconnectedPath);
        s_IconPrefabOverridden = LoadTexture2D(prefabIconOverriddenPath);
        s_IconPrefabMissing = LoadTexture2D(prefabIconMissingPath);

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

        if (HotKeys.objectsSelectedForCut != null && HotKeys.objectsSelectedForCut.Contains(target))
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
