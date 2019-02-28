using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

using FloatField = UnityEditor.VFX.UIElements.VFXLabeledField<UnityEditor.Experimental.UIElements.FloatField, float>;
namespace UnityEditor.VFX.UIElements
{
    abstract class VFXVectorNField<T> : VFXControl<T>
    {
        FloatField[] m_Fields;

        protected abstract int componentCount {get; }
        public virtual string GetComponentName(int i)
        {
            switch (i)
            {
                case 0:
                    return "x";
                case 1:
                    return "y";
                case 2:
                    return "z";
                case 3:
                    return "w";
                default:
                    return "a";
            }
        }

        void CreateTextField()
        {
            m_Fields = new FloatField[componentCount];


            for (int i = 0; i < m_Fields.Length; ++i)
            {
                m_Fields[i] = new FloatField(GetComponentName(i));
                m_Fields[i].control.AddToClassList("fieldContainer");
                m_Fields[i].AddToClassList("fieldContainer");
                m_Fields[i].RegisterCallback<ChangeEvent<float>, int>(OnValueChanged, i);
            }

            m_Fields[0].label.AddToClassList("first");
        }

        public override bool indeterminate
        {
            get
            {
                return m_Fields[0].indeterminate;
            }
            set
            {
                foreach (var field in m_Fields)
                {
                    field.indeterminate = value;
                }
            }
        }

        protected abstract void SetValueComponent(ref T value, int i, float componentValue);
        protected abstract float GetValueComponent(ref T value, int i);

        void OnValueChanged(ChangeEvent<float> e, int component)
        {
            T newValue = value;
            SetValueComponent(ref newValue, component, m_Fields[component].value);
            SetValueAndNotify(newValue);
        }

        public VFXVectorNField()
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Row;

            foreach (var field in m_Fields)
            {
                Add(field);
            }
        }

        protected override void ValueToGUI(bool force)
        {
            T value = this.value;
            for (int i = 0; i < m_Fields.Length; ++i)
            {
                if (!m_Fields[i].control.HasFocus() || force)
                {
                    m_Fields[i].SetValueWithoutNotify(GetValueComponent(ref value, i));
                }
            }
        }
    }
    class VFXVector3Field : VFXVectorNField<Vector3>
    {
        protected override  int componentCount {get {return 3; }}
        protected override void SetValueComponent(ref Vector3 value, int i, float componentValue)
        {
            switch (i)
            {
                case 0:
                    value.x = componentValue;
                    break;
                case 1:
                    value.y = componentValue;
                    break;
                default:
                    value.z = componentValue;
                    break;
            }
        }

        protected override float GetValueComponent(ref Vector3 value, int i)
        {
            switch (i)
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                default:
                    return value.z;
            }
        }
    }
}
