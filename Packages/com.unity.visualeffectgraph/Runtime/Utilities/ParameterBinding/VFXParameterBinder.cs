using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.VFX.Utils
{
    [RequireComponent(typeof(VisualEffect))]
    [DefaultExecutionOrder(1)]
    [ExecuteInEditMode]
    public class VFXParameterBinder : MonoBehaviour
    {
        [SerializeField]
        protected bool m_ExecuteInEditor = true;
        public List<VFXBinderBase> m_Bindings = new List<VFXBinderBase>();
        [SerializeField]
        protected VisualEffect m_VisualEffect;

        private void OnEnable()
        {
            m_VisualEffect = GetComponent<VisualEffect>();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            foreach (var binding in m_Bindings)
                UnityEditor.Undo.DestroyObjectImmediate(binding);
#endif
        }

        void Update()
        {
            if (!m_ExecuteInEditor && Application.isEditor && !Application.isPlaying) return;

            foreach (var binding in m_Bindings)
                if (binding.IsValid(m_VisualEffect)) binding.UpdateBinding(m_VisualEffect);
        }

        public T AddParameterBinder<T>() where T : VFXBinderBase
        {
            return gameObject.AddComponent<T>();
        }

        public void ClearParameterBinders()
        {
            var allBinders = GetComponents<VFXBinderBase>();
            foreach (var binder in allBinders) Destroy(binder);
        }

        public void RemoveParameterBinder(VFXBinderBase binder)
        {
            if (binder.gameObject == this.gameObject) Destroy(binder);
        }

        public void RemoveParameterBinders<T>() where T : VFXBinderBase
        {
            var allBinders = GetComponents<VFXBinderBase>();
            foreach (var binder in allBinders)
                if (binder is T) Destroy(binder);
        }

        public IEnumerable<T> GetParameterBinders<T>() where T : VFXBinderBase
        {
            foreach (var binding in m_Bindings)
            {
                if (binding is T) yield return binding as T;
            }
        }
    }
}
