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
    public string playerName;
    public int testId;
    public string userCity;
    public string userCountry;
    public string connectionType;
    public GDNData gdnData;
}


//add Change and Flush methods

public class RwConfig {
    public const string TextAssetPath = "configGDN";
    public const string DefualtFileName = "ConfigGDN.json";
    private static ConfigData _configData;
    private static bool _read = false;
    private static string _path;
    private static bool _dirty = false;
    private static string _prevFileName = "";
    
    public static void Change(ConfigData data) {
        _configData = data;
        _dirty = true;
    }
    
    public static void Flush() {
        if (_dirty) {
            GameDebug.Log("Flush: "+_prevFileName);
            WriteConfig(_configData,_prevFileName);
        }
    }
    
    protected static void WriteConfig( ConfigData data, string fileName = "" ) {
        if (fileName == "") {
            fileName = DefualtFileName;
        }
        _prevFileName = fileName;
        _configData = data;
        _path = Application.dataPath + "/"+fileName;
        string contents = JsonUtility.ToJson(data);
        GameDebug.Log("WriteConfig: "+_path);
        File.WriteAllText(_path, contents);
        _dirty = false;
    }

    public static ConfigData ReadConfig(string fileName = "") {
        if (fileName == "") {
            fileName = DefualtFileName;
        } 
        _prevFileName = fileName;
        return ReadConfig(fileName,Resources.Load<TextAsset>(TextAssetPath));
    }
    
    public static ConfigData ReadConfig(string fileName, TextAsset defaultConfig) {
        if (_read) {
            GDNStats.playerName = _configData.playerName;
            return _configData;
        } else {
            string path = Application.dataPath + "/" + fileName;
            if (File.Exists(path)) {
                _configData = JsonUtility.FromJson<ConfigData>(File.ReadAllText(path));
                _read = true;
                _dirty = false;
                GameDebug.Log("Read Config: "+ fileName);
                return _configData;
            } else {
                _configData = JsonUtility.FromJson<ConfigData>(defaultConfig.text);
                _read = true;
                _dirty = false;
                WriteConfig(_configData,fileName);
                GameDebug.Log("WriteConfig: text asset : "+fileName );
                return _configData;
            }
        }
    }   

}
