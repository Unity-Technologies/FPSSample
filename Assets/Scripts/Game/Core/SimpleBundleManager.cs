using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SimpleBundleManager
{
    public static string assetBundleFolder = "AssetBundles";

    public static string GetRuntimeBundlePath()
    {
#if UNITY_PS4
        return Application.streamingAssetsPath + "/" + assetBundleFolder;
#else
        if (Application.isEditor)
            return "AutoBuild/" + assetBundleFolder;
        else
            return m_runtimeBundlePath.Value;
#endif
    }

    public static void Init()
    {
    }

    public static AssetBundle LoadLevelAssetBundle(string name)
    {
        var bundle_pathname = GetRuntimeBundlePath() + "/" + name;

        GameDebug.Log("loading:" + bundle_pathname);

        var cacheKey = name.ToLower();

        AssetBundle result;
        if (!m_levelBundles.TryGetValue(cacheKey, out result))
        {
            result = AssetBundle.LoadFromFile(bundle_pathname);
            if (result != null)
                m_levelBundles.Add(cacheKey, result);
        }

        return result;
    }

    public static void ReleaseLevelAssetBundle(string name)
    {
        // TODO (petera) : Implement unloading of asset bundles. Ideally not by name.
    }

    static Dictionary<string, AssetBundle> m_levelBundles = new Dictionary<string, AssetBundle>();

    [ConfigVar(Name = "res.runtimebundlepath", DefaultValue = "AssetBundles", Description = "Asset bundle folder", Flags = ConfigVar.Flags.ServerInfo)]
    public static ConfigVar m_runtimeBundlePath;


}
