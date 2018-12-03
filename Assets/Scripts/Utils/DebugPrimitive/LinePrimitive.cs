using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class LinePrimitive: MonoBehaviour , INetSerialized
{
	public Vector3 pA;
	public Vector3 pB;
	public Color color;
    
	public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
	{
		writer.WriteVector3Q("center",pA,3);
		writer.WriteVector3Q("center",pB,3);
		writer.WriteVector3Q("color",new Vector3(color.r, color.g, color.b),2);
	}

	public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
	{
		pA = reader.ReadVector3Q();
		pB = reader.ReadVector3Q();
		var v = reader.ReadVector3Q();
		color = new Color(v.x, v.y, v.z);
	}
}

[DisableAutoCreation]
public class DrawLinePrimitives : ComponentSystem
{
	ComponentGroup Group;

	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		Group = GetComponentGroup(typeof(LinePrimitive));
	}

	protected override void OnUpdate()
	{
		var primArray = Group.GetComponentArray<LinePrimitive>();
		for (int i = 0, c = primArray.Length; i < c; i++)
		{
			var line = primArray[i];
			Debug.DrawLine(line.pA, line.pB, line.color);
		}
	}
}