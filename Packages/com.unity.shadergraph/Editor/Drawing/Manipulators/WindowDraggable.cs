using System;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class WindowDraggable : MouseManipulator
    {
        bool m_Active;

        WindowDockingLayout m_WindowDockingLayout;

        Vector2 m_LocalMosueOffset;

        VisualElement m_Handle;
        GraphView m_GraphView;

        public Action OnDragFinished;

        public WindowDraggable(VisualElement handle = null, VisualElement container = null)
        {
            m_Handle = handle;
            m_Active = false;
            m_WindowDockingLayout = new WindowDockingLayout();

            if (container != null)
                container.RegisterCallback<GeometryChangedEvent>(OnParentGeometryChanged);
        }

        protected override void RegisterCallbacksOnTarget()
        {
            if (m_Handle == null)
                m_Handle = target;
            m_Handle.RegisterCallback(new EventCallback<MouseDownEvent>(OnMouseDown), TrickleDownEnum.NoTrickleDown);
            m_Handle.RegisterCallback(new EventCallback<MouseMoveEvent>(OnMouseMove), TrickleDownEnum.NoTrickleDown);
            m_Handle.RegisterCallback(new EventCallback<MouseUpEvent>(OnMouseUp), TrickleDownEnum.NoTrickleDown);
            target.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            m_Handle.UnregisterCallback(new EventCallback<MouseDownEvent>(OnMouseDown), TrickleDownEnum.NoTrickleDown);
            m_Handle.UnregisterCallback(new EventCallback<MouseMoveEvent>(OnMouseMove), TrickleDownEnum.NoTrickleDown);
            m_Handle.UnregisterCallback(new EventCallback<MouseUpEvent>(OnMouseUp), TrickleDownEnum.NoTrickleDown);
            target.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            if (m_GraphView != null)
                m_GraphView.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            m_Active = true;

            VisualElement parent = target.parent;
            while (parent != null && !(parent is GraphView))
                parent = parent.parent;
            m_GraphView = parent as GraphView;

            if (m_GraphView != null)
                m_GraphView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            // m_LocalMouseOffset is offset from the target element's (0, 0) to the
            // to the mouse position.
            m_LocalMosueOffset = m_Handle.WorldToLocal(evt.mousePosition);

            m_Handle.CaptureMouse();
            evt.StopImmediatePropagation();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_Active)
            {
                // The mouse position of is corrected according to the offset within the target
                // element (m_LocalWorldOffset) to set the position relative to the mouse position
                // when the dragging started.
                Vector2 position = target.parent.WorldToLocal(evt.mousePosition) - m_LocalMosueOffset;

                // Make sure that the object remains in the parent window
                position.x = Mathf.Clamp(position.x, 0f, target.parent.layout.width - target.layout.width);
                position.y = Mathf.Clamp(position.y, 0f, target.parent.layout.height - target.layout.height);

                // While moving, use only the left and top position properties,
                // while keeping the others NaN to not affect layout.
                target.style.positionLeft = StyleValue<float>.Create(position.x);
                target.style.positionTop = StyleValue<float>.Create(position.y);
                target.style.positionRight = StyleValue<float>.Create(float.NaN);
                target.style.positionBottom = StyleValue<float>.Create(float.NaN);
            }
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            bool emitDragFinishedEvent = m_Active;

            m_Active = false;

            if (m_Handle.HasMouseCapture())
            {
                m_Handle.ReleaseMouse();
            }

            evt.StopImmediatePropagation();

            // Recalculate which corner to dock to
            m_WindowDockingLayout.CalculateDockingCornerAndOffset(target.layout, target.parent.layout);
            m_WindowDockingLayout.ClampToParentWindow();

            // Use the docking results to figure which of left/right and top/bottom needs to be set.
            m_WindowDockingLayout.ApplyPosition(target);

            // Signal that the dragging has finished.
            if (emitDragFinishedEvent && OnDragFinished != null)
                OnDragFinished();
        }

        void OnGeometryChanged(GeometryChangedEvent geometryChangedEvent)
        {
            // Make the target clamp to the border of the window if the
            // parent window becomes too small to contain it.
            if (target.parent.layout.width < target.layout.width)
            {
                if (m_WindowDockingLayout.dockingLeft)
                {
                    target.style.positionLeft = StyleValue<float>.Create(0f);
                    target.style.positionRight = StyleValue<float>.Create(float.NaN);
                }
                else
                {
                    target.style.positionLeft = StyleValue<float>.Create(float.NaN);
                    target.style.positionRight = StyleValue<float>.Create(0f);
                }
            }

            if (target.parent.layout.height < target.layout.height)
            {
                if (m_WindowDockingLayout.dockingTop)
                {
                    target.style.positionTop = StyleValue<float>.Create(0f);
                    target.style.positionBottom = StyleValue<float>.Create(float.NaN);
                }
                else
                {
                    target.style.positionTop = StyleValue<float>.Create(float.NaN);
                    target.style.positionBottom = StyleValue<float>.Create(0f);
                }
            }
        }

        void OnParentGeometryChanged(GeometryChangedEvent geometryChangedEvent)
        {
            // Check if the parent window can no longer contain the target window.
            // If the window is out of bounds, make one edge clamp to the border of the
            // parent window.
            if (target.layout.xMin < 0f)
            {
                target.style.positionLeft = StyleValue<float>.Create(0f);
                target.style.positionRight = StyleValue<float>.Create(float.NaN);
            }

            if (target.layout.xMax > geometryChangedEvent.newRect.width)
            {
                target.style.positionLeft = StyleValue<float>.Create(float.NaN);
                target.style.positionRight = StyleValue<float>.Create(0f);
            }

            if (target.layout.yMax > geometryChangedEvent.newRect.height)
            {
                target.style.positionTop = StyleValue<float>.Create(float.NaN);
                target.style.positionBottom = StyleValue<float>.Create(0f);
            }

            if (target.layout.yMin < 0f)
            {
                target.style.positionTop = StyleValue<float>.Create(0f);
                target.style.positionBottom = StyleValue<float>.Create(float.NaN);
            }
        }
    }
}
