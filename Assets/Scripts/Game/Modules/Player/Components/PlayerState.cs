using System;
using UnityEngine;
using Unity.Entities;

public class PlayerState : MonoBehaviour
{
    public int playerId;
    public string playerName;
    public int teamIndex;   
    public int score;
    public Entity controlledEntity;
    public bool gameModeSystemInitialized;

    // These are only sync'hed to owning client
    public bool displayScoreBoard;
    public bool displayGameScore;
    public bool displayGameResult;
    public string gameResult;

    public bool displayGoal;
    public Vector3 goalPosition;
    public uint goalDefendersColor;
    public uint goalAttackersColor;
    public uint goalAttackers;
    public uint goalDefenders;
    public string goalString;
    public string actionString;
    public float goalCompletion;

    // Non synchronized
    public bool enableCharacterSwitch;
    
    private void OnEnable()
    {
        // TODO (mogensh) As we dont have good way of having strings on ECS data components we keep this as monobehavior and only use GameModeData for serialization 
        var goe = GetComponent<GameObjectEntity>();
        goe.EntityManager.AddComponent(goe.Entity,typeof(PlayerStateData));
    }
}



[Serializable]
public struct PlayerStateData : IComponentData, IReplicatedComponent
{
    public int foo;
    
    public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
    {
        return new ReplicatedComponentSerializerFactory<PlayerStateData>();
    }    
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        var behaviour = context.entityManager.GetComponentObject<PlayerState>(context.entity);
        
        writer.WriteInt32("playerId", behaviour.playerId);
        writer.WriteString("playerName", behaviour.playerName);
        writer.WriteInt32("teamIndex", behaviour.teamIndex);
        writer.WriteInt32("score", behaviour.score);
        context.refSerializer.SerializeReference(ref writer, "controlledEntity", behaviour.controlledEntity);

        writer.SetFieldSection(NetworkWriter.FieldSectionType.OnlyPredicting);
        writer.WriteBoolean("displayScoreBoard", behaviour.displayScoreBoard);
        writer.WriteBoolean("displayGameScore", behaviour.displayGameScore);
        writer.WriteBoolean("displayGameResult", behaviour.displayGameResult);
        writer.WriteString("gameResult", behaviour.gameResult);

        writer.WriteBoolean("displayGoal", behaviour.displayGoal);
        writer.WriteVector3Q("goalPosition", behaviour.goalPosition, 2);
        writer.WriteUInt32("goalDefendersColor", behaviour.goalDefendersColor);
        writer.WriteUInt32("goalAttackersColor", behaviour.goalAttackersColor);
        writer.WriteUInt32("goalAtackers", behaviour.goalAttackers);
        writer.WriteUInt32("goalDefenders", behaviour.goalDefenders);
        writer.WriteString("goalString", behaviour.goalString);
        writer.WriteString("actionString", behaviour.actionString);
        writer.WriteFloatQ("goalCompletion", behaviour.goalCompletion, 2);
        writer.ClearFieldSection();
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        var behaviour = context.entityManager.GetComponentObject<PlayerState>(context.entity);
        
        behaviour.playerId = reader.ReadInt32();
        behaviour.playerName = reader.ReadString();
        behaviour.teamIndex = reader.ReadInt32();
        behaviour.score = reader.ReadInt32();
        context.refSerializer.DeserializeReference(ref reader, ref behaviour.controlledEntity);

        behaviour.displayScoreBoard = reader.ReadBoolean();
        behaviour.displayGameScore = reader.ReadBoolean();
        behaviour.displayGameResult = reader.ReadBoolean();
        behaviour.gameResult = reader.ReadString();

        behaviour.displayGoal = reader.ReadBoolean();
        behaviour.goalPosition = reader.ReadVector3Q();
        behaviour.goalDefendersColor = reader.ReadUInt32();
        behaviour.goalAttackersColor = reader.ReadUInt32();
        behaviour.goalAttackers = reader.ReadUInt32();
        behaviour.goalDefenders = reader.ReadUInt32();
        behaviour.goalString = reader.ReadString();
        behaviour.actionString = reader.ReadString();
        behaviour.goalCompletion = reader.ReadFloatQ();
    }
}
