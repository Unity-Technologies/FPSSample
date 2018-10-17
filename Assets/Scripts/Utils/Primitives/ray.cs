using System.Runtime.CompilerServices;
using UnityEngine;

using static Unity.Mathematics.math;
namespace CollisionLib
{
	using Unity.Mathematics;

	public struct ray
	{
		public float3 origin;
		public float3 direction;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ray(float3 origin, float3 direction)
		{
			this.origin = origin;
			this.direction = direction;
		}
	}
	
	public static partial class coll
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ray ray(float3 origin, float3 direction) { return new ray(origin, direction); }

	}
}

