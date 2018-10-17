using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomCameraSelector : MonoBehaviour
{
    public float cameraTime;
    public int numCameras;

    Animator cameraAnimator;
    float nextCamTime;
    List<int> camSeq = new List<int>();
    int seq;

    void Start()
    {
        cameraAnimator = GetComponent<Animator>();
        nextCamTime = Time.time + cameraTime;
        for (var i = 0; i < numCameras; i++)
            camSeq.Add(i);
        camSeq.Shuffle();
    }

    void Update()
    {
        if(Time.time > nextCamTime)
        {
            nextCamTime = Time.time + cameraTime;
            cameraAnimator.SetInteger("CameraNumber", camSeq[seq]);
            seq++;
            if(seq >= camSeq.Count)
            {
                seq = 0;
                camSeq.Shuffle();
            }
        }
    }
}
