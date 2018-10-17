using System;
using System.Runtime.CompilerServices;
using static Unity.Mathematics.math;

namespace Primitives
{
	using Unity.Mathematics;

	[Serializable]
	public struct sphere
	{
		public float3 center;
		public float radius;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public sphere(float3 center, float radius)
		{
			this.center = center;
			this.radius = radius;
		}
	}
	
	public static partial class primlib
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static sphere sphere(float3 center, float radius) { return new sphere(center,radius); }
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static sphere transform(sphere prim, float3 position, quaternion rotation)
		{
			prim.center = mul(rotation, prim.center) + position;
			return prim;
		}
	}
}

