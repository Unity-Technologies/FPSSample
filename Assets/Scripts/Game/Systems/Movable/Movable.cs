using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;



public struct MovableData : IComponentData, IInterpolatedComponent<MovableData>
{
    Vector3 position;
    Quaternion rotation;

    public static IInterpolatedComponentSerializerFactory CreateSerializerFactory()
    {
        return new InterpolatedComponentSerializerFactory<MovableData>();
    }
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        var transform = context.entityManager.GetComponentObject<Transform>(context.entity);
        
        writer.WriteVector3Q("position", transform.position);
        writer.WriteQuaternionQ("rotation", transform.rotation);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        position = reader.ReadVector3Q();
        rotation = reader.ReadQuaternionQ();
    }

    public void Interpolate(ref SerializeContext context, ref MovableData first, ref MovableData last, float t)
    {
        var transform = context.entityManager.GetComponentObject<Transform>(context.entity);
        transform.position = Vector3.Lerp(first.position, last.position, t);
        transform.rotation = Quaternion.Lerp(first.rotation, last.rotation, t);
    }
}

[RequireComponent(typeof(Rigidbody))]
public class Movable : ComponentDataProxy<MovableData>
{
    public void Start()
    {
        if(Game.GetGameLoop<ServerGameLoop>() == null)
        {
            GetComponent<Rigidbody>().isKinematic = true;
        }
    }
}
