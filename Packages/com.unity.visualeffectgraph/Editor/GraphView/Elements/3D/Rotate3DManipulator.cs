#if false
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor;

namespace UnityEditor.VFX.UI
{
    class Rotate3DManipulator : Manipulator
    {
        public Rotate3DManipulator(Element3D element3D)
        {
            m_Element3D = element3D;
        }

        Element3D m_Element3D;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseUpEvent>(OnMouseUp, Capture.Capture);
            target.RegisterCallback<MouseDownEvent>(OnMouseDown, Capture.Capture);
            //target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            //target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void Release()
        {
            if (m_Dragging)
            {
                m_Dragging = false;
                if (target.HasMouseCapture())
                    target.ReleaseMouseCapture();
                EditorGUIUtility.SetWantsMouseJumping(0);

                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            }
        }

        bool m_Dragging;

        void OnMouseDown(MouseDownEvent e)
        {
            m_Dragging = true;
            EditorGUIUtility.SetWantsMouseJumping(1);
            target.TakeMouseCapture();
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove, Capture.Capture);
            m_Dragging = true;
            e.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent e)
        {
            Release();
            e.StopPropagation();
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (m_Dragging)
            {
                if (!target.HasMouseCapture())
                {
                    Release();
                    return;
                }
                Quaternion rotation = m_Element3D.rotation;
                rotation = Quaternion.AngleAxis(e.mouseDelta.y * .003f * Mathf.Rad2Deg, rotation * Vector3.right) * rotation;
                rotation = Quaternion.AngleAxis(e.mouseDelta.x * .003f * Mathf.Rad2Deg, Vector3.up) * rotation;

                m_Element3D.rotation = rotation;
                e.StopPropagation();
            }
        }
    }
}

#endif
