using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A super crummy, poor mans cross scene reference setup for capture points
/// </summary>
public class CapturePointReference : MonoBehaviour
{
    public int capturePointIndex;

    public struct CapturePointAnimator
    {
        public int index;
        public Animator animator;
    }

    public static List<CapturePointAnimator> capturePointReferences = new List<CapturePointAnimator>();

    private void Awake()
    {
        capturePointReferences.Add(new CapturePointAnimator()
        {
            index = capturePointIndex,
            animator = GetComponent<Animator>()
        });
    }

    private void OnDestroy()
    {
        for(int i=0; i < capturePointReferences.Count; ++i)
        {
            var r = capturePointReferences[i];
            if(r.index == capturePointIndex && r.animator == GetComponent<Animator>())
            {
                capturePointReferences.RemoveAt(i);
                return;
            }
        }
    }

}
