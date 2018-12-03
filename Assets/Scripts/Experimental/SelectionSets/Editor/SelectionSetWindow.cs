using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SelectionSetWindow : EditorWindow
{
    [MenuItem("FPS Sample/Windows/Selection Set")]
    public static void ShowWindow()
    {
        var window = GetWindow<SelectionSetWindow>(false, "Selection Sets", true);
        window.LoadSets();
    }
    
    private const string k_editorPrefKey = "SelectionSetWindow";
    
    [Serializable]
    public class Set
    {
        public string name = "New Set";
        public bool expanded = true;
        public List<Object> entries = new List<Object>();
    }

    [Serializable]
    public class SceneObjectRef
    {
        public int sceneId;
        public int fileId;
    }
    
    [Serializable]
    public class SerializedSet
    {
        public string name;
        public List<string> assetGuids = new List<string>();
        public List<int> sceneObjects = new List<int>();
    }
    
    public List<Set> sets = new List<Set>();
    private GUIStyle referenceStyle;
    private Vector2 scrollViewPos;
    
    

    private void OnDisable()
    {
        SaveSets();
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    void OnGUI()
    {
        if (referenceStyle == null)
        {
            referenceStyle = SelectionHistoryWindow.CreateObjectReferenceStyle();
        }
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Create New"))
        {
            CreateNewSet();
        }

        GUILayout.EndVertical();

        scrollViewPos = GUILayout.BeginScrollView(scrollViewPos);

        Set deletedSet = null;
        foreach (var set in sets)
        {
            GUILayout.BeginHorizontal();
            
            var expandButtonText = set.expanded ? "^" : "v";
            if (GUILayout.Button(expandButtonText, GUILayout.Width(20)))
                set.expanded = !set.expanded;

             
            if (GUILayout.Button("S", GUILayout.Width(20)))
            {
                Selection.objects = set.entries.ToArray();
            }

            set.name = GUILayout.TextField(set.name);

            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                AddToSet(set);
            }   
            
            if (GUILayout.Button("Set", GUILayout.Width(50)))
            {
                OverrideSet(set);
            }   
            
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                var result = EditorUtility.DisplayDialog("Delete set?",
                    "Are you sure you want to delete set:" + set.name, "Delete", "Cancel");
                if (result)
                    deletedSet = set;
            }   
            
            GUILayout.EndHorizontal();
            if (!set.expanded)
                continue;

            UnityEngine.Object deletedEntry = null;
            foreach (var entry in set.entries)
            {
                GUILayout.BeginHorizontal();
                
                SelectionHistoryWindow.DrawObjectReference(entry,referenceStyle);

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    deletedEntry = entry;
                }    
                
                
                GUILayout.EndHorizontal();
            }

            if (deletedEntry != null)
            {
                Undo.RecordObject(this,"Remove from set");
                set.entries.Remove(deletedEntry);
                SaveSets();
                Repaint();
            }
        }
        
        GUILayout.EndScrollView();

        if (deletedSet != null)
        {
            Undo.RecordObject(this,"Delete set");
            sets.Remove(deletedSet);
            SaveSets();
            Repaint();
        }
    }
    
    void CreateNewSet()
    {
        Undo.RecordObject(this,"Create set");
        
        var set = new Set();
        set.entries = new List<Object>(Selection.objects);
        
        sets.Add(set);

        SaveSets();
        Repaint();
    }

    void OverrideSet(Set set)
    {
        if (Selection.objects == null)
            return;

        if (Selection.objects.Length == 0)
            return;
        
        set.entries.Clear();
        AddToSet(set);
    }
    
    void AddToSet(Set set)
    {
        if (Selection.objects == null)
            return;

        if (Selection.objects.Length == 0)
            return;

        Undo.RecordObject(this,"Add to set");

        foreach (var o in Selection.objects)
        {
            if (set.entries.Contains(o))
                continue;
            set.entries.Add(o);
        }

        SaveSets();
        Repaint();
    }

    void SaveSets()
    {
        var serializedSets = new List<SerializedSet>();

        foreach (var set in sets)
        {
            var serializedSet = new SerializedSet
            {
                name = set.name,
            };

            foreach (var entry in set.entries)
            {
                var assetPath = AssetDatabase.GetAssetPath(entry);
                if (assetPath != null)
                {
                    var guid = AssetDatabase.AssetPathToGUID(assetPath);
                    serializedSet.assetGuids.Add(guid);
                    continue;
                }

                var gameObject = entry as GameObject;
                if (gameObject != null)
                {
                    var sceneHandle = gameObject.scene.handle;

                  //  var fileId = gameObject.m_LocalIdentfierInFile;
                }
            }
            
            serializedSets.Add(serializedSet);
        }

        EditorPrefs.SetInt(k_editorPrefKey + "Count", serializedSets.Count);

        for (int i = 0; i < serializedSets.Count; i++)
        {
            var val =  JsonUtility.ToJson(serializedSets[i]);
            var name = k_editorPrefKey + "_" + i;
            EditorPrefs.SetString(name, val);
        }
    }

    void LoadSets()
    {
        sets.Clear();
        
        var count = EditorPrefs.GetInt(k_editorPrefKey + "Count", -1);
        if (count == -1)
            return;

        
        
        for (int i = 0; i < count; i++)
        {
            var name = k_editorPrefKey + "_" + i;
            var val = EditorPrefs.GetString(name, null);
            if (val == null)
                continue;

            var serializedSet = JsonUtility.FromJson<SerializedSet>(val);

            var set = new Set();
            set.name = serializedSet.name;

            foreach (var guid in serializedSet.assetGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                set.entries.Add(asset);
            }
            
            sets.Add(set);
        }
    }
}
