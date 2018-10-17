using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    abstract class HDProbeEditor : Editor
    {
        static Dictionary<HDProbe, HDProbeUI> s_StateMap = new Dictionary<HDProbe, HDProbeUI>();

        internal static bool TryGetUIStateFor(HDProbe p, out HDProbeUI r)
        {
            return s_StateMap.TryGetValue(p, out r);
        }

        internal abstract HDProbe GetTarget(Object editorTarget);

        protected SerializedHDProbe m_SerializedHDProbe;
        internal HDProbeUI m_UIState;
        HDProbeUI[] m_UIHandleState;
        protected HDProbe[] m_TypedTargets;

        protected virtual void OnEnable()
        {
            if(m_UIState == null)
            {
                m_UIState = HDProbeUI.CreateFor(this);
            }
            m_UIState.Reset(m_SerializedHDProbe, Repaint);

            m_TypedTargets = new HDProbe[targets.Length];
            m_UIHandleState = new HDProbeUI[m_TypedTargets.Length];
            for (var i = 0; i < m_TypedTargets.Length; i++)
            {
                m_TypedTargets[i] = GetTarget(targets[i]);
                m_UIHandleState[i] = HDProbeUI.CreateFor(m_TypedTargets[i]);
                m_UIHandleState[i].Reset(m_SerializedHDProbe, null);

                s_StateMap[m_TypedTargets[i]] = m_UIHandleState[i];
            }
        }

        protected virtual void OnDisable()
        {
            for (var i = 0; i < m_TypedTargets.Length; i++)
                s_StateMap.Remove(m_TypedTargets[i]);
        }

        protected abstract void Draw(HDProbeUI s, SerializedHDProbe serialized, Editor owner);

        public override void OnInspectorGUI()
        {
            var s = m_UIState;
            var d = m_SerializedHDProbe;
            var o = this;

            s.Update();
            d.Update();

            Draw(s, d, o);

            d.Apply();
        }

        protected virtual void OnSceneGUI()
        {
            //mandatory update as for strange reason the serialized rollback one update here
            m_UIState.Update();
            m_SerializedHDProbe.Update();

            HDProbeUI.DrawHandles(m_UIState, m_SerializedHDProbe, this);
            m_UIState.DoShortcutKey(this);
        }
    }
}
