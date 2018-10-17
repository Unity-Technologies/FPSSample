using Unity.Mathematics;
using UnityEngine;

public static class LODGroupExtensions
{
    public struct LODParams
    {
        public float  lodBias;
        public float3 cameraPos;
        public float  screenRelativeMetric;

        public bool   isOrtho;
        public float  orthosize;
    }

    public static LODParams CalculateLODParams(Camera camera)
    {
        LODParams lodParams;
        lodParams.cameraPos = camera.transform.position;
        lodParams.isOrtho = camera.orthographic;
        lodParams.orthosize= camera.orthographicSize;
        lodParams.lodBias = QualitySettings.lodBias;

        var halfAngle = math.tan(math.radians(camera.fieldOfView * 0.5F));

        float screenRelativeMetric;
        if (lodParams.isOrtho)
        {
            screenRelativeMetric = 2.0F * lodParams.orthosize;
        }
        else
        {
            // Half angle at 90 degrees is 1.0 (So we skip halfAngle / 1.0 calculation)
            screenRelativeMetric = (2.0f * halfAngle) / lodParams.lodBias;
            screenRelativeMetric = screenRelativeMetric * screenRelativeMetric;
        }

        lodParams.screenRelativeMetric = screenRelativeMetric;

        return lodParams;
    }
    
    public static float GetWorldSpaceSize(LODGroup lodGroup)
    {
        return GetWorldSpaceScale(lodGroup.transform) * lodGroup.size;
    }

    static float GetWorldSpaceScale(Transform t)
    {
        var scale = t.lossyScale;
        float largestAxis = Mathf.Abs(scale.x);
        largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.y));
        largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.z));
        return largestAxis;
    }
    
    public static int CalculateCurrentLODIndex(float4 lodDistances, float3 worldReferencePoint, ref LODParams lodParams)
    {
        var distanceSqr = CalculateDistanceSqr(worldReferencePoint, ref lodParams);
        var lodIndex = CalculateCurrentLODIndex(lodDistances, distanceSqr);
        return lodIndex;
    }
    
    public static int CalculateCurrentLODMask(float4 lodDistances, float3 worldReferencePoint, ref LODParams lodParams)
    {
        var distanceSqr = CalculateDistanceSqr(worldReferencePoint, ref lodParams);
        return CalculateCurrentLODMask(lodDistances, distanceSqr);
    }

    static int CalculateCurrentLODIndex(float4 lodDistances, float measuredDistanceSqr)
    {
        var lodResult = measuredDistanceSqr < (lodDistances * lodDistances);
        if (lodResult.x)
            return 0;
        else if (lodResult.y)
            return 1;
        else if (lodResult.z)
            return 2;
        else if (lodResult.w)
            return 3;
        else
            
            // Can return 0 or 16. Doesn't matter...
            return -1;
    }
    
    static int CalculateCurrentLODMask(float4 lodDistances, float measuredDistanceSqr)
    {
        var lodResult = measuredDistanceSqr < (lodDistances * lodDistances);
        if (lodResult.x)
            return 1;
        else if (lodResult.y)
            return 2;
        else if (lodResult.z)
            return 4;
        else if (lodResult.w)
            return 8;
        else
            // Can return 0 or 16. Doesn't matter...
            return 16;
    }

    static float CalculateDistanceSqr(float3 worldReferencePoint, ref LODParams lodParams)
    {
        if (lodParams.isOrtho)
        {
            //return worldSpaceSize * lodParams.screenRelativeMetric;
            //@TODO:
            throw new System.NotImplementedException();
        }
        else
        {
            return math.lengthSquared(lodParams.cameraPos - worldReferencePoint) * lodParams.screenRelativeMetric;
        }
    }
}