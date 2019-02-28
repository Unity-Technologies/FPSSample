using UnityEngine;
using UnityEngine.Audio;

public interface ISoundSystem
{
    void Init(AudioMixer mixer);
    void MountBank(SoundBank bank);
    void SetCurrentListener(AudioListener audioListener);
    SoundSystem.SoundHandle Play(SoundDef soundDef);
    SoundSystem.SoundHandle Play(SoundDef soundDef, Transform parent);
    SoundSystem.SoundHandle Play(SoundDef soundDef, Vector3 position);
    SoundSystem.SoundHandle Play(WeakSoundDef weakSoundDef);
    void Stop(SoundSystem.SoundHandle sh, float fadeOutTime = 0);
    void UnmountBank(SoundBank bank);
    void Update();
}