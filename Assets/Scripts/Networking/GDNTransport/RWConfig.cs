using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Macrometa;
using Object = UnityEngine.Object;

[Serializable]
public struct ConfigData {
    public bool isServer;
    public string gameName;
    public GDNData gdnData;
}

public class RWConfig {
    public static void WriteConfig(string fileName, ConfigData data) {
        string path = Application.dataPath + "/"+fileName;
            string contents = JsonUtility.ToJson(data);
            File.WriteAllText(path, contents);
    }

    public static ConfigData ReadConfig(string fileName, TextAsset defaultConfig) {
        string path = Application.dataPath + "/"+fileName;
        if (File.Exists(path)) {
            return JsonUtility.FromJson<ConfigData>(File.ReadAllText(path));
        }
        else {
            var configData = JsonUtility.FromJson<ConfigData>(defaultConfig.text);
            WriteConfig(fileName,configData);
            return configData;
        }
    }   

}
