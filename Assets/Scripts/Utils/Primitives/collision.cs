using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine;
using static Unity.Mathematics.math;
namespace CollisionLib
{
	using Unity.Mathematics;
	using Primitives;

	public static partial class coll
	{
		public static bool RayCast(sphere sphere, ray ray, float rayDist, float rayRadius)
		{
			var rayEnd = ray.origin + ray.direction * rayDist;
			var closestPointOnRay = ClosestPointOnLineSegment(ray.origin, rayEnd, sphere.center);
			var dist = distance(closestPointOnRay, sphere.center);
			return dist < rayRadius + sphere.radius;
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

		public static float DistanceToSegment(float3 lineStart, float3 lineEnd, float3 point, out float3 closest)
		{
			closest = ClosestPointOnLineSegment(lineStart, lineEnd, point);
			return distance(point, closest);
		}


		public static bool OverlapCapsuleAABB(capsule capsule, box AABB)
		{
			var closestOnCapsule = ClosestPointOnLineSegment(capsule.p1, capsule.p2, AABB.center);

			float sqDist;
			float3 closestInBox;
			CalculateClosestPointInBox(closestOnCapsule, AABB, out closestInBox, out sqDist);
			return math.sqrt(sqDist) < capsule.radius;
		}


		public static bool OverlapCapsuleBox(capsule capsule, box box)
		{
			// Transform capsule into box space
			var rayCapsule = capsule;
			var invBoxRot = math.inverse(box.rotation);
			var invBoxPos = - math.mul(invBoxRot,box.center);
			rayCapsule = primlib.transform(rayCapsule, invBoxPos, invBoxRot);

			var primLocalSpace = box;
			primLocalSpace.center = float3.zero;
			primLocalSpace.rotation = Quaternion.identity;

			var hit = coll.OverlapCapsuleAABB(rayCapsule, primLocalSpace);
			return hit;
		}
		
		
		public static void CalculateClosestPointInBox(float3 point, box AABB, out float3 outPoint, out float outSqrDistance)
		{
			// compute coordinates of point in box coordinate system
			float3 closest = point - AABB.center;

			float3 halfSize = AABB.size * 0.5f;
			// project test point onto box
			float fSqrDistance = 0.0f;
			float fDelta;

			for (int i = 0; i < 3; i++)
			{
				if (closest[i] < -halfSize[i])
				{
					fDelta = closest[i] + halfSize[i];
					fSqrDistance += fDelta * fDelta;
					closest[i] = -halfSize[i];
				}
				else if (closest[i] > halfSize[i])
				{
					fDelta = closest[i] - halfSize[i];
					fSqrDistance += fDelta * fDelta;
					closest[i] = halfSize[i];
				}
			}

			// Inside
			if (fSqrDistance == 0.0F)
			{
				outPoint = point;
				outSqrDistance = 0.0F;
			}
			// Outside
			else
			{
				outPoint = closest + AABB.center;
				outSqrDistance = fSqrDistance;
			}
		}
	}
	
}