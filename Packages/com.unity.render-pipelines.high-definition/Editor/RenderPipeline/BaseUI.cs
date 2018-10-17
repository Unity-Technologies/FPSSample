using UnityEditor.AnimatedValues;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class BaseUI<TType>
    {
        protected AnimBool[] m_AnimBools = null;
        protected TType data { get; private set; }

        public BaseUI(int animBoolCount)
        {
            m_AnimBools = new AnimBool[animBoolCount];
            for (var i = 0; i < m_AnimBools.Length; ++i)
                m_AnimBools[i] = new AnimBool();
        }

        public virtual void Reset(TType data, UnityAction repaint)
        {
            this.data = data;
            for (var i = 0; i < m_AnimBools.Length; ++i)
            {
                m_AnimBools[i].valueChanged.RemoveAllListeners();
                if (repaint != null)
                    m_AnimBools[i].valueChanged.AddListener(repaint);
            }

            Update();
        }

        public virtual void Update()
        {
        }
    }
}
