using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.VFX.Utils
{
    [VFXBinder("Transform/Transform")]
    public class VFXTransformBinder : VFXBinderBase
    {
        public string Parameter { get { return (string)m_Parameter; } set { m_Parameter = value; UpdateSubParameters(); } }

        [VFXParameterBinding("UnityEditor.VFX.Transform"), SerializeField]
        protected ExposedParameter m_Parameter = "Transform";
        public Transform Target;

        private ExposedParameter Position;
        private ExposedParameter Angles;
        private ExposedParameter Scale;
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
            Angles = m_Parameter + "_angles";
            Scale = m_Parameter + "_scale";
        }

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3((int)Position) && component.HasVector3((int)Angles) && component.HasVector3((int)Scale);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetVector3((int)Position, Target.transform.position);
            component.SetVector3((int)Angles, Target.transform.eulerAngles);
            component.SetVector3((int)Scale, Target.transform.localScale);
        }

        public override string ToString()
        {
            return string.Format("Transform : '{0}' -> {1}", m_Parameter, Target == null ? "(null)" : Target.name);
        }
    }
}
