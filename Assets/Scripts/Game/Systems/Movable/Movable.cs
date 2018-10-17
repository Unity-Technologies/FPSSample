using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Movable : MonoBehaviour, INetworkSerializable
{
    //Vector3 oldPosition;
    Vector3 newPosition;
    //Quaternion oldRotation;
    Quaternion newRotation;

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        //oldPosition = newPosition;
        //oldRotation = newRotation;
        newPosition = reader.ReadVector3Q();
        newRotation = reader.ReadQuaternionQ();
        transform.position = newPosition;
        transform.rotation = newRotation;
    }

    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteVector3Q("position", transform.position);
        writer.WriteQuaternionQ("rotation", transform.rotation);
    }

    public void Start()
    {
        if(Game.GetGameLoop<ServerGameLoop>() == null)
        {
            GetComponent<Rigidbody>().isKinematic = true;
        }
    }
}
