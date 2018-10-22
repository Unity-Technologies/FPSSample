using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class WeakSoundDef : Weak<SoundDef>
{
    public bool IsValid() { return guid != ""; }
}

[CreateAssetMenu(fileName = "Sound", menuName = "FPS Sample/Audio/SoundBank", order = 10000)]
public class SoundBank : ScriptableObject
{
    public List<SoundDef> soundDefs;

    public List<string> soundDefGuids;

    public SoundDef FindByName(string name)
    {
        foreach(var s in soundDefs)
        {
            if (s.name == name)
                return s;
        }
        return null;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        soundDefGuids.Clear();
        foreach(var s in soundDefs)
        {
            var p = AssetDatabase.GetAssetPath(s);
            soundDefGuids.Add(AssetDatabase.AssetPathToGUID(p));
        }
    }
#endif
}
