using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BundledResourceManager  {      

    [ConfigVar(Name = "resources.forcebundles", DefaultValue = "0", Description = "Force use of bundles even in editor")]
    static ConfigVar forceBundles;

    [ConfigVar(Name = "resources.verbose", DefaultValue = "0", Description = "Verbose logging about resources")]
    static ConfigVar verbose;

    private string GetBundlePath()
    {
#if UNITY_PS4
        return Application.streamingAssetsPath + "/" + SimpleBundleManager.assetBundleFolder;
#else
        string bundlePath = SimpleBundleManager.GetRuntimeBundlePath();
        return bundlePath;
#endif
    }
                                 
    public BundledResourceManager(string registryName)
    {
        bool useBundles = !Application.isEditor || forceBundles.IntValue > 0;
      

#if UNITY_EDITOR
        if (!useBundles)
        {
            string assetPath = "Assets/" + registryName + ".asset";
            m_assetRegistryRoot = AssetDatabase.LoadAssetAtPath<AssetRegistryRoot>(assetPath);
            if (verbose.IntValue > 0)
                GameDebug.Log("resource: loading resource: " + assetPath);
        }
#endif

        if (useBundles)
        {
            var bundlePath = GetBundlePath();
            var assetPath = bundlePath + "/" + registryName;

            if (verbose.IntValue > 0)
                GameDebug.Log("resource: loading bundle (" + assetPath + ")");
            m_assetRegistryRootBundle = AssetBundle.LoadFromFile(assetPath);     

            var registryRoots = m_assetRegistryRootBundle.LoadAllAssets<AssetRegistryRoot>();

            if(registryRoots.Length == 1)
                m_assetRegistryRoot = registryRoots[0];
            else
                GameDebug.LogError("Wrong number(" + registryRoots.Length + ") of registry roots in "+ registryName);
        }

        // Update asset registry map
        if(m_assetRegistryRoot != null)
        {
            foreach(var registry in m_assetRegistryRoot.assetRegistries)
            {
                if(registry == null)
                {
                    continue;
                }

                System.Type type = registry.GetType();
                m_assetRegistryMap.Add(type, registry);
            }
            m_assetResourceFolder = registryName + "_Assets";
        }
    }

    public void Shutdown()
    {
        foreach(var bundle in m_singleResourceBundles.Values)
        {
            // If we are in editor we may not have loaded these as bundles
            if(bundle.bundle != null)
                bundle.bundle.Unload(false);
        }

        if(m_assetRegistryRootBundle != null)
            m_assetRegistryRootBundle.Unload(false);      

        m_assetRegistryRoot = null;
        m_assetRegistryRootBundle = null;
        m_assetRegistryMap.Clear();
        m_assetResourceFolder = "";
        m_singleResourceBundles.Clear();
    }
    

    public T GetResourceRegistry<T>() where T : ScriptableObject
    {
        ScriptableObject result = null;
        m_assetRegistryMap.TryGetValue(typeof(T), out result);
        return (T)result;
    }

    public Object LoadSingleAssetResource(string guid)        
    {
        var def = new SingleResourceBundle();

        if(m_singleResourceBundles.TryGetValue(guid, out def)) {
            return def.asset;
        }

        def = new SingleResourceBundle();
        var useBundles = !Application.isEditor || forceBundles.IntValue > 0;

#if UNITY_EDITOR
        if(!useBundles)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            def.asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

            if (def.asset == null)
                GameDebug.LogWarning("Failed to load resource " + guid + " at " + path);
            if (verbose.IntValue > 0)
                GameDebug.Log("resource: loading non-bundled asset " + path + "(" + guid + ")");
        }
#endif
        if(useBundles)
        {
            var bundlePath = GetBundlePath();
            def.bundle = AssetBundle.LoadFromFile(bundlePath + "/" + m_assetResourceFolder + "/" + guid);
            if (verbose.IntValue > 0)
                GameDebug.Log("resource: loading bundled asset: " + m_assetResourceFolder + "/" + guid);
            var handles = def.bundle.LoadAllAssets();
            if (handles.Length > 0)
                def.asset = handles[0];
            else
                GameDebug.LogWarning("Failed to load resource " + guid);
        }

        m_singleResourceBundles.Add(guid, def);
        return def.asset;
    }

    class SingleResourceBundle
    {
        public AssetBundle bundle;
        public Object asset;
    }
    
    AssetRegistryRoot m_assetRegistryRoot;
    AssetBundle m_assetRegistryRootBundle;
    Dictionary<System.Type, ScriptableObject> m_assetRegistryMap = new Dictionary<System.Type, ScriptableObject>();
    string m_assetResourceFolder = "";

    Dictionary<string, SingleResourceBundle> m_singleResourceBundles = new Dictionary<string, SingleResourceBundle>();
}
