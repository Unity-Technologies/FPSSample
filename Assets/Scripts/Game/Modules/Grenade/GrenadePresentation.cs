using System;
using System.Collections.Generic;
using UnityEngine;

public class GrenadePresentation : MonoBehaviour, INetSerialized
{
    public struct State
    {
        public Vector3 position;
        public bool exploded;
        public int bouncetick;

        public void Serialize(ref NetworkWriter writer)
        {
            writer.WriteVector3("position", position);
            writer.WriteBoolean("exploded", exploded);
            writer.WriteInt32("bouncetick", bouncetick);
        }

        public void Deserialize(ref NetworkReader reader)
        {
            position = reader.ReadVector3();
            exploded = reader.ReadBoolean();
            bouncetick = reader.ReadInt32();
        }

        public void Interpolate(ref State prevState, ref State nextState, float f)
        {
            position = Vector3.Lerp(prevState.position, nextState.position, f);
            exploded = nextState.exploded;
            bouncetick = nextState.bouncetick;
        }
    }
    
    public State state;
    public TickStateSparseBuffer<State> stateHistory = new TickStateSparseBuffer<State>(32);

    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        state.Serialize(ref writer);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        var state = new State();
        state.Deserialize(ref reader);
        
        stateHistory.Add(tick, state);
    }
}
