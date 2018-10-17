using UnityEngine;
using Unity.Entities;

public class PlayerState : MonoBehaviour, INetworkSerializable
{
    public int playerId;
    public string playerName;
    public int teamIndex;   
    public int score;
    public Entity controlledEntity;
    public bool gameModeSystemInitialized;

    // These are only sync'hed to owning client
    public bool displayCountDown;
    public int countDown;
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

    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteInt32("playerId", playerId);
        writer.WriteString("playerName", playerName);
        writer.WriteInt32("teamIndex", teamIndex);
        writer.WriteInt32("score", score);
        refSerializer.SerializeReference(ref writer, "controlledEntity", controlledEntity);

        writer.SetFieldSection(NetworkWriter.FieldSectionType.OnlyPredicting);
        writer.WriteBoolean("displayCountDown", displayCountDown);
        writer.WriteInt32("countdown", countDown);
        writer.WriteBoolean("displayScoreBoard", displayScoreBoard);
        writer.WriteBoolean("displayGameScore", displayGameScore);
        writer.WriteBoolean("displayGameResult", displayGameResult);
        writer.WriteString("gameResult", gameResult);

        writer.WriteBoolean("displayGoal", displayGoal);
        writer.WriteVector3Q("goalPosition", goalPosition, 2);
        writer.WriteUInt32("goalDefendersColor", goalDefendersColor);
        writer.WriteUInt32("goalAttackersColor", goalAttackersColor);
        writer.WriteUInt32("goalAtackers", goalAttackers);
        writer.WriteUInt32("goalDefenders", goalDefenders);
        writer.WriteString("goalString", goalString);
        writer.WriteString("actionString", actionString);
        writer.WriteFloatQ("goalCompletion", goalCompletion, 2);
        writer.ClearFieldSection();
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        playerId = reader.ReadInt32();
        playerName = reader.ReadString();
        teamIndex = reader.ReadInt32();
        score = reader.ReadInt32();
        refSerializer.DeserializeReference(ref reader, ref controlledEntity);

        displayCountDown = reader.ReadBoolean();
        countDown = reader.ReadInt32();
        displayScoreBoard = reader.ReadBoolean();
        displayGameScore = reader.ReadBoolean();
        displayGameResult = reader.ReadBoolean();
        gameResult = reader.ReadString();

        displayGoal = reader.ReadBoolean();
        goalPosition = reader.ReadVector3Q();
        goalDefendersColor = reader.ReadUInt32();
        goalAttackersColor = reader.ReadUInt32();
        goalAttackers = reader.ReadUInt32();
        goalDefenders = reader.ReadUInt32();
        goalString = reader.ReadString();
        actionString = reader.ReadString();
        goalCompletion = reader.ReadFloatQ();
    }
}
