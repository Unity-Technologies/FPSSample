using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

public class SoundSystemNull : ISoundSystem
{
    public void Init(AudioMixer mixer) { }

    public void MountBank(SoundBank bank) { }

    public SoundSystem.SoundHandle Play(SoundDef soundDef)
    {
        return default(SoundSystem.SoundHandle);
    }

    public SoundSystem.SoundHandle Play(SoundDef soundDef, Transform parent)
    {
        return default(SoundSystem.SoundHandle);
    }

    public SoundSystem.SoundHandle Play(SoundDef soundDef, Vector3 position)
    {
        return default(SoundSystem.SoundHandle);
    }

    public SoundSystem.SoundHandle Play(WeakSoundDef weakSoundDef)
    {
        return default(SoundSystem.SoundHandle);
    }

    public void SetCurrentListener(AudioListener audioListener) { }

    public void Stop(SoundSystem.SoundHandle sh, float fadeOutTime = 0) { }

    public void UnmountBank(SoundBank bank) { }

    public void Update() { }
}

public class SoundSystem : ISoundSystem
{
    [ConfigVar(Name = "sound.debug", DefaultValue = "0", Description = "Enable sound debug overlay")]
    public static ConfigVar soundDebug;

    [ConfigVar(Name = "sound.numemitters", DefaultValue = "48", Description = "Number of sound emitters")]
    public static ConfigVar soundNumEmitters;

    [ConfigVar(Name = "sound.spatialize", DefaultValue = "1", Description = "Use spatializer")]
    public static ConfigVar soundSpatialize;

    [ConfigVar(Name = "sound.mute", DefaultValue = "-1", Description = "Is audio enabled. -1 causes default behavior (on when window has focus)", Flags = ConfigVar.Flags.None)]
    public static ConfigVar soundMute;

    // Debugging only
    [ConfigVar(Name = "sound.mastervol", DefaultValue = "1", Description = "Master volume", Flags = ConfigVar.Flags.None)]
    public static ConfigVar soundMasterVol;

