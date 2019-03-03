using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.VFX.Utils
{
    [VFXBinder("Utility/Velocity")]
    public class VFXVelocityBinder : VFXBinderBase
    {
        public string Parameter { get { return (string)m_Parameter; } set { m_Parameter = value; } }

        [VFXParameterBinding("UnityEngine.Vector3"), SerializeField]
        public ExposedParameter m_Parameter = "Velocity";
        public Transform Target;

        private float m_PreviousTime = -1.0f;
        private Vector3 m_PreviousPosition = Vector3.zero;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3((int)m_Parameter);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            Vector3 velocity = Vector3.zero;
            float time;
#if UNITY_EDITOR
            if (Application.isEditor && !Application.isPlaying)
                time = (float)UnityEditor.EditorApplication.timeSinceStartup;
            else
#endif
            time = Time.time;

            if (m_PreviousTime != -1.0f)
            {
                var delta = Target.transform.position - m_PreviousPosition;

                if (Vector3.Magnitude(delta) > float.Epsilon)
                    velocity = delta / (time - m_PreviousTime);
            }

            component.SetVector3((int)m_Parameter, velocity);
            m_PreviousPosition = Target.transform.position;
            m_PreviousTime = time;
        }

        public override string ToString()
        {
            return string.Format("Velocity : '{0}' -> {1}", m_Parameter, Target == null ? "(null)" : Target.name);
        }
    }
}
