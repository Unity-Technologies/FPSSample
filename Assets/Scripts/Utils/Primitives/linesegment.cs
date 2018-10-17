using System.Runtime.CompilerServices;
using UnityEngine;

using static Unity.Mathematics.math;
namespace Primitives
{
	using Unity.Mathematics;

	public struct lineseg
	{
		public float3 start;
		public float3 end;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public lineseg(float3 start, float3 end)
		{
			this.start = start;
			this.end = end;
		}
	}
	
	public static partial class primlib
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static lineseg lineseg(float3 p1, float3 p2) { return new lineseg(p1, p2); }
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static lineseg transform(lineseg prim, float3 position, Quaternion rotation)
		{
			prim.start = rotation * prim.start + (Vector3) position;
			prim.end = rotation * prim.end + (Vector3) position;
			return prim;
		}
	}
}

