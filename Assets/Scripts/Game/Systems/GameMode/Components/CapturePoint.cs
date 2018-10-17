using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapturePoint : MonoBehaviour, INetworkSerializable
{

    public string objectiveName;
    public float radius;
    public float height;
    public byte captureIndex;
    public float captureTime = 10.0f;
    public SpawnPoint[] spawns;
    public Animator animator;

    public enum Status
    {
        Locked,
        Active,
        Capturing,
        Healing,
        Contested,
        Completed
    }

    [System.NonSerialized]
    public Status status;
    [System.NonSerialized]
    public float captured;

    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteString("objectiveName", objectiveName);

        writer.WriteByte("status", (byte)status);
        writer.WriteFloatQ("captured", captured, 2);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        objectiveName = reader.ReadString();

        status = (Status)reader.ReadByte();
        captured = reader.ReadFloatQ();

        // TODO (petera) replace with proper cross scene reference system
        foreach(var i in CapturePointReference.capturePointReferences)
        {
            if(i.index == captureIndex && i.animator != null)
            {
                int captured = 0;
                if (status == Status.Capturing || status == Status.Contested)
                    captured = 1;
                else if (status == Status.Completed)
                    captured = 2;
                i.animator.SetInteger("Captured", captured);
            }
        }
    }

#if UNITY_EDITOR
    [ConfigVar(Name = "debug.capture", DefaultValue = "0", Description = "Debugging capture zones")]
    static ConfigVar debugCapture;
    void Update()
    {
        foreach(var i in CapturePointReference.capturePointReferences)
        {
            if(i.index == captureIndex && i.animator != null)
            {
                i.animator.SetInteger("Captured", debugCapture.IntValue);
            }
        }
    }

    void OnDrawGizmos()
    {
        var position = transform.position;
        var halfHeight = height / 2;
        position.y += halfHeight;
        DebugDraw.Cylinder(position, Vector3.up, radius, halfHeight, Color.red);
    }
#endif
}
