using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Profiling;

public class SoundTimelineMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        base.ProcessFrame(playable, info, playerData);

        if (info.deltaTime == 0)
            return;

        Transform trackBinding = playerData as Transform;

        int inputCount = playable.GetInputCount();

        for(int i=0;i<inputCount;i++)
        {
            ScriptPlayable<SoundTimelineBehaviour> inputBehaviour = (ScriptPlayable<SoundTimelineBehaviour>)playable.GetInput(i);
            SoundTimelineBehaviour input = inputBehaviour.GetBehaviour();

            if(input.triggered)
            {
                input.triggered = false;

                if (Game.SoundSystem == null)
                {
                    GameDebug.LogWarning("SoundTimeline: You should not try to play sound with no soundsystem");
                    return;
                }

                Profiler.BeginSample("Play sound");        
                switch (input.position)
                {
                    case SoundTimelineBehaviour.SoundPosition.None:
                        Game.SoundSystem.Play(input.sound);
                        break;
                    case SoundTimelineBehaviour.SoundPosition.Position:
                        if(trackBinding == null)
                        {
                            GameDebug.LogError("Cant play timeline sound as no transform is defined for track. Sound:" + input.sound.name);
                            break;
                        }
                        Game.SoundSystem.Play(input.sound, trackBinding.position);
                        break;
                    case SoundTimelineBehaviour.SoundPosition.Follow:
                        if (trackBinding == null)
                        {
                            GameDebug.LogError("Cant play timeline sound as no transform is defined for track. Sound:"  + input.sound.name);
                            break;
                        }
                        Game.SoundSystem.Play(input.sound, trackBinding);
                        break;
                }
                Profiler.EndSample();    
            }
        }
    }
}
