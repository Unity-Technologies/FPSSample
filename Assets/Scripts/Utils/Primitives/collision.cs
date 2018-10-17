using System.Runtime.CompilerServices;
using UnityEngine;
using static Unity.Mathematics.math;
namespace CollisionLib
{
	using Unity.Mathematics;
	using Primitives;

	public static partial class coll
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool RayCast(sphere sphere, ray ray, float rayDist)
		{
			var rayEnd = ray.origin + ray.direction * rayDist;
			var closestPoint = ClosestPointOnLineSegment(ray.origin, rayEnd, sphere.center);

			var dist = distance(closestPoint, sphere.center);
			return dist < sphere.radius;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ProjectPointToLineSegment(float3 lineStart, float3 lineEnd, float3 point, out float t)
		{
			var v = lineStart - lineEnd;
			t = dot(point - lineStart, v) / dot(v, v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 closest(lineseg lineseg, float3 point)
		{
			return ClosestPointOnLineSegment(lineseg.start, lineseg.end, point);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 ClosestPointOnLineSegment(float3 lineStart, float3 lineEnd, float3 point)
		{
			var v = lineEnd - lineStart;
			var t = dot(point - lineStart, v) / dot(v, v);
			t = max(t, 0.0f);
			t = min(t, 1.0f);
			return lineStart + v * t;
		}
		

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float SqrLineSegmentDist(lineseg line1, lineseg line2)
		{
			
			return 1;
		}
	}
}