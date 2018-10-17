using System;
using UnityEngine;

public class RagdollState : MonoBehaviour, INetworkSerializable
{
    [NonSerialized] public bool ragdollActive;
    [NonSerialized] public Vector3 impulse;
    
    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteBoolean("ragdollEnabled",ragdollActive);
        writer.WriteVector3Q("impulse",impulse,1);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        ragdollActive = reader.ReadBoolean();
        impulse = reader.ReadVector3Q();
    }
}
