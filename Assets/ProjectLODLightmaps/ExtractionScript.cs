using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

public class ExtractionScript : MonoBehaviour
{
    [MenuItem("Lightmapper/Get LOD stats")]
    private static void GetStats()
    {
        LODGroup[] objects = FindObjectsOfType<LODGroup>();

        SortedDictionary<int, int> countDictionary = new SortedDictionary<int, int>();
        SortedDictionary<int, SortedDictionary<int, int>> lodDictionary = new SortedDictionary<int, SortedDictionary<int, int>>();

        int lodgroupsFiltered = 0;

        foreach (LODGroup group in objects)
        {
            var lods = group.GetLODs();
            bool anyStatic = false;

            for (int lodIndex = 0; lodIndex < group.lodCount; lodIndex++)
            {
                foreach (var renderer in lods[lodIndex].renderers)
                {
                    if (renderer != null)
                    {
                        if ((GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.LightmapStatic) != 0)
                        {
                            anyStatic = true;
                        }
                    }
                }
            }

            if (!anyStatic)
                continue;

            lodgroupsFiltered++;

            if (!countDictionary.ContainsKey(group.lodCount))
                countDictionary.Add(group.lodCount, 0);

            countDictionary[group.lodCount] += 1;

            for (int lodIndex = 0; lodIndex < group.lodCount; lodIndex++)
            {

                // per LOD level, renderer count and occurrence
                int rendererCount = 0;
                foreach (Renderer renderer in lods[lodIndex].renderers)
                {
                    if (renderer != null && (GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.LightmapStatic) != 0)
                        rendererCount++;
                }

                if (!lodDictionary.ContainsKey(lodIndex))
                    lodDictionary.Add(lodIndex, new SortedDictionary<int, int>());

                if (!lodDictionary[lodIndex].ContainsKey(rendererCount))
                    lodDictionary[lodIndex].Add(rendererCount, 0);

                lodDictionary[lodIndex][rendererCount] += 1;
            }
        }

        Debug.Log("LOD groups: " + objects.Length);
        Debug.Log("LOD groups with any static renderers: " + lodgroupsFiltered);

        foreach (var entry in countDictionary)
        {
            Debug.Log("LOD count " + entry.Key + ": " + entry.Value + " occurrences");
        }

        foreach (var entry0 in lodDictionary)
        {
            foreach (var entry1 in entry0.Value)
            {
                Debug.Log("LOD level " + entry0.Key + " with " + entry1.Key + " renderers: " + entry1.Value + " occurrences");
            }
        }
    }


    [MenuItem("Lightmapper/Set up LOD projection [dry run]")]
    private static void SetUpProjectionDryRun()
    {
        SetUpProjectionInternal(true);
    }

    [MenuItem("Lightmapper/Set up LOD projection")]
    private static void SetUpProjection()
    {
        SetUpProjectionInternal(false);
    }

    private static bool IsLightmapStatic(Renderer renderer)
    {
        return (GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.LightmapStatic) != 0;
    }

    private static void SetUpProjectionInternal(bool dryRun)
    {
        LODGroup[] objects = FindObjectsOfType<LODGroup>();

        foreach (LODGroup group in objects)
        {
            var lods = group.GetLODs();
            if (lods.Length <= 1)
                continue;

            LOD lod0 = lods[0];
            Dictionary<string, Renderer> lod0RendererNames = new Dictionary<string, Renderer>();

            foreach (Renderer renderer in lod0.renderers)
            {
                string prefix = GetPrefix(renderer);

                if (prefix != null)
                {
                    if (!lod0RendererNames.ContainsKey(prefix))
                        lod0RendererNames.Add(prefix, renderer);
                    else
                        Debug.LogError("ERROR DUPLICATE: " + prefix);
                }
            }

            for (int lodIndex = 1; lodIndex < lods.Length; lodIndex++)
            {
                LOD lod = lods[lodIndex];
                foreach (var renderer in lod.renderers)
                {
                    if (renderer == null || !IsLightmapStatic(renderer))
                        continue;

                    string prefix = GetPrefix(renderer);

                    if (prefix != null)
                    {
                        if (lod0RendererNames.ContainsKey(prefix))
                        {
                            Debug.Log("Setting up projection for " + prefix + " at LOD " + lodIndex, renderer);

                            bool isMatchingLOD0RendererLightmapStatic = IsLightmapStatic(lod0RendererNames[prefix]);
                            if (!isMatchingLOD0RendererLightmapStatic)
                                Debug.LogWarning("The matching renderer for " + prefix + " at LOD0 is not lightmap static. Can't set up projection, just disabling the lightmap static flag on the current renderer.", renderer);

                            if (!dryRun)
                            {
                                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
                                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags & ~StaticEditorFlags.LightmapStatic);

                                if (isMatchingLOD0RendererLightmapStatic)
                                {
                                    ProjectLODLightmaps projectLODLightmaps = renderer.gameObject.AddComponent<ProjectLODLightmaps>();
                                    projectLODLightmaps.m_Renderer = lod0RendererNames[prefix];
                                }
                            }
                        }
                        else
                        { 
                            Debug.LogError(prefix + " was not found in LOD0", renderer);
                        }
                    }
                }
            }

            /*foreach (var name in lod0RendererNames)
            {
                Debug.Log(name);
            }*/
        }
    }

    static string GetPrefix(Renderer renderer)
    {
        if (renderer == null)
            return null;

        string rendererName = renderer.gameObject.name;
        string pattern = @"^(.*?)_LOD(\d+)";
        Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
        Match match = rgx.Match(rendererName);

        if (match.Success)
        {
            if (match.Groups.Count > 1)
                return match.Groups[1].ToString();
            else
            {
                Debug.LogError("Error: " + match.ToString());
            }
        }

        return null;
    }
}
#endif
