using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine.Networking;

public class ResizeBorderFrame : VisualElement
{
    List<ResizeSideHandle> m_ResizeSideHandles;

    bool m_MaintainApsectRatio;

    public bool maintainAspectRatio
    {
        get { return m_MaintainApsectRatio; }
        set
        {
            m_MaintainApsectRatio = value;
            foreach (ResizeSideHandle resizeHandle in m_ResizeSideHandles)
            {
                resizeHandle.maintainAspectRatio = value;
            }
        }
    }

    public Action OnResizeFinished;

    public ResizeBorderFrame(VisualElement target)
    {
        InitializeResizeBorderFrame(target, target);
    }

    public ResizeBorderFrame(VisualElement target, VisualElement container)
    {
        InitializeResizeBorderFrame(target, container);
    }

    void InitializeResizeBorderFrame(VisualElement target, VisualElement container)
    {
        pickingMode = PickingMode.Ignore;

        AddToClassList("resizeBorderFrame");

        m_ResizeSideHandles = new List<ResizeSideHandle>();

        // Add resize handles along the border
        // m_ResizeSideHandles.Add(new ResizeSideHandle(target, container, ResizeHandleAnchor.TopLeft));
        // m_ResizeSideHandles.Add(new ResizeSideHandle(target, container, ResizeHandleAnchor.Top));
        // m_ResizeSideHandles.Add(new ResizeSideHandle(target, container, ResizeHandleAnchor.TopRight));
        // m_ResizeSideHandles.Add(new ResizeSideHandle(target, container, ResizeHandleAnchor.Right));
        m_ResizeSideHandles.Add(new ResizeSideHandle(target, container, ResizeHandleAnchor.BottomRight));
        // m_ResizeSideHandles.Add(new ResizeSideHandle(target, container, ResizeHandleAnchor.Bottom));
        // m_ResizeSideHandles.Add(new ResizeSideHandle(target, container, ResizeHandleAnchor.BottomLeft));
        // m_ResizeSideHandles.Add(new ResizeSideHandle(target, container, ResizeHandleAnchor.Left));

        foreach (ResizeSideHandle resizeHandle in m_ResizeSideHandles)
        {
            resizeHandle.OnResizeFinished += HandleResizefinished;
            Add(resizeHandle);
        }
    }

    void HandleResizefinished()
    {
        if (OnResizeFinished != null)
        {
            OnResizeFinished();
        }
    }
}
