using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugOverlayCamera : MonoBehaviour
{

    void OnPostRender()
    {
        Line3DBuffer line3DBuffer = DebugOverlay.GetLine3DBuffer();
        if (line3DBuffer != null)
            line3DBuffer.Draw();
    }
}
