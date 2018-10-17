using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.ShaderGraph.Drawing
{
    enum ResizeDirection
    {
        Any,
        Vertical,
        Horizontal
    }

    public enum ResizeHandleAnchor
    {
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left,
        TopLeft
    }

    public class ResizeSideHandle : VisualElement
    {
        VisualElement m_ResizeTarget;
        VisualElement m_Container;

        WindowDockingLayout m_WindowDockingLayout;

        bool m_MaintainAspectRatio;

        public bool maintainAspectRatio
        {
            get { return m_MaintainAspectRatio; }
            set { m_MaintainAspectRatio = value; }
        }

        public Action OnResizeFinished;

        bool m_Dragging;

        Rect m_ResizeBeginLayout;
        Vector2 m_ResizeBeginMousePosition;

        private GUIStyle m_StyleWidget;
        private GUIStyle m_StyleLabel;
        private Texture image { get; set; }

        public ResizeSideHandle(VisualElement resizeTarget, VisualElement container, ResizeHandleAnchor anchor)
        {
            m_WindowDockingLayout = new WindowDockingLayout();

            m_ResizeTarget = resizeTarget;
            m_Container = container;

            AddToClassList("resize");

            switch (anchor)
            {
                case ResizeHandleAnchor.Top:
                {
                    AddToClassList("vertical");
                    AddToClassList("top");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromTop);
                    break;
                }
                case ResizeHandleAnchor.TopRight:
                {
                    AddToClassList("diagonal");
                    AddToClassList("top-right");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromTopRight);
                    break;
                }
                case ResizeHandleAnchor.Right:
                {
                    AddToClassList("horizontal");
                    AddToClassList("right");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromRight);
                    break;
                }
                case ResizeHandleAnchor.BottomRight:
                {
                    AddToClassList("diagonal");
                    AddToClassList("bottom-right");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromBottomRight);
                    break;
                }
                case ResizeHandleAnchor.Bottom:
                {
                    AddToClassList("vertical");
                    AddToClassList("bottom");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromBottom);
                    break;
                }
                case ResizeHandleAnchor.BottomLeft:
                {
                    AddToClassList("diagonal");
                    AddToClassList("bottom-left");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromBottomLeft);
                    break;
                }
                case ResizeHandleAnchor.Left:
                {
                    AddToClassList("horizontal");
                    AddToClassList("left");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromLeft);
                    break;
                }
                case ResizeHandleAnchor.TopLeft:
                {
                    AddToClassList("diagonal");
                    AddToClassList("top-left");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromTopLeft);
                    break;
                }
            }

            RegisterCallback<MouseDownEvent>(HandleMouseDown);
            RegisterCallback<MouseUpEvent>(HandleDraggableMouseUp);

            m_ResizeTarget.RegisterCallback<GeometryChangedEvent>(InitialLayoutSetup);
        }

        void InitialLayoutSetup(GeometryChangedEvent evt)
        {
            m_ResizeTarget.UnregisterCallback<GeometryChangedEvent>(InitialLayoutSetup);
        }

        void HandleResizeFromTop(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
                return;

            Vector2 relativeMousePosition = mouseMoveEvent.mousePosition - m_ResizeBeginMousePosition;

            // Set anchor points for positioning
            m_Container.style.positionTop = StyleValue<float>.Create(float.NaN);
            m_Container.style.positionBottom = StyleValue<float>.Create(m_Container.parent.layout.height - m_Container.layout.yMax);

            float newHeight = Mathf.Max(0f, m_ResizeBeginLayout.height - relativeMousePosition.y);

            m_ResizeTarget.style.height = StyleValue<float>.Create(newHeight);

            if (maintainAspectRatio)
                m_ResizeTarget.style.width = StyleValue<float>.Create(newHeight);

            mouseMoveEvent.StopImmediatePropagation();
        }

        void HandleResizeFromTopRight(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
                return;

            Vector2 relativeMousePosition = mouseMoveEvent.mousePosition - m_ResizeBeginMousePosition;

            // Set anchor points for positioning
            m_Container.style.positionTop = StyleValue<float>.Create(float.NaN);
            m_Container.style.positionBottom = StyleValue<float>.Create(m_Container.parent.layout.height - m_Container.layout.yMax);
            m_Container.style.positionLeft = StyleValue<float>.Create(m_Container.layout.xMin);
            m_Container.style.positionRight = StyleValue<float>.Create(float.NaN);

            float newWidth = Mathf.Max(0f, m_ResizeBeginLayout.width + relativeMousePosition.x);
            float newHeight = Mathf.Max(0f, m_ResizeBeginLayout.height - relativeMousePosition.y);

            if (maintainAspectRatio)
                newWidth = newHeight = Mathf.Min(newWidth, newHeight);

            m_ResizeTarget.style.width = StyleValue<float>.Create(newWidth);
            m_ResizeTarget.style.height = StyleValue<float>.Create(newHeight);

            mouseMoveEvent.StopPropagation();
        }

        void HandleResizeFromRight(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
                return;

            Vector2 relativeMousePosition = mouseMoveEvent.mousePosition - m_ResizeBeginMousePosition;

            // Set anchor points for positioning
            m_Container.style.positionLeft = StyleValue<float>.Create(m_Container.layout.xMin);
            m_Container.style.positionRight = StyleValue<float>.Create(float.NaN);

            float newWidth = Mathf.Max(0f, m_ResizeBeginLayout.width + relativeMousePosition.x);

            m_ResizeTarget.style.width = StyleValue<float>.Create(newWidth);

            if (maintainAspectRatio)
            {
                m_ResizeTarget.style.height = StyleValue<float>.Create(newWidth);
            }

            mouseMoveEvent.StopPropagation();
        }

        void HandleResizeFromBottomRight(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
                return;

            Vector2 relativeMousePosition = mouseMoveEvent.mousePosition - m_ResizeBeginMousePosition;

            // Set anchor points for positioning
            m_Container.style.positionTop = StyleValue<float>.Create(m_Container.layout.yMin);
            m_Container.style.positionBottom = StyleValue<float>.Create(float.NaN);
            m_Container.style.positionLeft = StyleValue<float>.Create(m_Container.layout.xMin);
            m_Container.style.positionRight = StyleValue<float>.Create(float.NaN);

            float newWidth = Mathf.Max(0f, m_ResizeBeginLayout.width + relativeMousePosition.x);
            float newHeight = Mathf.Max(0f, m_ResizeBeginLayout.height + relativeMousePosition.y);

            if (maintainAspectRatio)
                newWidth = newHeight = Mathf.Min(newWidth, newHeight);

            m_ResizeTarget.style.width = StyleValue<float>.Create(newWidth);
            m_ResizeTarget.style.height = StyleValue<float>.Create(newHeight);

            mouseMoveEvent.StopPropagation();
        }

        void HandleResizeFromBottom(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
                return;

            Vector2 relativeMousePosition = mouseMoveEvent.mousePosition - m_ResizeBeginMousePosition;

            // Set anchor points for positioning
            m_Container.style.positionTop = StyleValue<float>.Create(m_Container.layout.yMin);
            m_Container.style.positionBottom = StyleValue<float>.Create(float.NaN);

            float newHeight = Mathf.Max(0f, m_ResizeBeginLayout.height + relativeMousePosition.y);

            m_ResizeTarget.style.height = StyleValue<float>.Create(newHeight);

            if (maintainAspectRatio)
                m_ResizeTarget.style.width = StyleValue<float>.Create(newHeight);

            mouseMoveEvent.StopPropagation();
        }

        void HandleResizeFromBottomLeft(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
                return;

            Vector2 relativeMousePosition = mouseMoveEvent.mousePosition - m_ResizeBeginMousePosition;

            // Set anchor points for positioning
            m_Container.style.positionTop = StyleValue<float>.Create(m_Container.layout.yMin);
            m_Container.style.positionBottom = StyleValue<float>.Create(float.NaN);
            m_Container.style.positionLeft = StyleValue<float>.Create(float.NaN);
            m_Container.style.positionRight = StyleValue<float>.Create(m_Container.parent.layout.width - m_Container.layout.xMax);

            float newWidth = Mathf.Max(0f, m_ResizeBeginLayout.width - relativeMousePosition.x);
            float newHeight = Mathf.Max(0f, m_ResizeBeginLayout.height + relativeMousePosition.y);

            if (maintainAspectRatio)
                newWidth = newHeight = Mathf.Min(newWidth, newHeight);

            m_ResizeTarget.style.width = StyleValue<float>.Create(newWidth);
            m_ResizeTarget.style.height = StyleValue<float>.Create(newHeight);

            mouseMoveEvent.StopPropagation();
        }

        void HandleResizeFromLeft(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
                return;

            Vector2 relativeMousePosition = mouseMoveEvent.mousePosition - m_ResizeBeginMousePosition;

            // Set anchor points for positioning
            m_Container.style.positionLeft = StyleValue<float>.Create(float.NaN);
            m_Container.style.positionRight = StyleValue<float>.Create(m_Container.parent.layout.width - m_Container.layout.xMax);

            float newWidth = Mathf.Max(0f, m_ResizeBeginLayout.width - relativeMousePosition.x);

            m_ResizeTarget.style.width = StyleValue<float>.Create(newWidth);

            if (maintainAspectRatio)
                m_ResizeTarget.style.height = StyleValue<float>.Create(newWidth);

            mouseMoveEvent.StopPropagation();
        }

        void HandleResizeFromTopLeft(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
                return;

            Vector2 relativeMousePosition = mouseMoveEvent.mousePosition - m_ResizeBeginMousePosition;

            // Set anchor points for positioning
            m_Container.style.positionTop = StyleValue<float>.Create(float.NaN);
            m_Container.style.positionBottom = StyleValue<float>.Create(m_Container.parent.layout.height - m_Container.layout.yMax);
            m_Container.style.positionLeft = StyleValue<float>.Create(float.NaN);
            m_Container.style.positionRight = StyleValue<float>.Create(m_Container.parent.layout.width - m_Container.layout.xMax);

            float newWidth = Mathf.Max(0f, m_ResizeBeginLayout.width - relativeMousePosition.x);
            float newHeight = Mathf.Max(0f, m_ResizeBeginLayout.height - relativeMousePosition.y);

            if (maintainAspectRatio)
                newWidth = newHeight = Mathf.Min(newWidth, newHeight);

            m_ResizeTarget.style.width = StyleValue<float>.Create(newWidth);
            m_ResizeTarget.style.height = StyleValue<float>.Create(newHeight);

            mouseMoveEvent.StopPropagation();
        }

        void HandleMouseDown(MouseDownEvent mouseDownEvent)
        {
            // Get the docking settings for the window, as well as the
            // layout and mouse position when resize begins.
            m_WindowDockingLayout.CalculateDockingCornerAndOffset(m_Container.layout, m_Container.parent.layout);
            m_WindowDockingLayout.ApplyPosition(m_Container);

            m_ResizeBeginLayout = m_ResizeTarget.layout;
            m_ResizeBeginMousePosition = mouseDownEvent.mousePosition;

            m_Dragging = true;
            this.CaptureMouse();
            mouseDownEvent.StopPropagation();
        }

        void HandleDraggableMouseUp(MouseUpEvent mouseUpEvent)
        {
            m_Dragging = false;

            if (this.HasMouseCapture())
                this.ReleaseMouse();

            if (OnResizeFinished != null)
                OnResizeFinished();

            m_WindowDockingLayout.CalculateDockingCornerAndOffset(m_Container.layout, m_Container.parent.layout);
            m_WindowDockingLayout.ApplyPosition(m_Container);
        }

#if UNITY_2018_3_OR_NEWER
        protected override void DoRepaint(IStylePainter painter)
#else
        public override void DoRepaint()
#endif
        {
            if (m_StyleWidget == null)
            {
                m_StyleWidget = new GUIStyle("WindowBottomResize") { fixedHeight = 0 };
                image = m_StyleWidget.normal.background;
            }

            if (image == null)
            {
                Debug.LogWarning("null texture passed to GUI.DrawTexture");
                return;
            }

            GUI.DrawTexture(contentRect, image, ScaleMode.ScaleAndCrop, true, 0, GUI.color, 0, 0);
        }
    }
}
