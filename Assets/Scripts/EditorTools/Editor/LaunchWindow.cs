using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Networking.PlayerConnection;
#if UNITY_EDITOR
using UnityEditor;

public class LaunchWindow : EditorWindow
{

    [Serializable]
    class Entry
    {
        public bool runInEditor;
        public string name = "Entry name";
        public int count = 1;
        public bool selected;
        public string arguments;
        public bool showArguments;
    }

    [Serializable]
    class Data
    {
        public List<Entry> entries = new List<Entry>();
    }


    [MenuItem("FPS Sample/Windows/LaunchWindow")]
    public static void ShowWindow()
    {
        GetWindow<LaunchWindow>(false, "Launch", true);
    }

    private void OnEnable()
    {
        var str = EditorPrefs.GetString(editorPrefName, "");
        if (str != "")
            data = JsonUtility.FromJson<Data>(str);
        else
            data = new Data();
    }

    void OnGUI()
    {
        var defaultGUIColor = GUI.color;
        var defaultGUIBackgrounColor = GUI.backgroundColor;
            
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginVertical();


        // Quick start buttons
        GUILayout.BeginHorizontal();
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Start Selected"))
            {
                for (var i = 0; i < data.entries.Count; i++)
                {
                    var entry = data.entries[i];
                    if (!entry.selected)
                        continue;
                    StartEntry(data.entries[i]);
                }
            }
            GUI.backgroundColor = defaultGUIBackgrounColor;

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Stop All"))
            {
                StopAll();
            }
            GUI.backgroundColor = defaultGUIBackgrounColor;

            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                data.entries.Add(new Entry());
            }
        }
        GUILayout.EndHorizontal();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Draw entries
        for (var i = 0; i < data.entries.Count; i++)
        {
            var entry = data.entries[i];
            //var style = "Box"; //  entry.runInEditor ? "selectionRect" : "Box";

            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
            {
                GUI.backgroundColor = entry.selected ? Color.green : defaultGUIBackgrounColor;
                if (GUILayout.Button("S", GUILayout.Width(20)))
                    entry.selected = !entry.selected;
                GUI.backgroundColor = defaultGUIBackgrounColor;

                entry.name = EditorGUILayout.TextField(entry.name);

                if (GUILayout.Button(entry.showArguments ? "^" : "v", GUILayout.Width(30)))
                    entry.showArguments = !entry.showArguments;

                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Start", GUILayout.Width(50)))
                {
                    StartEntry(entry);
                }
                GUI.backgroundColor = defaultGUIBackgrounColor;

                entry.count = EditorGUILayout.IntField(entry.count, GUILayout.Width(30), GUILayout.ExpandWidth(false));

                GUI.backgroundColor = entry.runInEditor ? Color.yellow : GUI.backgroundColor;
                
               
               
                GUI.backgroundColor = defaultGUIBackgrounColor;
                
                var runInEditor = GUILayout.Toggle(entry.runInEditor, "Editor", new GUIStyle("Button"), GUILayout.Width(50));
                if (runInEditor != entry.runInEditor)
                {
                    for (var j = 0; j < data.entries.Count; j++)
                        data.entries[j].runInEditor = false;
                    entry.runInEditor = runInEditor;
                }

            }
            GUILayout.EndHorizontal();

            if (entry.showArguments)
            {
                entry.arguments = EditorGUILayout.TextArea(entry.arguments);
            }
        }


        GUILayout.FlexibleSpace();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
        {
            var json = JsonUtility.ToJson(data);
            EditorPrefs.SetString(editorPrefName, json);
        }
    }

    void StartEntry(Entry entry)
    {
        
        // Convert line breaks to space and remove commented out lines
        var lines = entry.arguments.Split(new String[] { "\r\n","\n"  }, StringSplitOptions.RemoveEmptyEntries).ToList();
        lines.RemoveAll(str => str.Contains("//"));
        var args = "";
        lines.ForEach(str => args += str + " ");

        int standaloneCount = entry.count;
        if (!Application.isPlaying && entry.runInEditor)
        {
            EditorLevelManager.StartGameInEditor(args);
            standaloneCount--;
        }
            
        for (var i = 0; i < standaloneCount; i++)
        {
            //if (allowDevBuild && entry.useDevBuild)
            //    RunDevBuild(args);
            //else
            BuildWindow.RunBuild(args + " -title \"" + entry.name + "\"");
        }
    }

    static string GetBuildPath(BuildTarget buildTarget)
    {
        if (buildTarget == BuildTarget.PS4)
            return "AutoBuildPS4";
        else
            return "AutoBuild";
    }

    static string GetDevBuildPath(BuildTarget buildTarget)
    {
        var path = GetBuildPath(buildTarget);
        return path + "_Dev";
    }

    static string GetBuildExeName(BuildTarget buildTarget)
    {
        if (buildTarget == BuildTarget.PS4)
            return "AutoBuild";
        else
            return "AutoBuild.exe";
    }

    static string GetBuildExe(BuildTarget buildTarget)
    {
        if (buildTarget == BuildTarget.PS4)
            return "AutoBuild/AutoBuild.bat";
        else
            return "AutoBuild.exe";
    }
    
    static void StopAll()
    {
        EditorApplication.isPlaying = false;

        var buildExe = GetBuildExe(EditorUserBuildSettings.activeBuildTarget);

        var processName = Path.GetFileNameWithoutExtension(buildExe);
        var processes = System.Diagnostics.Process.GetProcesses();
        foreach (var process in processes)
        {
            if (process.HasExited)
                continue;

            if (process.ProcessName != null && process.ProcessName == processName)
            {
                process.Kill();
            }
        }
    }


    const string editorPrefName = "LauchWindowData";
    Data data;
    Vector2 scrollPosition;
}

#endif