    // Exposed in options menu
    [ConfigVar(Name = "sound.menuvol", DefaultValue = "1", Description = "Menu volume", Flags = ConfigVar.Flags.None)]
    public static ConfigVar soundMenuVol;
    [ConfigVar(Name = "sound.sfxvol", DefaultValue = "1", Description = "SFX volume", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar soundSFXVol;
    [ConfigVar(Name = "sound.musicvol", DefaultValue = "1", Description = "Music volume", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar soundMusicVol;

    // These are passed to the game code
    public struct SoundHandle
    {
        public SoundEmitter emitter;
        public int seq;
        public bool IsValid() { return emitter != null && emitter.seqId == seq; }
        public SoundHandle(SoundEmitter e)
        {
            emitter = e;
            seq = e != null ? e.seqId : -1;
        }
    }

    // These are internal to the SoundSystem
    public class SoundEmitter
    {
        public AudioSource source;
        public SoundDef soundDef;
        public bool playing;
        public int repeatCount;
        public Interpolator fadeToKill;
        public int seqId;

        internal void Kill()
        {
            source.Stop();
            repeatCount = 0;
        }
    };

    AudioSource MakeAudioSource()
    {
        var go = new GameObject("SoundSystemSource");
        go.transform.parent = m_SourceHolder.transform;
        return go.AddComponent<AudioSource>();
    }

    static AudioMixerGroup[] s_MixerGroups;
    public void Init(AudioMixer mixer)
    {
        m_SourceHolder = new GameObject("SoundSystemSources");
        GameObject.DontDestroyOnLoad(m_SourceHolder);
        m_AudioMixer = mixer;
        GameDebug.Log("SoundSystem using mixer: " + m_AudioMixer.name);
        m_SequenceId = 0;

        // Create pool of emitters
        m_Emitters = new SoundEmitter[soundNumEmitters.IntValue];
        for (var i = 0; i < soundNumEmitters.IntValue; i++)
        {
            var emitter = new SoundEmitter();
            emitter.source = MakeAudioSource();
            emitter.fadeToKill = new Interpolator(1.0f, Interpolator.CurveType.Linear);
            m_Emitters[i] = emitter;
        }

        // Set up mixer groups
        s_MixerGroups = new AudioMixerGroup[(int)SoundMixerGroup._Count];
        s_MixerGroups[(int)SoundMixerGroup.Menu] = m_AudioMixer.FindMatchingGroups("Menu")[0];
        s_MixerGroups[(int)SoundMixerGroup.Music] = m_AudioMixer.FindMatchingGroups("Music")[0];
        s_MixerGroups[(int)SoundMixerGroup.SFX] = m_AudioMixer.FindMatchingGroups("SFX")[0];
    }

    public struct SoundReq
    {
        public SoundDef def;
        public bool usePos;
        public Vector3 pos;
        public Transform transform;
    }

    SoundEmitter AllocEmitter()
    {
        // Look for unused emitter
        foreach (var e in m_Emitters)
        {
            if (!e.playing)
            {
                e.seqId = m_SequenceId++;
                return e;
            }
        }

        // Hunt down one emitter to kill
        SoundEmitter emitter = null;
        float distance = float.MinValue;
        var listenerPos = m_CurrentListener != null ? m_CurrentListener.transform.position : Vector3.zero;
        foreach(var e in m_Emitters)
        {
            var s = e.source;

            if (s == null)
            {
                // Could happen if parent was killed. Not good, but fixable:
                GameDebug.LogWarning("Soundemitter had its audiosource destroyed. Making a new.");
                e.source = MakeAudioSource();
                e.repeatCount = 0;
                s = e.source;
            }

            // Skip destroyed sources and looping sources
            if (s.loop)
                continue;

            // Pick closest; assuming 2d sounds very close!
            var dist = 0.0f;
            if(s.spatialBlend > 0.0f)
            {
                dist = (s.transform.position - listenerPos).magnitude;

                // if tracking another object assume closer
                var t = s.transform;
                if (t.parent != m_SourceHolder.transform)
                    dist *= 0.5f;
            }

            if(dist > distance)
            {
                distance = dist;
                emitter = e;
            }

        }
        if(emitter != null)
        {
            emitter.Kill();
            emitter.seqId = m_SequenceId++;
            return emitter;
        }
        GameDebug.Log("Unable to allocate sound emitter!");
        return null;
    }

    public SoundHandle Play(WeakSoundDef weakSoundDef)
    {
        SoundDef def;
        return m_SoundDefs.TryGetValue(weakSoundDef.guid , out def) ? Play(def) : new SoundHandle(null);
    }
    public SoundHandle Play(SoundDef soundDef)
    {
        GameDebug.Assert(soundDef != null);

        var e = AllocEmitter();

        if (e == null)
            return new SoundHandle(null);

        if (soundDef.spatialBlend > 0.0f)
            GameDebug.LogWarning(string.Format("Playing 3d {0} sound at 0,0,0", soundDef.name));

        e.source.transform.position = new Vector3(0, 0, 0);
        e.repeatCount = Random.Range(soundDef.repeatMin, soundDef.repeatMax);
        e.playing = true;
        e.soundDef = soundDef;
        StartEmitter(e);
        return new SoundHandle(e);
    }

    public SoundHandle Play(SoundDef soundDef, Vector3 position)
    {
        var e = AllocEmitter();

        if (e == null)
            return new SoundHandle(null);

        e.source.transform.position = position;
        e.repeatCount = Random.Range(soundDef.repeatMin, soundDef.repeatMax);
        e.playing = true;
        e.soundDef = soundDef;
        StartEmitter(e);
        return new SoundHandle(e);
    }

    public SoundHandle Play(SoundDef soundDef, Transform parent)
    {
        Profiler.BeginSample("SoundSystem.AllocEmitter");
        var e = AllocEmitter();
        Profiler.EndSample();
        
        if (e == null)
            return new SoundHandle(null);

        e.source.transform.parent = parent;
        e.source.transform.localPosition = Vector3.zero;
        e.repeatCount = Random.Range(soundDef.repeatMin, soundDef.repeatMax);
        e.playing = true;
        e.soundDef = soundDef;
        Profiler.BeginSample("SoundSystem.StartEmitter");
        StartEmitter(e);
        Profiler.EndSample();
        return new SoundHandle(e);
    }

    public void Stop(SoundHandle sh, float fadeOutTime = 0.0f)
    {
        if (!sh.IsValid())
        {
            GameDebug.LogWarning("SoundSystem.Stop(): invalid SoundHandle");
            return;
        }
        if (fadeOutTime == 0.0f)
            sh.emitter.fadeToKill.SetValue(0.0f);
        else
        {
            sh.emitter.fadeToKill.SetValue(1.0f);
            sh.emitter.fadeToKill.MoveTo(0.0f, fadeOutTime);
        }
    }

    bool focus = false;
    public void Update()
    {
        if(focus != Application.isFocused)
        {
            focus = Application.isFocused;
            if(soundMute.IntValue == -1)
                m_MasterVolume.MoveTo(focus ? 1.0f : 0.0f, 0.5f);
        }

        var masterVolume = m_MasterVolume.GetValue();

        if (soundMute.IntValue == 0)
        {
            masterVolume = 0.0f;
            DebugOverlay.Write(Color.red, DebugOverlay.Width - 10, 2, "{0}", "AUDIO MUTED");
        }
        else if (soundMute.IntValue == 1)
        {
            masterVolume = 1.0f;
            DebugOverlay.Write(Color.green, DebugOverlay.Width - 10, 2, "{0}", "AUDIO PLAYING");
        }

        m_AudioMixer.SetFloat("MasterVolume", DecibelFromAmplitude(Mathf.Clamp(soundMasterVol.FloatValue, 0.0f, 1.0f) * masterVolume));
        m_AudioMixer.SetFloat("MusicVolume", DecibelFromAmplitude(Mathf.Clamp(soundMusicVol.FloatValue, 0.0f, 1.0f)));
        m_AudioMixer.SetFloat("SFXVolume", DecibelFromAmplitude(Mathf.Clamp(soundSFXVol.FloatValue, 0.0f, 1.0f)));
        m_AudioMixer.SetFloat("MenuVolume", DecibelFromAmplitude(Mathf.Clamp(soundMenuVol.FloatValue, 0.0f, 1.0f)));

        // Update running sounds
        int count = 0;
        foreach (var e in m_Emitters)
        {
            if (!e.playing)
                continue;
            if (e.source == null)
            {
                // Could happen if parent was killed. Not good, but fixable:
                GameDebug.LogWarning("Soundemitter had its audiosource destroyed. Making a new.");
                e.source = MakeAudioSource();
                e.repeatCount = 0;
            }
            if (e.fadeToKill.IsMoving())
            {
                e.source.volume = AmplitudeFromDecibel(e.soundDef.volume) * e.fadeToKill.GetValue();
            }
            else if (e.fadeToKill.GetValue() == 0.0f)
            {
                // kill no matter what
                e.Kill();
            }
            if (e.source.isPlaying)
            {
                count++;
                continue;
            }
            if (e.repeatCount > 1)
            {
                e.repeatCount--;
                StartEmitter(e);
                continue;
            }
            // Reset for reuse
            e.playing = false;
            e.source.transform.parent = m_SourceHolder.transform;
            e.source.enabled = true;
            e.source.gameObject.SetActive(true);
            e.source.transform.position = Vector3.zero;
            e.fadeToKill.SetValue(1.0f);
        }
        if (soundDebug.IntValue > 0)
        {
            DebugOverlay.Write(30, 1, "Mixer: {0} {1}", m_AudioMixer.GetInstanceID(), Game.game.audioMixer.GetInstanceID());
            int ii = 4;
            foreach(var o in GameObject.FindObjectsOfType<AudioMixerGroup>())
            {
                DebugOverlay.Write(30, ii++, "group: {0} {1}", o.name, o.GetInstanceID());
            }
            DebugOverlay.Write(1, 1, "Num audios {0}", count);
            for (int i = 0, c = m_Emitters.Length; i < c; ++i)
            {
                var e = m_Emitters[i];
                DebugOverlay.Write(1, 3 + i, "Emitter {0:##}  {1} {2} {3}", i, e.playing ? e.soundDef.name : "<n/a>", e.source.gameObject.activeInHierarchy ? "act":"nact", e.playing ? "Mixer: " +e.source.outputAudioMixerGroup.audioMixer.GetInstanceID() : "");
            }
            if(m_CurrentListener == null)
            {
                DebugOverlay.Write(DebugOverlay.Width / 2 - 5, DebugOverlay.Height, "No AudioListener?");
                return;
            }
            for (int i = 0, c = m_Emitters.Length; i < c; ++i)
            {
                var e = m_Emitters[i];
                if (!e.playing)
                    continue;
                var s = e.source.spatialBlend;
                Vector3 locpos = m_CurrentListener.transform.InverseTransformPoint(e.source.transform.position);
                float x = Mathf.Lerp(e.source.panStereo*10.0f, Mathf.Clamp(locpos.x, -10, 10), s); ;
                float z = Mathf.Lerp(-10.0f, Mathf.Clamp(locpos.z, -10, 10), s);
                DebugOverlay.Write(Color.Lerp(Color.green, Color.blue, s), DebugOverlay.Width / 2 + x, DebugOverlay.Height / 2 - z, "{0} ({1:##.#})", e.soundDef.name, locpos.magnitude);
            }
        }
    }

    void StartEmitter(SoundEmitter emitter)
    {
        var soundDef = emitter.soundDef;
        var source = emitter.source;

        StartSource(source, soundDef);
    }

#if UNITY_EDITOR
    public static void StartSource(AudioSource source, SoundDef soundDef)
#else
    static void StartSource(AudioSource source, SoundDef soundDef)
#endif
    {
        Profiler.BeginSample(".Set source clip");
        source.clip = soundDef.clips[Random.Range(0, soundDef.clips.Count)];
        Profiler.EndSample();
        
        Profiler.BeginSample(".Setup source");
        // Map from halftone space to linear playback multiplier
        source.pitch = Mathf.Pow(2.0f, Random.Range(soundDef.pitchMin, soundDef.pitchMax) / 12.0f);
        source.minDistance = soundDef.distMin;
        source.maxDistance = soundDef.distMax;
        source.volume = AmplitudeFromDecibel(soundDef.volume);
        source.loop = soundDef.loopCount < 1 ? true : false;
        source.rolloffMode = soundDef.rolloffMode;
        float delay = Random.Range(soundDef.delayMin, soundDef.delayMax);
        if(s_MixerGroups != null)
            source.outputAudioMixerGroup = s_MixerGroups[(int)soundDef.soundGroup];
        source.spatialBlend = soundDef.spatialBlend;
        source.panStereo = Random.Range(soundDef.panMin, soundDef.panMax);
        Profiler.EndSample();

        // soundSpatialize can be null as this is run from editor too
        Profiler.BeginSample(".Setup spatializer");
        if(soundSpatialize != null && soundSpatialize.IntValue > 0 && soundDef.spatialBlend > 0.5f)
        {
            source.spatialize = true;
            source.SetSpatializerFloat(0, 8.0f);
            source.SetSpatializerFloat(1, 0.0f);
            //source.SetSpatializerFloat(2, soundDef.distMin);
            //source.SetSpatializerFloat(3, soundDef.distMax);
            source.SetSpatializerFloat(4, 0.0f);
            source.SetSpatializerFloat(5, 0.0f);
            source.spatializePostEffects = false;
            //source.rolloffMode = source.spatialize ? AudioRolloffMode.Linear : source.rolloffMode;
        }
        else
        {
            source.spatialize = false;
        }
        Profiler.EndSample();
        
        // TODO (petera) can we remove this? -- should never be needed due to re-enabling code in main update loop
        if (!source.enabled)
        {
            GameDebug.Log("Fixing disabled soundsource");
            source.enabled = true;
        }

        Profiler.BeginSample("AudioSource.Play");
        if (delay > 0.0f)
            source.PlayDelayed(delay);
        else
            source.Play();
        Profiler.EndSample();
    }

    public void MountBank(SoundBank bank)
    {
        GameDebug.Assert(bank.soundDefs.Count == bank.soundDefGuids.Count);
        for (int i = 0, c = bank.soundDefGuids.Count; i < c; ++i)
        {
            m_SoundDefs[bank.soundDefGuids[i]] = bank.soundDefs[i];
        }
        GameDebug.Log("Mounted soundbank: " + bank.name + " with " + bank.soundDefGuids.Count + " sounds");
    }

    public void UnmountBank(SoundBank bank)
    {
        GameDebug.Assert(bank.soundDefs.Count == bank.soundDefGuids.Count);
        for (int i = 0, c = bank.soundDefGuids.Count; i < c; ++i)
        {
            m_SoundDefs.Remove(bank.soundDefGuids[i]);
        }
        GameDebug.Log("Unmounted soundbank: " + bank.name + " with " + bank.soundDefGuids.Count + " sounds");
    }

    public void SetCurrentListener(AudioListener audioListener)
    {
        m_CurrentListener = audioListener;
    }

    public static float SOUND_VOL_CUTOFF = -60.0f;
    public static float SOUND_AMP_CUTOFF = Mathf.Pow(2.0f, SOUND_VOL_CUTOFF / 6.0f);

    public static float DecibelFromAmplitude(float amplitude)
    {
        if (amplitude < SOUND_AMP_CUTOFF)
            return -60.0f;
        return 6.0f * Mathf.Log(amplitude) / Mathf.Log(2.0f);
    }

    public static float AmplitudeFromDecibel(float decibel)
    {
        if (decibel <= SOUND_VOL_CUTOFF)
        {
            return 0;
        }
        return Mathf.Pow(2.0f, decibel / 6.0f);
    }

    Dictionary<string, SoundDef> m_SoundDefs = new Dictionary<string, SoundDef>();
    AudioMixer m_AudioMixer;
    int m_SequenceId;
    SoundEmitter[] m_Emitters;
    GameObject m_SourceHolder;
    AudioListener m_CurrentListener;
    Interpolator m_MasterVolume = new Interpolator(1.0f, Interpolator.CurveType.SmoothStep);

}
