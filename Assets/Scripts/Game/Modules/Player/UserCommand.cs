using Unity.Entities;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public struct UserCommand : IReplicatedComponent
{
    public enum Button : uint
    {
        None = 0,
        Jump = 1 << 0,
        Boost = 1 << 1,
        PrimaryFire = 1 << 2,
        SecondaryFire = 1 << 3,
        Reload = 1 << 4,
        Melee = 1 << 5,
        Use = 1 << 6,
        Ability1 = 1 << 7,
        Ability2 = 1 << 8,
        Ability3 = 1 << 9,
    }

    public struct ButtonBitField
    {
        public uint flags;

        public bool IsSet(Button button)
        {
            return (flags & (uint)button) > 0;
        }

        public void Or(Button button, bool val)
        {
            if(val)
                flags = flags | (uint) button;
        }

        
        public void Set(Button button, bool val)
        {
            if(val)
                flags = flags | (uint) button;
            else
            {
                flags = flags & ~(uint) button;
            }
        }
    }
    
    public int checkTick;        // For debug purposes
    public int renderTick;      
    public float moveYaw;
    public float moveMagnitude;
    public float lookYaw;
    public float lookPitch;
    public ButtonBitField buttons;
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
        buttons.flags = 0;
        emote = CharacterEmote.None;
    }
    
    public void ClearCommand()  
    {
        buttons.flags = 0;
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

    public void Serialize(ref SerializeContext context, ref NetworkWriter networkWriter)
    {
        networkWriter.WriteInt32("tick", checkTick);
        networkWriter.WriteInt32("renderTick", renderTick);
        networkWriter.WriteFloatQ("moveYaw", moveYaw, 0);
        networkWriter.WriteFloatQ("moveMagnitude", moveMagnitude, 2);
        networkWriter.WriteFloat("lookYaw", lookYaw); 
        networkWriter.WriteFloat("lookPitch", lookPitch);
        networkWriter.WriteUInt32("buttons", buttons.flags);
        networkWriter.WriteByte("emote", (byte)emote);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader networkReader)
    {
        checkTick = networkReader.ReadInt32();
        renderTick = networkReader.ReadInt32();
        moveYaw = networkReader.ReadFloatQ();
        moveMagnitude = networkReader.ReadFloatQ();
        lookYaw = networkReader.ReadFloat();
        lookPitch = networkReader.ReadFloat();
        buttons.flags = networkReader.ReadUInt32();
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
        strBuilder.AppendLine("buttons:" + buttons);
        strBuilder.AppendLine("emote:" + emote);
        return strBuilder.ToString();
    }
}
