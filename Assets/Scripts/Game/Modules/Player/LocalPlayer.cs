using System;
using UnityEngine;
using Unity.Entities;



// Component specifies that entity is on server or is predicted
public struct ServerEntity : IComponentData    // TODO (mogensh) move to ReplicatedModule rename to something relevant (as it is now tied to replicated entity predictiongPlayer) 
{
    public int foo;
}

public class LocalPlayer : MonoBehaviour
{
    public int playerId = -1;

    public PlayerState playerState;    
    public NetworkClient networkClient;

    public UserCommand command = UserCommand.defaultCommand;     
    public TickStateDenseBuffer<UserCommand> commandBuffer = new TickStateDenseBuffer<UserCommand>(NetworkConfig.commandClientBufferSize); 
    public Entity controlledEntity;   

    [System.NonSerialized] public float m_debugMoveDuration;
    [System.NonSerialized] public float m_debugMovePhaseDuration;
    [System.NonSerialized] public float m_debugMoveTurnSpeed;
    [System.NonSerialized] public float m_debugMoveMag;
}

