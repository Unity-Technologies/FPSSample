using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.VFX;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;
using UnityEditor.SceneManagement;

public class VFXMigration
{
    /*
    [MenuItem("VFX Editor/Migrate to .vfx")]
    static void Migrate()
    {
        MigrateFolder("Assets");
        AssetDatabase.Refresh();
    }
    */

    //[MenuItem("VFX Editor/Resave All VFX assets")]
    public static void Resave()
    {
        ResaveFolder("Assets");

        AssetDatabase.SaveAssets();
    }

    static void MigrateFolder(string dirPath)
    {
        foreach (var path in Directory.GetFiles(dirPath))
        {
            if (Path.GetExtension(path) == ".asset")
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Experimental.VFX.VisualEffectAsset>(path) != null)
                {
                    string pathWithoutExtension = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path);

                    if (!File.Exists(pathWithoutExtension + ".vfx"))
                    {
                        bool success = false;

                        string message = null;
                        for (int i = 0; i < 10 && !success; ++i)
                        {
                            try
                            {
                                File.Move(path, pathWithoutExtension + ".vfx");
                                File.Move(pathWithoutExtension + ".asset.meta", pathWithoutExtension + ".vfx.meta");
                                Debug.Log("renaming " + path + " to " + pathWithoutExtension + ".vfx");
                                success = true;
                            }
                            catch (System.Exception e)
                            {
                                message = e.Message;
                            }
                        }
                        if (!success)
                        {
                            Debug.LogError(" failed renaming " + path + " to " + pathWithoutExtension + ".vfx" + message);
                        }
                    }
                }
            }
        }
        foreach (var path in Directory.GetDirectories(dirPath))
        {
            MigrateFolder(path);
        }
    }

    static void ResaveFolder(string dirPath)
    {
        foreach (var path in Directory.GetFiles(dirPath))
        {
            if (Path.GetExtension(path) == ".vfx")
            {
                VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                if (asset == null)
                {
                    AssetDatabase.ImportAsset(path);
                    asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                }

                if (asset == null)
                {
                    Debug.LogError("Couldn't Import vfx" + path);
                }

                var resource = asset.GetResource();
                if (resource != null)
                {
                    resource.ValidateAsset();
                    try
                    {
                        var graph = resource.GetOrCreateGraph();
                        graph.RecompileIfNeeded();
                        EditorUtility.SetDirty(graph);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Couldn't resave vfx" + path + " " + e.Message);
                    }
                }
            }
        }
        foreach (var path in Directory.GetDirectories(dirPath))
        {
            ResaveFolder(path);
        }
    }

    struct ComponentData
    {
        public string assetPath;
        public Dictionary<string, Dictionary<string, object>> values;
        public Dictionary<string, Dictionary<string, bool>> prefabOverrides;
    }

    class FileVFXComponents
    {
        public string path;

        public Dictionary<string, ComponentData> componentPaths;
    }

    public static void MigrateComponents()
    {
        List<FileVFXComponents> files = new List<FileVFXComponents>();
        var sceneGuids = AssetDatabase.FindAssets("t:Scene");


        HashSet<GameObject> usedPrefabs = new HashSet<GameObject>();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); // load a new scene to make sure we don't have multiple scenes loaded

        foreach (var guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            try
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
            catch (System.Exception)
            {
                //Ignore exception thrown when opening scenes.
            }
            files.Add(FindComponentsInScene(usedPrefabs));
        }


        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); // load a new scene to make sure we don't have multiple scenes loaded


        int countReferenced = usedPrefabs.Count();
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                usedPrefabs.Add(prefab);
            }
        }

        if (countReferenced < usedPrefabs.Count())
        {
            Debug.Log("found :" + (usedPrefabs.Count() - countReferenced) + " prefabs not used in any scene");
        }

        List<FileVFXComponents> prefabsInfos = new List<FileVFXComponents>();
        foreach (var prefab in usedPrefabs)
        {
            prefabsInfos.Add(FindComponentsInPrefab(prefab));
            Debug.Log("Found prefab : " + AssetDatabase.GetAssetPath(prefab));
        }

        Resave(); // Convert to the new format with the vfx assets in the library

        foreach (var prefab in prefabsInfos)
        {
            SetComponentInPrefab(prefab);
        }

        AssetDatabase.SaveAssets();

        foreach (var file in files)
        {
            EditorSceneManager.OpenScene(file.path, OpenSceneMode.Single);
            SetComponentsInScene(file);

            EditorSceneManager.SaveScene(EditorSceneManager.GetSceneByPath(file.path));
        }
    }

    public static void MigrateComponentsCurrentScnene()
    {
        HashSet<GameObject> prefabs = new HashSet<GameObject>();
        FileVFXComponents components = FindComponentsInScene(prefabs);

        List<FileVFXComponents> prefabsInfos = new List<FileVFXComponents>();
        foreach (var prefab in prefabs)
        {
            prefabsInfos.Add(FindComponentsInPrefab(prefab));
            Debug.Log("Found prefab : " + AssetDatabase.GetAssetPath(prefab));
        }

        foreach (var path in components.componentPaths.Values.Union(prefabsInfos.SelectMany(t => t.componentPaths.Values)).Distinct())
        {
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path.assetPath);
            if (asset == null)
            {
                AssetDatabase.ImportAsset(path.assetPath);
                asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path.assetPath);
            }

            if (asset != null)
            {
                var resource = asset.GetResource();
                resource.ValidateAsset();
                try
                {
                    var graph = resource.GetOrCreateGraph();
                    graph.RecompileIfNeeded();
                    EditorUtility.SetDirty(graph);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Couldn't resave vfx" + path.assetPath + " " + e.Message);
                }
            }
        }


        SetComponentsInScene(components);
        EditorSceneManager.SaveScene(EditorSceneManager.GetSceneByPath(components.path));
    }

    static FileVFXComponents FindComponentsInScene(HashSet<GameObject> prefabs)
    {
        string path = EditorSceneManager.GetActiveScene().path;
        try
        {
            var objects = EditorSceneManager.GetActiveScene().GetRootGameObjects();

            FileVFXComponents infos = new FileVFXComponents();
            infos.path = path;
            infos.componentPaths = new Dictionary<string, ComponentData>();

            foreach (var obj in objects)
            {
                FindVFXInGameObjectRecurse(infos, obj, "", prefabs, false);
            }

            return infos;
        }
        catch (System.Exception)
        {
            Debug.Log("Error analyzing scene" + path);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            throw;
        }
    }

    static FileVFXComponents FindComponentsInPrefab(GameObject prefab)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        try
        {
            FileVFXComponents infos = new FileVFXComponents();
            infos.path = path;
            infos.componentPaths = new Dictionary<string, ComponentData>();

            FindVFXInGameObjectRecurse(infos, prefab, "", null, false);

            return infos;
        }
        catch (System.Exception)
        {
            Debug.Log("Error analyzing prefab" + path);

            throw;
        }
    }

    static void SetComponentInPrefab(FileVFXComponents infos)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(infos.path);

        SetVFXInGameObjectRecurse(infos, go, "");

        EditorUtility.SetDirty(go);
    }

    static void SetComponentsInScene(FileVFXComponents infos)
    {
        var objects = EditorSceneManager.GetActiveScene().GetRootGameObjects();

        foreach (var obj in objects)
        {
            SetVFXInGameObjectRecurse(infos, obj, "");
        }
    }

    static void FindVFXInGameObjectRecurse(FileVFXComponents infos, GameObject go, string path, HashSet<GameObject> prefabs, bool isInPrefab)
    {
        if (prefabs != null && !isInPrefab && PrefabUtility.GetCorrespondingObjectFromSource(go) != null)
        {
            prefabs.Add(PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject);

            isInPrefab = true;
        }
        VisualEffect effect = go.GetComponent<VisualEffect>();
        if (effect != null)
        {
            string componentPath = path + "/" + effect.name;
            if (!object.ReferenceEquals(effect.visualEffectAsset, null))
            {
                string assetPath = AssetDatabase.GetAssetPath(effect.visualEffectAsset.GetInstanceID());

                if (string.IsNullOrEmpty(assetPath) || effect.visualEffectAsset == null)
                {
                    throw new System.InvalidOperationException("Effect+" + componentPath + " of scene" + infos.path + " invalid. Please fix it before migration");
                }

                Dictionary<string, Dictionary<string, object>> values = new Dictionary<string, Dictionary<string, object>>();
                Dictionary<string, Dictionary<string, bool>> prefaboverride = new Dictionary<string, Dictionary<string, bool>>();

                SerializedObject obj = new SerializedObject(effect);

                if (assetPath.Contains("Gradient"))
                {
                    Debug.Log("");
                }
                bool hasOneValue = false;

                foreach (var setter in m_Setters)
                {
                    string property = "m_PropertySheet." + setter.Key + ".m_Array";

                    SerializedProperty arrayProp = obj.FindProperty(property);
                    if (arrayProp.arraySize > 0)
                    {
                        values[setter.Key] = new Dictionary<string, object>();
                        for (int i = 0; i < arrayProp.arraySize; ++i)
                        {
                            var elementProp = arrayProp.GetArrayElementAtIndex(i);

                            var overridenProp = elementProp.FindPropertyRelative("m_Overridden");
                            bool overridenInPrefab = isInPrefab && overridenProp.prefabOverride;

                            var valueProp = elementProp.FindPropertyRelative("m_Value");

                            if ((!isInPrefab && overridenProp.boolValue) || (isInPrefab && valueProp.prefabOverride))
                            {
                                values[setter.Key].Add(elementProp.FindPropertyRelative("m_Name").stringValue, setter.Value.get(valueProp));
                                hasOneValue = true;
                            }

                            if (overridenInPrefab)
                            {
                                if (!prefaboverride.ContainsKey(setter.Key))
                                {
                                    prefaboverride[setter.Key] = new Dictionary<string, bool>();
                                }
                                prefaboverride[setter.Key].Add(elementProp.FindPropertyRelative("m_Name").stringValue, overridenProp.boolValue);
                                hasOneValue = true;
                            }
                        }
                    }
                }

                //Skip components that are in a prefab and don't have overridden parameters because some have the same path.
                bool hasAssetSet = !isInPrefab || obj.FindProperty("m_Asset").prefabOverride;
                if (hasOneValue || hasAssetSet)
                {
                    if (infos.componentPaths.ContainsKey(componentPath))
                    {
                        Debug.LogError("Two components have the path" + componentPath);
                    }
                    infos.componentPaths.Add(componentPath, new ComponentData() { assetPath = hasAssetSet ? assetPath : null, values = values, prefabOverrides = prefaboverride });
                }
            }
            else
            {
                Debug.LogWarning("VisualEffect has no asset" + componentPath + " of scene" + infos.path);
            }
        }

        foreach (UnityEngine.Transform child in go.transform)
        {
            FindVFXInGameObjectRecurse(infos, child.gameObject, path + "/" + go.name, prefabs, isInPrefab);
        }
    }

    static void SetVFXInGameObjectRecurse(FileVFXComponents infos, GameObject go, string path)
    {
        VisualEffect effect = go.GetComponent<VisualEffect>();
        if (effect != null)
        {
            string componentPath = path + "/" + effect.name;


            ComponentData componentData;
            if (infos.componentPaths.TryGetValue(componentPath, out componentData))
            {
                //if (effect.visualEffectAsset == null)
                {
                    string assetPath = componentData.assetPath;
                    if (assetPath != null)
                    {
                        VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(componentData.assetPath);

                        if (asset == null)
                        {
                            Debug.Log("Couldn't load used asset:" + componentData.assetPath);
                        }

                        EditorUtility.SetDirty(effect);
                        effect.visualEffectAsset = asset;
                    }
                    else
                    {
                        if (effect.visualEffectAsset == null)
                        {
                            Debug.LogError("Component " + componentPath + " asset should have been set with its prefab be didn't in scene " + infos.path);
                        }
                    }

                    SerializedObject obj = new SerializedObject(effect);

                    foreach (var value in componentData.values)
                    {
                        string property = "m_PropertySheet." + value.Key + ".m_Array";
                        SerializedProperty arrayProp = obj.FindProperty(property);
                        foreach (var setter in value.Value)
                        {
                            bool found = false;
                            for (int i = 0; i < arrayProp.arraySize; ++i)
                            {
                                var elementProp = arrayProp.GetArrayElementAtIndex(i);

                                if (elementProp.FindPropertyRelative("m_Name").stringValue == setter.Key)
                                {
                                    m_Setters[value.Key].set(elementProp.FindPropertyRelative("m_Value"), setter.Value);

                                    elementProp.FindPropertyRelative("m_Overridden").boolValue = true;
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                Debug.LogWarning("Asset : " + assetPath + " no longer seems to have a parameter " + setter.Key + " of type " + value.Key.Substring(2) + "referenced from "  + componentPath + " in scene" + infos.path);
                            }
                            else
                            {
                                Debug.Log("Asset : " + assetPath + " restored parameter " + setter.Key + " of type " + value.Key.Substring(2) + "referenced from " + componentPath + " in scene" + infos.path);
                            }
                        }
                    }

                    foreach (var value in componentData.prefabOverrides)
                    {
                        string property = "m_PropertySheet." + value.Key + ".m_Array";
                        SerializedProperty arrayProp = obj.FindProperty(property);
                        foreach (var setter in value.Value)
                        {
                            for (int i = 0; i < arrayProp.arraySize; ++i)
                            {
                                var elementProp = arrayProp.GetArrayElementAtIndex(i);

                                if (elementProp.FindPropertyRelative("m_Name").stringValue == setter.Key)
                                {
                                    elementProp.FindPropertyRelative("m_Overridden").boolValue = setter.Value;
                                    break;
                                }
                            }
                        }
                    }

                    obj.ApplyModifiedProperties();

                    Debug.Log("Restoring component :" + componentPath + "of scene :" + infos.path + " to have asset :" + assetPath);
                }
            }
        }

        foreach (UnityEngine.Transform child in go.transform)
        {
            SetVFXInGameObjectRecurse(infos, child.gameObject, path + "/" + go.name);
        }
    }

    static string GetComponentPath(Component c)
    {
        if (c.transform.parent == null)
            return c.name;

        return GetComponentPath(c.transform.parent) + "/" + c.name;
    }

    struct PropertyInfo
    {
        public System.Action<SerializedProperty, object> set;
        public System.Func<SerializedProperty, object> get;
    }


        #pragma warning disable 0414
    static Dictionary<System.Type, string> m_Properties = new Dictionary<System.Type, string>()
    {
        { typeof(Vector2), "m_Vector2f"},
        { typeof(Vector3), "m_Vector3f"},
        { typeof(Vector4), "m_Vector4f"},
        { typeof(Color), "m_Vector4f"},
        { typeof(AnimationCurve), "m_AnimationCurve"},
        { typeof(Gradient), "m_Gradient"},
        { typeof(Texture2D), "m_NamedObject"},
        { typeof(Texture2DArray), "m_NamedObject"},
        { typeof(Texture3D), "m_NamedObject"},
        { typeof(Cubemap), "m_NamedObject"},
        { typeof(CubemapArray), "m_NamedObject"},
        { typeof(float), "m_Float"},
        { typeof(int), "m_Int"},
        { typeof(uint), "m_Uint"},
        { typeof(bool), "m_Bool"},
        { typeof(Matrix4x4), "m_Matrix4x4f"}
    };
        #pragma warning restore 0414


    static Dictionary<string, PropertyInfo> m_Setters = new Dictionary<string, PropertyInfo>()
    {
        {"m_Vector2f", new PropertyInfo() {set = (SerializedProperty p, object o) => p.vector2Value = (Vector2)o, get = (SerializedProperty p) => p.vector2Value} },
        {"m_Vector3f", new PropertyInfo() {set = (SerializedProperty p, object o) => p.vector3Value = (Vector3)o, get = (SerializedProperty p) => p.vector3Value} },
        {"m_Vector4f", new PropertyInfo() {set = (SerializedProperty p, object o) => p.vector4Value = (Vector4)o, get = (SerializedProperty p) => p.vector4Value} },
        {"m_AnimationCurve", new PropertyInfo() {set = (SerializedProperty p, object o) => p.animationCurveValue = (AnimationCurve)o, get = (SerializedProperty p) => p.animationCurveValue} },
        {"m_Gradient", new PropertyInfo() {set = (SerializedProperty p, object o) => p.gradientValue = (Gradient)o, get = (SerializedProperty p) => p.gradientValue} },
        {"m_NamedObject", new PropertyInfo() {set = (SerializedProperty p, object o) => p.objectReferenceValue = (UnityEngine.Object)o, get = (SerializedProperty p) => p.objectReferenceValue} },
        {"m_Float", new PropertyInfo() {set = (SerializedProperty p, object o) => p.floatValue = (float)o, get = (SerializedProperty p) => p.floatValue} },
        {"m_Int", new PropertyInfo() {set = (SerializedProperty p, object o) => p.intValue = (int)o, get = (SerializedProperty p) => p.intValue} },
        {"m_Uint", new PropertyInfo() {set = (SerializedProperty p, object o) => p.longValue = (long)(uint)o, get = (SerializedProperty p) => (uint)p.longValue} },
        {"m_Bool", new PropertyInfo() {set = (SerializedProperty p, object o) => p.boolValue = (bool)o, get = (SerializedProperty p) => p.boolValue} },
    };
}
