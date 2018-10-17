using UnityEngine;
using UnityEngine.Playables;


public interface IGraphState
{
    void UpdatePresentationState(bool firstUpdate, GameTime time, float deltaTime);  
}

public interface IGraphLogic   
{
    void UpdateGraphLogic(GameTime time, float deltaTime);
}

public interface IAnimGraphInstance    
{
    void Shutdown();

    void SetPlayableInput(int portId, Playable playable, int playablePort);
    void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort);

    void ApplyPresentationState(GameTime time, float deltaTime);   
}

