using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum SoundMixerGroup
{
    Music,
    SFX,
    Menu,
    _Count
}

[CreateAssetMenu(fileName = "Sound", menuName = "FPS Sample/Audio/SoundDef", order = 10000)]
public class SoundDef : ScriptableObject
{
    public List<AudioClip> clips;
    [Range(-60.0f,0.0f)]
    public float volume = -6.0f;
//    public float gain = -60.0f; // could be a lower floor on rolloff func
    [Range(0.1f,100.0f)]
    public float distMin = 1.5f;
    [Range(0.1f,100.0f)]
    public float distMax = 30.0f;
    [Range(-20,20.0f)]
    public float pitchMin = 0.0f;
    [Range(-20,20.0f)]
    public float pitchMax = 0.0f;
    [Range(0,10)]
    public int loopCount = 1;
    [Range(0,10.0f)]
    public float delayMin = 0.0f;
    [Range(0,10.0f)]
    public float delayMax = 0.0f;
    [Range(1,20)]
    public int repeatMin = 1;
    [Range(1,20)]
    public int repeatMax = 1;
    public SoundMixerGroup soundGroup;
    [Range(0.0f,1.0f)]
    public float spatialBlend = 1.0f;
    [Range(-1.0f,1.0f)]
    public float panMin = 0.0f;
    [Range(-1.0f,1.0f)]
    public float panMax = 0.0f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    public void OnValidate()
    {
        if (distMin > distMax)
            distMin = distMax;
        if (pitchMin > pitchMax)
            pitchMin = pitchMax;
        if (delayMin > delayMax)
            delayMin = delayMax;
        if (repeatMin > repeatMax)
            repeatMin = repeatMax;
        if (panMin > panMax)
            panMin = panMax;
    }
}
