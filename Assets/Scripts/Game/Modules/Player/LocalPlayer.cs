using System;
using UnityEngine;
using Unity.Entities;



// Component specifies that entity is on server or is predicted
public struct ServerEntity : IComponentData    
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
}

