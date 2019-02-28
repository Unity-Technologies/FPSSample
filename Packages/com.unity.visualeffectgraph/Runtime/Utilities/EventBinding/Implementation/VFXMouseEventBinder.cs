using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.VFX.Utils
{
    [RequireComponent(typeof(Collider))]
    public class VFXMouseEventBinder : VFXEventBinderBase
    {
        public enum Activation
        {
            OnMouseUp,
            OnMouseDown,
            OnMouseEnter,
            OnMouseExit,
            OnMouseOver,
            OnMouseDrag
        }

        public Activation activation = Activation.OnMouseDown;

        private ExposedParameter position = "position";

        [Tooltip("Computes intersection in world space and sets it to the position EventAttribute")]
        public bool RaycastMousePosition = false;

        protected override void SetEventAttribute(object[] parameters)
        {
            if (RaycastMousePosition)
            {
                Camera c = Camera.main;
                RaycastHit hit;
                Ray r = c.ScreenPointToRay(Input.mousePosition);
                if (GetComponent<Collider>().Raycast(r, out hit, float.MaxValue))
                {
                    eventAttribute.SetVector3(position, hit.point);
                }
            }
        }

        private void OnMouseDown()
        {
            if (activation == Activation.OnMouseDown) SendEventToVisualEffect();
        }

        private void OnMouseUp()
        {
            if (activation == Activation.OnMouseUp) SendEventToVisualEffect();
        }

        private void OnMouseDrag()
        {
            if (activation == Activation.OnMouseDrag) SendEventToVisualEffect();
        }

        private void OnMouseOver()
        {
            if (activation == Activation.OnMouseOver) SendEventToVisualEffect();
        }

        private void OnMouseEnter()
        {
            if (activation == Activation.OnMouseEnter) SendEventToVisualEffect();
        }

        private void OnMouseExit()
        {
            if (activation == Activation.OnMouseExit) SendEventToVisualEffect();
        }
    }
}
