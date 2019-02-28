using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.VFX.Utils
{
    [VFXBinder("Collider/Sphere")]
    public class VFXSphereBinder : VFXBinderBase
    {
        public string Parameter { get { return (string)m_Parameter; } set { m_Parameter = value; UpdateSubParameters(); } }

        [VFXParameterBinding("UnityEditor.VFX.Sphere"), SerializeField]
        protected ExposedParameter m_Parameter = "Sphere";
        public SphereCollider Target;

        private ExposedParameter Center;
        private ExposedParameter Radius;

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateSubParameters();
        }

        void OnValidate()
        {
            UpdateSubParameters();
        }

        void UpdateSubParameters()
        {
            Center = m_Parameter + "_center";
            Radius = m_Parameter + "_radius";
        }

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3(Center) && component.HasFloat(Radius);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetVector3(Center, Target.transform.position + Target.center);
            component.SetFloat(Radius, Target.radius * GetSphereColliderScale(Target.transform.localScale));
        }

        public float GetSphereColliderScale(Vector3 scale)
        {
            return Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
        }

        public override string ToString()
        {
            return string.Format("Sphere : '{0}' -> {1}", m_Parameter, Target == null ? "(null)" : Target.name);
        }
    }
}
