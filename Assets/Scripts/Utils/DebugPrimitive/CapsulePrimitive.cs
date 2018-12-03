using Unity.Collections;
using UnityEngine;
using Unity.Entities;

public class CapsulePrimitive : MonoBehaviour, INetSerialized
{
    public Vector3 pA;
    public Vector3 pB;
    public float radius;
    public Color color;
    
    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteVector3Q("pA",pA,3);
        writer.WriteVector3Q("pB",pB,3);
        writer.WriteFloatQ("width", radius,3);
        writer.WriteVector3Q("color",new Vector3(color.r, color.g, color.b),2);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        pA = reader.ReadVector3Q();
        pB = reader.ReadVector3Q();
        radius = reader.ReadFloatQ();
        var v = reader.ReadVector3Q();
        color = new Color(v.x, v.y, v.z);
    }
}

[DisableAutoCreation]
public class DrawCapsulePrimitives : ComponentSystem
{
    ComponentGroup Group;

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(CapsulePrimitive));
    }

    protected override void OnUpdate()
    {
        var capsuleArray = Group.GetComponentArray<CapsulePrimitive>();
        for (int i = 0, c = capsuleArray.Length; i < c; i++)
        {
            var capsule = capsuleArray[i];
            var v = capsule.pB - capsule.pA;
            var center = capsule.pA + v * 0.5f;
            DebugDraw.Capsule(center, v.normalized, capsule.radius, v.magnitude + capsule.radius*2, capsule.color);
        }
    }
}