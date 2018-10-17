using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class LinePrimitive: MonoBehaviour , INetworkSerializable
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
	struct LineGroup
	{
		[ReadOnly]
		public ComponentArray<LinePrimitive> lines;
	}

	[Inject]
	LineGroup Group;

	protected override void OnUpdate()
	{
		for (int i = 0, c = Group.lines.Length; i < c; i++)
		{
			var line = Group.lines[i];
			Debug.DrawLine(line.pA, line.pB, line.color);
		}
	}
}