using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

// TODO (petera) Rename this to GameModeState or something even better

// This is data is replicated to the clients about the 'global' state of
// the game mode, scores etc.

public class GameMode : MonoBehaviour
{
    public int gameTimerSeconds;
    public string gameTimerMessage;
    public string teamName0;
    public string teamName1;
    public int teamScore0;
    public int teamScore1;

    private void OnEnable()
    {
        // TODO (mogensh) As we dont have good way of having strings on ECS data components we keep this as monobehavior and only use GameModeData for serialization 
        var goe = GetComponent<GameObjectEntity>();
        goe.EntityManager.AddComponent(goe.Entity,typeof(GameModeData));
    }
}



[Serializable]
public struct GameModeData : IComponentData, IReplicatedComponent
{
    public int foo;
    
    public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
    {
        return new ReplicatedComponentSerializerFactory<GameModeData>();
    }    
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        var behaviour = context.entityManager.GetComponentObject<GameMode>(context.entity);
        
        writer.WriteInt32("gameTimerSeconds", behaviour.gameTimerSeconds);
        writer.WriteString("gameTimerMessage", behaviour.gameTimerMessage);

        writer.WriteString("teamName0", behaviour.teamName0);
        writer.WriteString("teamName1", behaviour.teamName1);
        writer.WriteInt32("teamScore0", behaviour.teamScore0);
        writer.WriteInt32("teamScore1", behaviour.teamScore1);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        var behaviour = context.entityManager.GetComponentObject<GameMode>(context.entity);
        
        behaviour.gameTimerSeconds = reader.ReadInt32();
        behaviour.gameTimerMessage = reader.ReadString();

        behaviour.teamName0 = reader.ReadString();
        behaviour.teamName1 = reader.ReadString();
        behaviour.teamScore0 = reader.ReadInt32();
        behaviour.teamScore1 = reader.ReadInt32();
    }
}
