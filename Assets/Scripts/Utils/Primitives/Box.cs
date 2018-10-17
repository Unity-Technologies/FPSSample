using System;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace Primitives
{
	using Unity.Mathematics;

	[Serializable]
	public struct box
	{
		public float3 center;
		public quaternion rotation;  
		public float3 size;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public box(float3 center, Quaternion rotation, float3 size)
		{
			this.center = center;
			this.rotation = rotation;
			this.size = size;
		}
	}
	
	public static partial class primlib
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static box box(float3 center, Quaternion rotation, float3 size) { return new box(center,rotation,size); }
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static box transform(box prim, float3 position, Quaternion rotation)
		{
			prim.center = rotation * prim.center + (Vector3) position;
			prim.rotation = rotation * prim.rotation;
			return prim;
		}
	}	
}

