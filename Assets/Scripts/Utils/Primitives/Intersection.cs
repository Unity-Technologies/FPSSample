using System.Runtime.CompilerServices;
using Primitives;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class InstersectionHelper
{
    static float SafeClampRatio(float n, float d)
    {
        return (n <= 0 ? 0 : (n >= d ? 1 : n / d));
    }

    public static bool IntersectCapsuleCapsule(ref capsule c0, ref capsule c1)
    {
        float3 D1 = c0.p2 - c0.p1;
        float3 D2 = c1.p2 - c1.p1;
        float3 diff = c0.p1 - c1.p1;
        float s, t;
    
        float a = math.dot(D1, D1);
        float b = math.dot(D1, D2);
        float c = math.dot(D2, D2);
        float d = math.dot(D1, diff);
        float e = math.dot(D2, diff);
    
        float det = a * c - b * b;
    
        if (det > float.Epsilon)
        {
            s = b * e - c * d;
            t = a * e - b * d;
    
            if (s <= 0)
            {
                // The projection of A1 onto L2 is t = e / c.  Figure out what region that lies in (negative, positive, or inside the interval).
                if (e <= 0)
                {
                    t = 0;
                    s = SafeClampRatio(-d, a);
                }
                else if (e < c) // t in (0,1)
                {
                    s = 0;
                    t = e / c;
                }
                else // t >= 1
                {
                    t = 1;
                    s = SafeClampRatio(b - d, a);
                }
            }
            else
            {
                if (s >= det) // s >= 1
                {
                    // The projection of A1+D1 onto L2 is t = (b + e) / c.
                    if (b + e <= 0) // t <= 0
                    {
                        t = 0;
                        s = SafeClampRatio(-d , a);
                    }
                    else if (b + e < c) // t in (0,1)
                    {
                        s = 1;
                        t = (b + e) / c;
                    }
                    else
                    {
                        t = 1;
                        s = SafeClampRatio(b - d, a);
                    }
                }
                else // s in (0,1)
                {
                    if (t <= 0)
                    {
                        t = 0;
                        s = SafeClampRatio(-d, a);
                    }
                    else
                    {
                        if (t >= det) // t >= 1
                        {
                            t = 1;
                            s = SafeClampRatio(b - d, a);
                        }
                        else
                        {
                            // the minimum is inside the unit square, just compute the unconstrained version
                            s /= det;
                            t /= det;
                        }
                    }
                }
            }
        }
        else
        {
            // Parallel axes, pick the endpoints
            if (e <= 0)  // Proj(A1,L2) = e / c
            {
                t = 0;
                s = SafeClampRatio(-d, a);
            }
            else if (e < c)  // t in (0,1)
            {
                s = 0;
                t = e / c;
            }
            else
            {
                t = 1;
                s = SafeClampRatio(b - d, a);
            }
        }
    
        // Finally, get the distance between points and compare to the sum of radiuses
        float3 comp = math.lerp(c0.p1, c0.p2, s) - math.lerp(c1.p1, c1.p2, t);
        float radSum = c0.radius + c1.radius;
        return (math.lengthsq(comp) <= radSum * radSum);
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IntersectAABBFrustum(ref Bounds a,NativeArray<Plane> planes, uint inClipMask)
    {
        float3 m       = a.center; // center of AABB
        float3 extent  = a.extents; // half-diagonal
    
        for(int i=0;i<planes.Length;i++)
        {
            var p = planes[i];
            uint mk = (uint)1 << i;
            
            // if clip plane is active...
            if ((inClipMask & mk) > 0)
            {
                float3 normal = p.normal;
                float dist = math.dot(p.normal, m) + p.distance;
                float radius = math.dot(extent, math.abs(normal));
    
                if (dist + radius < 0)
                    return false;                    // behind clip plane
            }
        }
        return true; 
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TestPlanesAABB(NativeArray<Plane> planes, ref Bounds bounds)
    {
        uint planeMask = 0;
        if (planes.Length == 6)
        {
            planeMask = 63;
        }
        else
        {
            for (int i = 0; i < planes.Length; ++i)
                planeMask |= (uint)1 << i;
        }
    
        return IntersectAABBFrustum(ref bounds, planes, planeMask);
    }
}
