using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.VFX.Utils
{
    [RequireComponent(typeof(Collider))]
    public class VFXTriggerEventBinder : VFXEventBinderBase
    {
        public enum Activation
        {
            OnEnter,
            OnExit,
            OnStay
        }

        public List<Collider> colliders = new List<Collider>();

        public Activation activation = Activation.OnEnter;

        private ExposedParameter positionParameter = "position";

        protected override void SetEventAttribute(object[] parameters)
        {
            Collider collider = (Collider)parameters[0];
            eventAttribute.SetVector3(positionParameter, collider.transform.position);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (activation != Activation.OnEnter) return;
            if (!colliders.Contains(other)) return;

            SendEventToVisualEffect(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (activation != Activation.OnExit) return;
            if (!colliders.Contains(other)) return;

            SendEventToVisualEffect(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (activation != Activation.OnStay) return;
            if (!colliders.Contains(other)) return;

            SendEventToVisualEffect(other);
        }
    }
}
