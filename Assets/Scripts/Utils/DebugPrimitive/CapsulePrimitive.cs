using Unity.Collections;
using UnityEngine;
using Unity.Entities;

public class CapsulePrimitive : MonoBehaviour, INetworkSerializable
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
    struct CapsulesGroup
    {
        [ReadOnly]
        public ComponentArray<CapsulePrimitive> capsules;
    }

    [Inject]
    CapsulesGroup Group;

    protected override void OnUpdate()
    {
        for (int i = 0, c = Group.capsules.Length; i < c; i++)
        {
            var capsule = Group.capsules[i];
            var v = capsule.pB - capsule.pA;
            var center = capsule.pA + v * 0.5f;
            DebugDraw.Capsule(center, v.normalized, capsule.radius, v.magnitude + capsule.radius*2, capsule.color);
        }
    }
}