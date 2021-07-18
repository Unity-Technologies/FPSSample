using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Macrometa;
using Object = UnityEngine.Object;

[Serializable]
public struct ConfigData {
    public int dummyTrafficQuantity;
    public int statsGroupSize; 
    public bool isServer;
    public string gameName;
    public int testId;
    public GDNData gdnData;
}


//add Change and Flush methods

public class RwConfig {
    public const string TextAssetPath = "configGDN";
    public const string DefualtFileName = "ConfigGDN.json";
    private static ConfigData _configData;
    private static bool _read = false;
    private static string _path;


    public static void Change(ConfigData data) {
        _configData = data;
    }
    
    public static void Flush() {
        WriteConfig(_configData, _path);
    }
    
    public static void WriteConfig( ConfigData data, string fileName = "" ) {
        if (fileName == "") {
            fileName = DefualtFileName;
        }
        _configData = data;
        _path = Application.dataPath + "/"+fileName;
        string contents = JsonUtility.ToJson(data);
        File.WriteAllText(_path, contents);
    }

    public static ConfigData ReadConfig(string fileName = "") {
        if (fileName == "") {
            fileName = DefualtFileName;
        }
        return ReadConfig(fileName,Resources.Load<TextAsset>(TextAssetPath));
    }
    
    public static ConfigData ReadConfig(string fileName, TextAsset defaultConfig) {
        if (_read) {
            return _configData;
        } else {
            string path = Application.dataPath + "/" + fileName;
            if (File.Exists(path)) {
                _configData = JsonUtility.FromJson<ConfigData>(File.ReadAllText(path));
                _read = true;
                return _configData;
            } else {
                _configData = JsonUtility.FromJson<ConfigData>(defaultConfig.text);
                _read = true;
                WriteConfig(_configData,fileName);
                return _configData;
            }
        }
    }   

}
