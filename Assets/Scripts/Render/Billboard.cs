using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Billboard : MonoBehaviour
{
    private Camera lastcam;
    void OnWillRenderObject()
    {
        lastcam = Camera.current;
    }

    void Update()
    {
        if (lastcam == null)
            return;
        var horizontalForward = Vector3.Scale(lastcam.transform.forward, new Vector3(1, 0, 1)).normalized;
        transform.forward = horizontalForward;
    }
}
