using UnityEngine;
using System.Collections.Generic;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(BoxCollider))]
public class LightProbesVolumeSettings : MonoBehaviour
{
    public float horizontalSpacing = 2.0f;
    public float verticalSpacing = 2.0f;
    public float OffsetFomFloor = 0.5f;
    public int numberOfLayers = 2;
    public bool FillVolume = false;
    public bool FollowFloor = true;
    public bool discardInsideGeometry;
    public bool drawDebug = false;

    private void OnEnable()
    {
        var boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;
    }

#if UNITY_EDITOR
    public void Populate()
    {
        //avoid division by 0
        horizontalSpacing = Mathf.Max(horizontalSpacing, 0.01f);
        verticalSpacing = Mathf.Max(horizontalSpacing, 0.01f);

        Collider col = gameObject.GetComponent<Collider>();
        if (col == null) Debug.Log("Col not found", col);

        //Check if there is already a lightprobegroup component
        // if there is destroy it
        LightProbeGroup oldLightprobes = gameObject.GetComponent<LightProbeGroup>();
        if (oldLightprobes != null)
            DestroyImmediate(oldLightprobes);

        // Get the col bounds
        Bounds bbox = col.bounds;
        gameObject.GetComponent<BoxCollider>().enabled = false;

        //Store bounds
        
        float minX = bbox.min.x;
        float minY = bbox.min.y;
        float minZ = bbox.min.z;
        float maxX = bbox.max.x;
        float maxY = bbox.max.y;
        float maxZ = bbox.max.z;

        // Now go through in a grid and attempt to place a light probe using raycasting
        float xCount = (maxX - minX)/horizontalSpacing;
        float zCount = (maxZ - minZ) / horizontalSpacing;
        float ycount = (maxY - minY) / verticalSpacing;
        float startxoffset = ((maxX - minX) - Mathf.FloorToInt(xCount) * horizontalSpacing) / 2;
        float startzoffset = ((maxZ - minZ) - Mathf.FloorToInt(zCount) * horizontalSpacing) / 2;

        //if lightprobe count fits exactly in bounds, I know the probes at the maximum bounds will be rejected, so add offset
        if (startxoffset == 0)
            startxoffset = horizontalSpacing / 2;
        if (startzoffset == 0)
            startzoffset = horizontalSpacing / 2;

        List<Vector3> VertPositions = new List<Vector3>();

        for (int z = 0; z < zCount; z++)
        {
            for (int x = 0; x < xCount; x++)
            {
                //RaycastHit hit;
                RaycastHit[] hits;
                Ray ray = new Ray();
                ray.origin = new Vector3(startxoffset + minX + x * horizontalSpacing, maxY + 1, startzoffset + minZ + z * horizontalSpacing);
                ray.direction = -Vector3.up;
                //if (Physics.Raycast(ray, out hit, (maxY - minY) * 2,-1,QueryTriggerInteraction.Ignore))
                hits = Physics.RaycastAll(ray, (maxY - minY) * 2, -1, QueryTriggerInteraction.Ignore);
                foreach(var hit in hits)
                {
                    if (!hit.collider.gameObject.isStatic)
                        break;
                    if (hit.point.y + OffsetFomFloor < maxY && hit.point.y + OffsetFomFloor > minY)
                        VertPositions.Add(hit.point + new Vector3(0, OffsetFomFloor, 0));
                    if(drawDebug)
                        Debug.DrawRay(hit.point, -ray.direction * hit.distance, Color.red, (maxY - minY));

                    int maxLayer = FillVolume ? Mathf.FloorToInt(ycount) : numberOfLayers ;

                    for (int i = 1; i < maxLayer; i++)
                    {
                        if (hit.point.y + OffsetFomFloor + i * verticalSpacing < maxY && hit.point.y + OffsetFomFloor + verticalSpacing > minY)
                            VertPositions.Add(hit.point + new Vector3(0, OffsetFomFloor + i * verticalSpacing, 0));
                    }
                    EditorUtility.DisplayProgressBar("Tracing floor collisions", (z * x).ToString() + "/" + (zCount * xCount).ToString(), (float)(z * x) / (float)(zCount * xCount));
                }
            }
        }
        EditorUtility.ClearProgressBar();
        List<Vector3> validVertPositions = new List<Vector3>();

        int j = 0;
        if (discardInsideGeometry)
        {
            Vector3 insideTestPosition = gameObject.transform.position + gameObject.GetComponent<BoxCollider>().center + new Vector3(0,maxY/2,0);
            if(drawDebug)
            {
                Debug.DrawLine(insideTestPosition + Vector3.up, insideTestPosition - Vector3.up, Color.green, 5);
                Debug.DrawLine(insideTestPosition + Vector3.right, insideTestPosition - Vector3.right, Color.green, 5);
                Debug.DrawLine(insideTestPosition + Vector3.forward, insideTestPosition - Vector3.forward, Color.green, 5);
            }
            foreach (Vector3 positionCandidate in VertPositions)
            {
                EditorUtility.DisplayProgressBar("Checking probes inside geometry", j.ToString() + "/" + VertPositions.Count, (float)j / (float)VertPositions.Count);

                Ray forwardRay = new Ray(insideTestPosition, Vector3.Normalize(positionCandidate - insideTestPosition));
                Ray backwardRay = new Ray(positionCandidate, Vector3.Normalize(insideTestPosition - positionCandidate));
                RaycastHit[] hitsForward;
                RaycastHit[] hitsBackward;
                hitsForward = Physics.RaycastAll(forwardRay,Vector3.Distance(positionCandidate,insideTestPosition),-1,QueryTriggerInteraction.Ignore);
                hitsBackward = Physics.RaycastAll(backwardRay, Vector3.Distance(positionCandidate, insideTestPosition), -1, QueryTriggerInteraction.Ignore);
                if (hitsForward.Length == hitsBackward.Length) validVertPositions.Add(positionCandidate);
                else if (drawDebug)
                    Debug.DrawRay(backwardRay.origin, backwardRay.direction * Vector3.Distance(positionCandidate, insideTestPosition), Color.red, 5);
                j++;
            }
            EditorUtility.ClearProgressBar();
        }
        else
            validVertPositions = VertPositions;


        // Check if we have any hits
        if (validVertPositions.Count < 1)
        {
            Debug.Log("no valid hit for "+ gameObject.name);
            gameObject.GetComponent<BoxCollider>().enabled = true;
            return;
        }

        LightProbeGroup LPGroup = gameObject.AddComponent<LightProbeGroup>();

        // Create lightprobe positions
        Vector3[] ProbePos = new Vector3[validVertPositions.Count];
        for (int i = 0; i < validVertPositions.Count; i++)
        {
            ProbePos[i] = gameObject.transform.InverseTransformPoint(validVertPositions[i]); 
        }

        // Set new light probes
        LPGroup.probePositions = ProbePos;
        gameObject.GetComponent<BoxCollider>().enabled = true;
        Debug.Log("Finished placing " + ProbePos.Length + " probes for " + gameObject.name);
    }
#endif
}