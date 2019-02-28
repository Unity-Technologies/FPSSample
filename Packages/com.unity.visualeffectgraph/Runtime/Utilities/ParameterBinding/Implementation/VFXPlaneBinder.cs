using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.VFX.Utils
{
    [VFXBinder("Utility/Plane")]
    public class VFXPlaneBinder : VFXBinderBase
    {
        public string Parameter { get { return (string)m_Parameter; } set { m_Parameter = value; UpdateSubParameters(); } }

        [VFXParameterBinding("UnityEditor.VFX.Plane"), SerializeField]
        protected ExposedParameter m_Parameter = "Plane";
        public Transform Target;

        private ExposedParameter Position;
        private ExposedParameter Normal;

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
            Position = m_Parameter + "_position";
            Normal = m_Parameter + "_normal";
        }

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3(Position) && component.HasVector3(Normal);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetVector3(Position, Target.transform.position);
            component.SetVector3(Normal, Target.transform.up);
        }

        public override string ToString()
        {
            return string.Format("Plane : '{0}' -> {1}", m_Parameter, Target == null ? "(null)" : Target.name);
        }
    }
}
