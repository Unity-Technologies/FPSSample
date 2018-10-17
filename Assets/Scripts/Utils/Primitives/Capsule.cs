using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Unity.Mathematics.math;
namespace Primitives
{
	using Unity.Mathematics;

	[Serializable]
	public struct capsule
	{
		public float3 p1;
		public float3 p2;
		public float radius;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public capsule(float3 p1, float3 p2, float radius)
		{
			this.p1 = p1;
			this.p2 = p2;
			this.radius = radius;
		}
	}
	
	public static partial class primlib
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static capsule capsule(float3 p1, float3 p2, float radius) { return new capsule(p1, p2, radius); }
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static capsule transform(capsule prim, float3 position, Quaternion rotation)
		{
			prim.p1 = rotation * prim.p1 + (Vector3) position;
			prim.p2 = rotation * prim.p2 + (Vector3) position;
			return prim;
		}
	}
}

