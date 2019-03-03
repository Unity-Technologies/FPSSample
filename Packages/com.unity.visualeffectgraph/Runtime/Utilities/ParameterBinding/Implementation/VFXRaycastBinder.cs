using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.VFX.Utils
{
    [VFXBinder("Utility/Raycast")]
    public class VFXRaycastBinder : VFXBinderBase
    {
        public string TargetPosition { get { return (string)m_TargetPosition; } set { m_TargetPosition = value; UpdateSubParameters(); } }
        public string TargetNormal { get { return (string)m_TargetNormal; } set { m_TargetNormal = value; UpdateSubParameters(); } }
        public string TargetHit { get { return (string)m_TargetHit; } set { m_TargetHit = value; } }

        [VFXParameterBinding("UnityEditor.VFX.Position"), SerializeField]
        protected ExposedParameter m_TargetPosition = "TargetPosition";

        [VFXParameterBinding("UnityEditor.VFX.DirectionType"), SerializeField]
        protected ExposedParameter m_TargetNormal = "TargetNormal";

        [VFXParameterBinding("System.Boolean"), SerializeField]
        protected ExposedParameter m_TargetHit = "TargetHit";


        protected ExposedParameter m_TargetPosition_position;
        protected ExposedParameter m_TargetNormal_direction;

        public enum Space
        {
            Local,
            World
        }

        public GameObject RaycastSource;
        public Vector3 RaycastDirection;
        public Space RaycastDirectionSpace = Space.Local;
        public LayerMask Layers = -1;
        public float MaxDistance = 100.0f;
        private RaycastHit m_HitInfo;


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
            m_TargetPosition_position = m_TargetPosition + "_position";
            m_TargetNormal_direction = m_TargetNormal + "_direction";
        }

        public override bool IsValid(VisualEffect component)
        {
            return component.HasVector3(m_TargetPosition_position) && component.HasVector3(m_TargetNormal_direction) && component.HasBool(m_TargetHit) && RaycastSource != null;
        }

        public override void UpdateBinding(VisualEffect component)
        {
            Vector3 direction = RaycastDirectionSpace == Space.Local ? RaycastSource.transform.TransformDirection(RaycastDirection) : RaycastDirection;
            Ray ray = new Ray(RaycastSource.transform.position, direction);

            bool hasHit = Physics.Raycast(ray, out m_HitInfo, MaxDistance, Layers);


            component.SetVector3(m_TargetPosition_position, m_HitInfo.point);
            component.SetVector3(m_TargetNormal_direction, m_HitInfo.normal);
            component.SetBool(TargetHit, hasHit);
        }

        public override string ToString()
        {
            return string.Format(string.Format("Raycast : {0} -> {1} ({2})", RaycastSource == null ? "null" : RaycastSource.name, RaycastDirection, RaycastDirectionSpace));
        }
    }
}
