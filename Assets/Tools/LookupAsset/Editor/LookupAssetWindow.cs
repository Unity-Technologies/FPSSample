using UnityEditor;
using UnityEngine;


public class LookupAssetWindow : EditorWindow
{
    [MenuItem("FPS Sample/Hotkeys/Lookup asset guid %&l")]
    static void LookupAsset()
    {
        Open();
    }
    
    public static void Open()
    {
        LookupAssetWindow window = GetWindow<LookupAssetWindow>(false);
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 800, 150);
        window.ShowPopup();
        guid = "";
    }

    static string guid;
    static Object asset;
    static string path;

    void OnGUI()
    {
        EditorGUILayout.LabelField("Enter asset GUID below", EditorStyles.wordWrappedLabel);
        GUILayout.Space(20);

        EditorGUI.BeginChangeCheck();
        guid = EditorGUILayout.TextField("GUID", guid);
        var guidChanged = EditorGUI.EndChangeCheck();

        EditorGUI.BeginChangeCheck();
        path = EditorGUILayout.TextField("Asset", path);
        var pathChanged = EditorGUI.EndChangeCheck();

        EditorGUI.BeginChangeCheck();
        asset = EditorGUILayout.ObjectField("Asset", asset, typeof(Object), false);
        var assetChanged = EditorGUI.EndChangeCheck();

        GUILayout.Space(20);
        if (GUILayout.Button("Close"))
            this.Close();

        if(guidChanged)
        {
            path = AssetDatabase.GUIDToAssetPath(guid);
            asset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
        }
        else if (pathChanged)
        {
            asset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            guid = AssetDatabase.AssetPathToGUID(path);
        }
        else if (assetChanged)
        {
            path = AssetDatabase.GetAssetPath(asset);
            guid = AssetDatabase.AssetPathToGUID(path);
        }

    }
}

