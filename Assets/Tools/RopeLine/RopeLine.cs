using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[InitializeOnLoad]
class RopeLineRunner
{
    static RopeLineRunner()
    {
        // Always unregister to prevent double registring
        EditorApplication.update -= RopeLineRunnerUpdate;
        EditorApplication.update += RopeLineRunnerUpdate;
        s_Ropes.Clear();
    }

    public static void RopeLineRunnerUpdate()
    {
        for(var i = s_Ropes.Count - 1; i >= 0; --i)
        {
            var r = s_Ropes[i];
            if (r != null)
            {
                if (r.simulate)
                    r.Tick();
            }
            else
                s_Ropes.EraseSwap(i);
        }
    }
    public static List<RopeLine> s_Ropes = new List<RopeLine>();
}
#endif


[RequireComponent(typeof(LineRenderer))]
[ExecuteInEditMode]
public class RopeLine : MonoBehaviour
{
    const float Dt = 0.02f;

    LineRenderer lineRenderer;

#if UNITY_EDITOR

    public bool simulate;

    public List<RopeAnchor> anchors = new List<RopeAnchor>();

    void Start()
    {
        simulate = false;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        CheckRebuildPositionBuffers();
    }

    private void OnEnable()
    {
        RopeLineRunner.s_Ropes.Add(this);
    }

    private void OnDisable()
    {
        RopeLineRunner.s_Ropes.Remove(this);
    }

    Vector3[] currPositions = new Vector3[0];
    Vector3[] prevPositions = new Vector3[0];

    public void RebuildPositionBuffer(Vector3 startPos, Vector3 endPos, int firstPos, int numSegments)
    {
        var dir = endPos - startPos;
        var length = dir.magnitude;
        for (var i = 0; i < numSegments + 1; i++)
        {
            currPositions[i + firstPos] = startPos + dir * (float)i / numSegments;
            prevPositions[i + firstPos] = currPositions[i + firstPos];
        }
    }

    void CheckRebuildPositionBuffers()
    {
        var children = GetComponentsInChildren<RopeAnchor>();

        // Add new anchors from below this object
        foreach (var a in children)
        {
            if (!anchors.Contains(a))
            {
                // When a new anchor is found, pick closest existing anchor point and insert next to it
                float dist = float.MaxValue;
                int best = 0;
                for (int i = 0; i < anchors.Count; i++)
                {
                    var d = Vector3.Distance(anchors[i].transform.localPosition, a.transform.localPosition);
                    if (d < dist)
                    {
                        dist = d;
                        best = i;
                    }
                }
                anchors.Insert(best, a);
            }
        }
        // Remove anchors that were deleted
        anchors.RemoveAll(x => x == null);

        if (anchors.Count < 2)
            return;

        // Segments and length on first anchor is unused
        anchors[0].numSegments = 0;
        anchors[0].length = 0;

        var totalSegments = 0;
        foreach (var a in anchors)
        {
            if(a != anchors[0])
            {
                if (a.numSegments < 3) a.numSegments = 3;
                if (a.length < 0.1f) a.length = 0.1f;
            }
            totalSegments += a.numSegments;
        }

        if (currPositions.Length == totalSegments + 1)
            return;

        currPositions = new Vector3[totalSegments + 1];
        prevPositions = new Vector3[totalSegments + 1];
        lineRenderer.positionCount = totalSegments + 1;

        int idx = 0;
        for (var i = 1; i < anchors.Count; i++)
        {
            RebuildPositionBuffer(anchors[i - 1].transform.localPosition, anchors[i].transform.localPosition, idx, anchors[i].numSegments);
            idx += anchors[i].numSegments;
        }
    }

    public void Simulate(float length, int firstPos, int numSegments)
    {
        var segmentLength = length / numSegments;

        for (var i = firstPos; i < firstPos + numSegments; i++)
        {
            Vector3 d = currPositions[i + 1] - currPositions[i];
            float dl = d.magnitude;
            if (dl < segmentLength)
                continue;
            float dif = (dl - segmentLength) / dl;
            float b = (i == firstPos) ? 0.0f : (i == firstPos + numSegments - 1) ? 1.0f : 0.5f;
            currPositions[i] += d * b * dif;
            currPositions[i + 1] -= d * (1.0f - b) * dif;
        }
    }

    public void Tick()
    {
        if(this == null || lineRenderer == null)
        {
            EditorApplication.update -= Tick;
            return;
        }
        CheckRebuildPositionBuffers();

        if (anchors.Count < 2)
            return;

        // Fix constraints
        int idx = 0;
        foreach (var a in anchors)
        {
            idx += a.numSegments;
            currPositions[idx] = a.transform.localPosition;
        }

        // Simulate
        idx = 0;
        foreach (var a in anchors)
        {
            if (a.numSegments > 0)
                Simulate(a.length, idx, a.numSegments);
            idx += a.numSegments;
        }

        // Apply gravity and copy to old pos
        var down = transform.InverseTransformDirection(Vector3.down);
        for (var i = 0; i < currPositions.Length; i++)
        {
            var old = currPositions[i];
            currPositions[i] = currPositions[i] + (currPositions[i] - prevPositions[i]) * 0.98f + 10.0f * down * Dt * Dt;
            prevPositions[i] = old;
        }

        lineRenderer.SetPositions(currPositions);
    }
#endif
}
