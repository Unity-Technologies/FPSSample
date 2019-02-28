using System;
using Unity.Entities;
using UnityEngine;

public class CapturePoint : MonoBehaviour
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

    private void OnEnable()
    {
        // TODO (mogensh) As we dont have good way of having strings on ECS data components we keep this as monobehavior and only use GameModeData for serialization 
        var goe = GetComponent<GameObjectEntity>();
        goe.EntityManager.AddComponent(goe.Entity,typeof(CapturePointData));
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



[Serializable]
public struct CapturePointData : IComponentData, IReplicatedComponent
{
    public int foo;
    
    public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
    {
        return new ReplicatedComponentSerializerFactory<CapturePointData>();
    }    
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        var behaviour = context.entityManager.GetComponentObject<CapturePoint>(context.entity);

        writer.WriteString("objectiveName", behaviour.objectiveName);

        writer.WriteByte("status", (byte)behaviour.status);
        writer.WriteFloatQ("captured", behaviour.captured, 2);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        var behaviour = context.entityManager.GetComponentObject<CapturePoint>(context.entity);
        
        behaviour.objectiveName = reader.ReadString();

        behaviour.status = (CapturePoint.Status)reader.ReadByte();
        behaviour.captured = reader.ReadFloatQ();

        // TODO (petera) replace with proper cross scene reference system
        foreach(var i in CapturePointReference.capturePointReferences)
        {
            if(i.index == behaviour.captureIndex && i.animator != null)
            {
                int captured = 0;
                if (behaviour.status == CapturePoint.Status.Capturing || behaviour.status == CapturePoint.Status.Contested)
                    captured = 1;
                else if (behaviour.status == CapturePoint.Status.Completed)
                    captured = 2;
                i.animator.SetInteger("Captured", captured);
            }
        }
    }
}