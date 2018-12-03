using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public struct UserCommand : INetSerialized
{
    public int checkTick;        // For debug purposes
    public int renderTick;      
    public float moveYaw;
    public float moveMagnitude;
    public float lookYaw;
    public float lookPitch;
    public bool jump;
    public bool boost;
    public bool sprint;     
    public bool primaryFire;
    public bool secondaryFire;
    public bool abilityA;
    public bool reload;
    public bool melee;
    public bool use;
    public CharacterEmote emote;

    public static readonly UserCommand defaultCommand = new UserCommand(0); 

    private UserCommand(int i)    
    {
        checkTick = 0;
        renderTick = 0;
        moveYaw = 0;
        moveMagnitude = 0;
        lookYaw = 0;
        lookPitch = 90;
        jump = false;
        boost = false;
        sprint = false;
        primaryFire = false;
        secondaryFire = false;
        abilityA = false;
        reload = false;
        melee = false;
        use = false;
        emote = CharacterEmote.None;
    }
    
    public void ClearCommand()  
    {
        jump = false;
        boost = false;
        sprint = false;
        primaryFire = false;
        secondaryFire = false;
        abilityA = false;
        reload = false;
        melee = false;
        use = false;
        emote = CharacterEmote.None;
    }
    

    public Vector3 lookDir
    {
        get { return Quaternion.Euler(new Vector3(-lookPitch, lookYaw, 0)) * Vector3.down; }
    }
    public Quaternion lookRotation
    {
        get { return Quaternion.Euler(new Vector3(90 - lookPitch, lookYaw, 0)); }
    }

    public void Serialize(ref NetworkWriter networkWriter, IEntityReferenceSerializer refSerializer)
    {
        networkWriter.WriteInt32("tick", checkTick);
        networkWriter.WriteInt32("renderTick", renderTick);
        networkWriter.WriteFloatQ("moveYaw", moveYaw, 0);
        networkWriter.WriteFloatQ("moveMagnitude", moveMagnitude, 2);
        networkWriter.WriteFloat("lookYaw", lookYaw); 
        networkWriter.WriteFloat("lookPitch", lookPitch);
        networkWriter.WriteBoolean("jump", jump);
        networkWriter.WriteBoolean("boost", boost);
        networkWriter.WriteBoolean("sprint", sprint);
        networkWriter.WriteBoolean("primaryFire", primaryFire);
        networkWriter.WriteBoolean("secondaryFire", secondaryFire);
        networkWriter.WriteBoolean("abilityA", abilityA);
        networkWriter.WriteBoolean("reload", reload);
        networkWriter.WriteBoolean("melee", melee);
        networkWriter.WriteBoolean("use", use);
        networkWriter.WriteByte("emote", (byte)emote);
    }

    public void Deserialize(ref NetworkReader networkReader, IEntityReferenceSerializer refSerializer, int tick)
    {
        checkTick = networkReader.ReadInt32();
        renderTick = networkReader.ReadInt32();
        moveYaw = networkReader.ReadFloatQ();
        moveMagnitude = networkReader.ReadFloatQ();
        lookYaw = networkReader.ReadFloat();
        lookPitch = networkReader.ReadFloat();
        jump = networkReader.ReadBoolean();
        boost = networkReader.ReadBoolean();
        sprint = networkReader.ReadBoolean();
        primaryFire = networkReader.ReadBoolean();
        secondaryFire = networkReader.ReadBoolean();
        abilityA = networkReader.ReadBoolean();
        reload = networkReader.ReadBoolean();
        melee = networkReader.ReadBoolean();
        use = networkReader.ReadBoolean();
        emote = (CharacterEmote)networkReader.ReadByte();
    }

    public override string ToString()
    {
        System.Text.StringBuilder strBuilder = new System.Text.StringBuilder();
        strBuilder.AppendLine("tick:" + checkTick);
        strBuilder.AppendLine("moveYaw:" + moveYaw);
        strBuilder.AppendLine("moveMagnitude:" + moveMagnitude);
        strBuilder.AppendLine("lookYaw:" + lookYaw);
        strBuilder.AppendLine("lookPitch:" + lookPitch);
        strBuilder.AppendLine("jump:" + jump);
        strBuilder.AppendLine("boost:" + boost);
        strBuilder.AppendLine("sprint:" + sprint);
        strBuilder.AppendLine("primaryFire:" + primaryFire);
        strBuilder.AppendLine("secondaryFire:" + secondaryFire);
        strBuilder.AppendLine("abilityA:" + abilityA);
        strBuilder.AppendLine("reload:" + reload);
        strBuilder.AppendLine("melee:" + melee);
        strBuilder.AppendLine("use:" + use);
        strBuilder.AppendLine("emote:" + emote);
        return strBuilder.ToString();
    }
}
