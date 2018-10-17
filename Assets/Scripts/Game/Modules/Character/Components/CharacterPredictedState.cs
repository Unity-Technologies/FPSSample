using UnityEngine;
using System;
using Unity.Entities;

[RequireComponent(typeof(HealthState))]
public class CharacterPredictedState : PredictedStructBehavior<CharacterPredictedState.StateData>
{
    public struct StateData : IPredictedData<StateData>, INetworkSerializable
    {
        public enum LocoState
        {
            Stand,
            GroundMove,
            Jump,
            DoubleJump,
            InAir,
            Dead,            
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
        public bool abilityActive;
        public bool sprinting;

        public int damageTick;
        public Vector3 damageDirection;
        public float damageImpulse;                                              

        public void SetAction(Action action, int tick)
        {
            this.action = action;
            this.actionStartTick = tick;
        }

        public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
        {
            writer.WriteInt32("tick", tick);
            writer.WriteVector3Q("velocity", velocity, 3);                   
            writer.WriteInt32("action", (int)action); 
            writer.WriteInt32("actionStartTick", actionStartTick);                            
            writer.WriteInt32("phase", (int)locoState);
            writer.WriteInt32("phaseStartTick", locoStartTick);                               
            writer.WriteVector3Q("position", position, 3);
            writer.WriteInt32("jumpCount", jumpCount);
            writer.WriteBoolean("abilityActive", abilityActive);
            writer.WriteBoolean("sprint", sprinting);
            writer.WriteInt32("damageTick", damageTick);
            writer.WriteVector3Q("damageDirection", damageDirection);
        }

        public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
        {
            this.tick = reader.ReadInt32();
            velocity = reader.ReadVector3Q();
            action = (Action)reader.ReadInt32();
            actionStartTick = reader.ReadInt32();
            locoState = (LocoState)reader.ReadInt32();
            locoStartTick = reader.ReadInt32();
            position = reader.ReadVector3Q();
            jumpCount = reader.ReadInt32();
            abilityActive = reader.ReadBoolean();
            sprinting = reader.ReadBoolean();
            damageTick = reader.ReadInt32();
            damageDirection = reader.ReadVector3Q();
        }

        public bool IsOnGround()
        {
            return locoState == StateData.LocoState.Stand || locoState == StateData.LocoState.GroundMove;
        }

#if UNITY_EDITOR
        public bool VerifyPrediction(ref StateData state)
        {
            return Vector3.Distance(position, state.position) < 0.1f 
                   && Vector3.Distance(velocity, state.velocity) < 0.1f
                   && locoState == state.locoState
                   && locoStartTick == state.locoStartTick
                   && action == state.action
                   && actionStartTick == state.actionStartTick
                   && jumpCount == state.jumpCount
                   && abilityActive == state.abilityActive
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
            strBuilder.AppendLine("abilityActive:" + abilityActive);
            strBuilder.AppendLine("sprinting:" + sprinting);
            strBuilder.AppendLine("damageTick:" + damageTick);
            strBuilder.AppendLine("damageDirection:" + damageDirection);
            return strBuilder.ToString();
        }        
#endif    
    }

    [NonSerialized] public Vector3 m_TeleportToPosition;    
    [NonSerialized] public Quaternion m_TeleportToRotation;
    [NonSerialized] public bool m_TeleportPending;
  
    [NonSerialized] public int teamId = -1;       

    [NonSerialized] public float altitude; 
    [NonSerialized] public Collider groundCollider; 
    [NonSerialized] public Vector3 groundNormal;

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        m_TeleportPending = true;
        m_TeleportToPosition = position;
        m_TeleportToRotation = rotation;
    }
    
#if UNITY_EDITOR
    public void ShowHistory(GameWorld world)
    {
        StateHistory.Enabled = true;
        var state = new CharacterPredictedState.StateData();
        var count = DebugOverlay.Height - 4;

        for (var iEntry = 0; iEntry < count; iEntry++)
        {
            var tick = world.worldTime.tick - count + 1 + iEntry;

            StateHistory.GetPredictedState(this, tick, ref state);

            var y = 2 + iEntry;

            {
                var color = (Color32)Color.HSVToRGB(0.21f*(int)state.locoState, 1, 1);
                var colorRGB = ((color.r >> 4) << 8) | ((color.g >> 4) << 4) | (color.b >> 4); 
                DebugOverlay.Write(2,y, "^{0}{1}",  colorRGB.ToString("X3"), state.locoState.ToString());
            }
            DebugOverlay.Write(14,y, state.sprinting ? "Sprint" : "no-sprint");
                
            {
                var color = (Color32)Color.HSVToRGB(0.21f*(int)state.action, 1, 1);
                var colorRGB = ((color.r >> 4) << 8) | ((color.g >> 4) << 4) | (color.b >> 4); 
                DebugOverlay.Write(26,y, "^{0}{1}",  colorRGB.ToString("X3"), state.action.ToString());
            }
        }
    }
#endif
}

[DisableAutoCreation]
public class CharacterRollback : PredictedStructBehaviorRollback<CharacterPredictedState,CharacterPredictedState.StateData>
{
    public CharacterRollback(GameWorld gameWorld) : base(gameWorld)
    {}
}
