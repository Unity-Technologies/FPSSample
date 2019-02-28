using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.VFX.Utils
{
    [VFXBinder("Transform/Position")]
    public class VFXPositionBinder : VFXBinderBase
    {
        public string Parameter { get { return (string)m_Parameter; } set { m_Parameter = value; } }

        [VFXParameterBinding("UnityEditor.VFX.Position", "UnityEngine.Vector3"), SerializeField]
        protected ExposedParameter m_Parameter = "Position";
        public Transform Target;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3(m_Parameter);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetVector3(m_Parameter, Target.transform.position);
        }

        public override string ToString()
        {
            return string.Format("Position : '{0}' -> {1}", m_Parameter, Target == null ? "(null)" : Target.name);
        }
    }
}
