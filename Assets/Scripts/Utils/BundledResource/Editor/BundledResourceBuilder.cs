using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public class BundledResourceBuilder
{
    static bool saveAssets;
    
    static BundledResourceBuilder()
    {
        EditorApplication.update += Update;
    }

    private static void Update()
    {
        if (saveAssets)
        {
            saveAssets = false;
            GameDebug.Log("BundledResourceBuilder saving assets...");
            AssetDatabase.SaveAssets();
            GameDebug.Log("BundledResourceBuilder saving done");
        }
    }

    public static void UpdateRegistries(bool dry)     
    {
        if (dry)
            Debug.Log("Updating registries");
        var assets = AssetDatabase.FindAssets("t:" + typeof(RegistryBase).Name);
        foreach (var a in assets)
        {
            var path = AssetDatabase.GUIDToAssetPath(a);
            Debug.Log("Updating " + path);
            var handle = AssetDatabase.LoadAssetAtPath<RegistryBase>(path);
            handle.UpdateRegistry(dry);

            if(!dry)
                EditorUtility.SetDirty(handle);
        }

        if (!dry)
            saveAssets = true;
    }

    [MenuItem("FPS Sample/Registries/Dryrun update")]
    public static void DryrunUpdateRegistries()
    {
        BuildWindowProgress.Open("Dryrun Registry Update");
        UpdateRegistries(true);
    }

    [MenuItem("FPS Sample/Registries/Test registries")]
    public static void TestRegistriesMenu()
    {
        BuildWindowProgress.Open("Verify Registries");
        TestRegistries();
    }

    public static bool TestRegistries()
    {
        bool ok = true;
        Debug.Log("Verifying registries...");
        var guids = AssetDatabase.FindAssets("t:" + typeof(AssetRegistryRoot).Name);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var registryRoot = AssetDatabase.LoadAssetAtPath<AssetRegistryRoot>(path);

            Debug.Log("root: " + path);

            if (registryRoot.assetRegistries == null)
            {
                Debug.Log("No entries, skipping");
                continue;
            }

            foreach (var registry in registryRoot.assetRegistries)
            {
                Debug.Log("  " + registry.name);
                var singleAssetGUIDs = new List<string>();

                var baseRegistry = registry as RegistryBase;
                if (baseRegistry != null)
                {
                    var verified = baseRegistry.Verify();
                    if (!verified)
                    {
                        ok = false;
                        Debug.Log("<color=red>ERROR- Registry:" + baseRegistry + " could not be verified</color>");
                    }
                        
                    
                    baseRegistry.GetSingleAssetGUIDs(singleAssetGUIDs, registryRoot.serverBuild);
                    foreach(var g in singleAssetGUIDs)
                    {
                        var p = AssetDatabase.GUIDToAssetPath(g);
                        var a = AssetDatabase.LoadAssetAtPath<Object>(p);
                        if(a != null)
                        {
                            Debug.Log("     - " + g + " : " + p);
                        }
                        else
                        {
                            Debug.Log("<color=red>ERROR- " + g + " : ???</color>");
                            ok = false;
                        }
                    }
                }
            }
        }
        Debug.Log(ok ? "<color=green>All good!</color>" : "<color=red>Errors found</color>");
        return ok;
    }

    public static void BuildBundles(string bundlePath, BuildTarget target, BuildAssetBundleOptions assetBundleOptions)
    {
        Debug.Log("Verifying asset registries ..");
        var ok = TestRegistries();
        if(!ok)
        {
            Debug.LogError("Asset registries appear broken.... Build failed.");
            return;
        }

        Debug.Log("Building asset registries ..");


        var guids = AssetDatabase.FindAssets("t:" + typeof(AssetRegistryRoot).Name);
        foreach (var guid in guids)
        {
            var builds = new List<AssetBundleBuild>();

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var registryRoot = AssetDatabase.LoadAssetAtPath<AssetRegistryRoot>(path);

            if (registryRoot.assetRegistries == null)
                continue;

            // Register asset registry bundle
            var registryBundleName = path;
            registryBundleName = registryBundleName.Replace("Assets/", "");
            registryBundleName = registryBundleName.Replace(".asset", "");

            Debug.Log("Building resource bundles for asset registry:" + registryBundleName);

            var paths = new List<string>();
            paths.Add(AssetDatabase.GetAssetPath(registryRoot));
            var build = new AssetBundleBuild();
            build.assetBundleName = registryBundleName;
            build.assetBundleVariant = "";
            build.assetNames = paths.ToArray();

            builds.Add(build);

            // Register single asset bundles
            var singleAssetFolder = registryBundleName + "_Assets";
            //singleAssetFolder = singleAssetFolder.ToLower();
            if (!Directory.Exists(singleAssetFolder))
                Directory.CreateDirectory(singleAssetFolder);


            var singleAssetGUIDs = new List<string>();

            // Get single assets from registries
            foreach (var registry in registryRoot.assetRegistries)
            {
                var baseRegistry = registry as RegistryBase;
                if (baseRegistry != null)
                {
                    baseRegistry.GetSingleAssetGUIDs(singleAssetGUIDs, registryRoot.serverBuild);    
                }
            }

            // Build single asset bundles
            var singleAssetBundlesHandled = new List<string>();
            foreach (var singleAssetBundleGUID in singleAssetGUIDs)
            {
                if (singleAssetBundleGUID == null || singleAssetBundleGUID == "")
                    continue;

                if (singleAssetBundlesHandled.Contains(singleAssetBundleGUID))
                    continue;

                path = AssetDatabase.GUIDToAssetPath(singleAssetBundleGUID);

                build = new AssetBundleBuild();
                build.assetBundleName = singleAssetFolder + "/" + singleAssetBundleGUID;
                build.assetBundleVariant = "";
                build.assetNames = new string[] { path };

                Debug.Log("Creating single asset bundle from asset:" + path + " Bundle name:" + build.assetBundleName);

                builds.Add(build);
                singleAssetBundlesHandled.Add(singleAssetBundleGUID);
            }


            BuildPipeline.BuildAssetBundles(bundlePath, builds.ToArray(), assetBundleOptions, target);
        }
    }
}
