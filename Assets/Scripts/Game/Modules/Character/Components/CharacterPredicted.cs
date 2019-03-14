using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public enum CameraProfile
{
    FirstPerson,
    Shoulder,
    ThirdPerson,
}

[Serializable]
public struct CharacterPredictedData : IComponentData, IPredictedComponent<CharacterPredictedData>
{
    public enum LocoState
    {
        Stand,
        GroundMove,
        Jump,
        DoubleJump,
        InAir,
        MaxValue
    }

    public enum Action
    {
        None,
        PrimaryFire,
        SecondaryFire,
        Reloading,
        Melee,
        NumActions,
    }


    public int tick;                    // Tick is only for debug purposes
    public Vector3 position;
    public Vector3 velocity;
    public LocoState locoState;
    public int locoStartTick;
    public Action action;
    public int actionStartTick;
    public int jumpCount;
    public int sprinting;

    public CameraProfile cameraProfile;
    
    public int damageTick;
    public Vector3 damageDirection;
    public float damageImpulse;                                              

    public void SetAction(Action action, int tick)
    {
//        GameDebug.Log("SetAction:" + action + " tick:" + tick);
        this.action = action;
        this.actionStartTick = tick;
    }

    public static IPredictedComponentSerializerFactory CreateSerializerFactory()
    {
        return new PredictedComponentSerializerFactory<CharacterPredictedData>();
    }
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        writer.WriteInt32("tick", tick);
        writer.WriteVector3Q("velocity", velocity, 2);                   
        writer.WriteInt32("action", (int)action); 
        writer.WriteInt32("actionStartTick", actionStartTick);                            
        writer.WriteInt32("phase", (int)locoState);
        writer.WriteInt32("phaseStartTick", locoStartTick);                               
        writer.WriteVector3Q("position", position, 2);
        writer.WriteInt32("jumpCount", jumpCount);
        writer.WriteBoolean("sprint", sprinting == 1);
        writer.WriteByte("cameraProfile", (byte)cameraProfile);
        writer.WriteInt32("damageTick", damageTick);
        writer.WriteVector3Q("damageDirection", damageDirection);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        this.tick = reader.ReadInt32();
        velocity = reader.ReadVector3Q();
        action = (Action)reader.ReadInt32();
        actionStartTick = reader.ReadInt32();
        locoState = (LocoState)reader.ReadInt32();
        locoStartTick = reader.ReadInt32();
        position = reader.ReadVector3Q();
        jumpCount = reader.ReadInt32();
        sprinting = reader.ReadBoolean() ? 1 : 0;
        cameraProfile = (CameraProfile)reader.ReadByte();
        damageTick = reader.ReadInt32();
        damageDirection = reader.ReadVector3Q();
    }

    public bool IsOnGround()
    {
        return locoState == CharacterPredictedData.LocoState.Stand || locoState == CharacterPredictedData.LocoState.GroundMove;
    }

#if UNITY_EDITOR
    public bool VerifyPrediction(ref CharacterPredictedData state)
    {
        return Vector3.Distance(position, state.position) < 0.1f 
               && Vector3.Distance(velocity, state.velocity) < 0.1f
               && locoState == state.locoState
               && locoStartTick == state.locoStartTick
               && action == state.action
               && actionStartTick == state.actionStartTick
               && jumpCount == state.jumpCount
               && sprinting == state.sprinting
               && damageTick == state.damageTick;
    }
    
    public override string ToString()
    {
        var strBuilder = new System.Text.StringBuilder();
        strBuilder.AppendLine("tick:" + tick);
        strBuilder.AppendLine("velocity:" + velocity);
        strBuilder.AppendLine("action:" + action);
        strBuilder.AppendLine("actionStartTick:" + actionStartTick);
        strBuilder.AppendLine("loco:" + locoState);
        strBuilder.AppendLine("phaseStartTick:" + locoStartTick);
        strBuilder.AppendLine("position:" + position);
        strBuilder.AppendLine("jumpCount:" + jumpCount);
        strBuilder.AppendLine("sprinting:" + sprinting);
        strBuilder.AppendLine("damageTick:" + damageTick);
        strBuilder.AppendLine("damageDirection:" + damageDirection);
        return strBuilder.ToString();
    }        
#endif    
}

public class CharacterPredicted : ComponentDataProxy<CharacterPredictedData>
{}