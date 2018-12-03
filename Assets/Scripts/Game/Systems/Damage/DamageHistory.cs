using System;
using UnityEngine;

public class DamageHistory : MonoBehaviour, INetSerialized             
{
    [System.Serializable]
    public struct InflictedDamage
    {
        public int tick;
        public bool lethal;

        public void Serialize(ref NetworkWriter writer)
        {
            writer.WriteInt32("tick", tick);
            writer.WriteBoolean("lethal", lethal);
        }

        public void Deserialize(ref NetworkReader reader, int tick)
        {
            this.tick = reader.ReadInt32();
            lethal = reader.ReadBoolean();
        }
    }

    [NonSerialized] public InflictedDamage inflictedDamage;

    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        inflictedDamage.Serialize(ref writer);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        inflictedDamage.Deserialize(ref reader, tick);
    }
}
