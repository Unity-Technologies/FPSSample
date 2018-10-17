using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeAnchor : MonoBehaviour
{
    public int numSegments = 5;
    public float length = 5;

#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        Gizmos.DrawCube(transform.position, Vector3.one * 0.05f);
    }
#endif
}
