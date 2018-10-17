using UnityEngine;
using UnityEngine.Experimental.Animations;

public static class AnimJobUtilities
{
  public static void SolveTwoBoneIK 
  (
        AnimationStream stream,
        TransformStreamHandle topHandle,
        TransformStreamHandle midHandle,
        TransformStreamHandle lowHandle,
        Vector3 effectorPosition,
        Quaternion effectorRotation,
        float posWeight,
        float rotWeight)
    {
        Quaternion aRotation = topHandle.GetRotation(stream);
        Quaternion bRotation = midHandle.GetRotation(stream);

        Vector3 aPosition = topHandle.GetPosition(stream);
        Vector3 bPosition = midHandle.GetPosition(stream);
        Vector3 cPosition = lowHandle.GetPosition(stream);

        Vector3 ab = bPosition - aPosition;
        Vector3 bc = cPosition - bPosition;
        Vector3 ac = cPosition - aPosition;
        Vector3 ad = (Vector3.Lerp(cPosition, effectorPosition, posWeight) - aPosition);

        float oldAbcAngle = TriangleAngle(ac.magnitude, ab, bc);
        float newAbcAngle = TriangleAngle(ad.magnitude, ab, bc);

        Vector3 axis = Vector3.Cross(ab, bc).normalized;
        float a = 0.5f * (oldAbcAngle - newAbcAngle);
        float sin = Mathf.Sin(a);
        float cos = Mathf.Cos(a);
        Quaternion q = new Quaternion(axis.x * sin, axis.y * sin, axis.z * sin, cos);

        Quaternion worldQ = q * bRotation;
        midHandle.SetRotation(stream, worldQ);

        aRotation = topHandle.GetRotation(stream);
        cPosition = lowHandle.GetPosition(stream);
        ac = cPosition - aPosition;
        Quaternion fromTo = Quaternion.FromToRotation(ac, ad);
        topHandle.SetRotation(stream, fromTo * aRotation);
        lowHandle.SetRotation(stream, Quaternion.Lerp(lowHandle.GetRotation(stream), effectorRotation, rotWeight));
    }
    
    static float TriangleAngle(float aLen, Vector3 v1, Vector3 v2)
    {
        float aLen1 = v1.magnitude;
        float aLen2 = v2.magnitude;
        float c = Mathf.Clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
        return Mathf.Acos(c);
    }
}
