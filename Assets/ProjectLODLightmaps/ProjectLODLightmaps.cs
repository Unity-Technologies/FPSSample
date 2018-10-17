using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectLODLightmaps : MonoBehaviour {

    public Renderer m_Renderer;

    public void SetupRenderer()
    {
        var renderer = GetComponent<Renderer>();

        if (renderer && m_Renderer)
        {
            renderer.lightmapScaleOffset = m_Renderer.lightmapScaleOffset;
            renderer.lightmapIndex = m_Renderer.lightmapIndex;
        }
    }
}
