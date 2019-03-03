using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.VFX.Utils
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class VFXRigidBodyCollisionEventBinder : VFXEventBinderBase
    {
        private ExposedParameter positionParameter = "position";
        private ExposedParameter directionParameter = "velocity";


        protected override void SetEventAttribute(object[] parameters)
        {
            ContactPoint contact = (ContactPoint)parameters[0];
            eventAttribute.SetVector3(positionParameter, contact.point);
            eventAttribute.SetVector3(directionParameter, contact.normal);
        }

        void OnCollisionEnter(Collision collision)
        {
            // Debug-draw all contact points and normals
            foreach (ContactPoint contact in collision.contacts)
            {
                SendEventToVisualEffect(contact);
            }
        }
    }
}
