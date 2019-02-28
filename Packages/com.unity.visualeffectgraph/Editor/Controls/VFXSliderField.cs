using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using System.Collections.Generic;

namespace UnityEditor.VFX.UIElements
{
    abstract class VFXBaseSliderField<T> : VisualElement, INotifyValueChanged<T>
    {
        protected Slider m_Slider;
        protected INotifyValueChanged<T> m_Field;

        public VFXBaseSliderField()
        {
            AddToClassList("sliderField");
        }

        protected void RegisterCallBack()
        {
            (m_Field as VisualElement).RegisterCallback<BlurEvent>(OnFocusLost);
        }

        void OnFocusLost(BlurEvent e)
        {
            //forward the focus lost event
            using (BlurEvent newE = BlurEvent.GetPooled(this, e.relatedTarget, e.direction, panel.focusController))
            {
                SendEvent(newE);
            }

            e.StopPropagation();
        }

        public T m_Value;

        public T value
        {
            get
            {
                return m_Value;
            }

            set
            {
                SetValueAndNotify(value);
            }
        }

        protected abstract float ValueToFloat(T value);


        private Vector2 m_Range;

        public Vector2 range
        {
            get
            {
                return m_Range;
            }
            set
            {
                m_Range = value;
                m_IgnoreNotification = true;
                if (m_Slider.lowValue != m_Range.x || m_Slider.highValue != m_Range.y)
                {
                    m_Slider.lowValue = m_Range.x;
                    m_Slider.highValue = m_Range.y;
                    m_Slider.pageSize = (m_Slider.highValue - m_Slider.lowValue) * 0.1f;

                    //TODO ask fix in Slider

                    m_Slider.value = m_Range.x;
                    m_Slider.value = m_Range.y;
                    m_Slider.value = ValueToFloat(this.value);
                }
                m_IgnoreNotification = false;
            }
        }

        protected bool m_IgnoreNotification;

        public abstract bool HasFocus();
        public void OnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            RegisterCallback(callback);
        }

        public void RemoveOnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            UnregisterCallback(callback);
        }

        public void SetValueAndNotify(T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(value, newValue))
            {
                using (ChangeEvent<T> evt = ChangeEvent<T>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    SetValueWithoutNotify(newValue);
                    SendEvent(evt);
                }
            }
        }

        public void SetValueWithoutNotify(T newValue)
        {
            m_IgnoreNotification = true;
            m_Value = newValue;
            if (!(m_Field as VisualElement).HasFocus())
                m_Field.value = newValue;
            m_Slider.value = ValueToFloat(value);
            m_IgnoreNotification = false;
        }

        protected void ValueChanged(ChangeEvent<T> e)
        {
            e.StopPropagation();
            if (!m_IgnoreNotification)
                SetValueAndNotify(e.newValue);
        }
    }
    class VFXFloatSliderField : VFXBaseSliderField<float>
    {
        public VFXFloatSliderField()
        {
            m_Slider = new Slider(0, 1, ValueChanged, SliderDirection.Horizontal, (range.y - range.x) * 0.1f);
            m_Slider.AddToClassList("textfield");
            m_Slider.valueChanged += ValueChanged;

            m_FloatField = new FloatField();
            m_FloatField.RegisterCallback<ChangeEvent<float>>(ValueChanged);
            m_FloatField.name = "Field";
            m_Field = m_FloatField;

            m_IndeterminateLabel = new Label()
            {
                name = "indeterminate",
                text = VFXControlConstants.indeterminateText
            };
            m_IndeterminateLabel.SetEnabled(false);

            Add(m_Slider);
            Add(m_FloatField);
            RegisterCallBack();
        }

        VisualElement m_IndeterminateLabel;
        FloatField m_FloatField;

        public override bool HasFocus()
        {
            return (m_Field as FloatField).HasFocus();
        }

        protected override float ValueToFloat(float value)
        {
            return value;
        }

        void ValueChanged(float newValue)
        {
            if (!m_IgnoreNotification)
                SetValueAndNotify(newValue);
        }

        public bool indeterminate
        {
            get {return m_FloatField.parent == null; }

            set
            {
                if (indeterminate != value)
                {
                    if (value)
                    {
                        m_FloatField.RemoveFromHierarchy();
                        Add(m_IndeterminateLabel);
                    }
                    else
                    {
                        m_IndeterminateLabel.RemoveFromHierarchy();
                        Add(m_FloatField);
                    }
                    m_Slider.SetEnabled(!value);
                }
            }
        }
    }
    class VFXIntSliderField : VFXBaseSliderField<int>
    {
        public VFXIntSliderField()
        {
            m_Slider = new Slider(0, 1, ValueChanged, SliderDirection.Horizontal, 0.1f);
            m_Slider.AddToClassList("textfield");
            m_Slider.valueChanged += ValueChanged;

            var integerField = new IntegerField();
            integerField.RegisterCallback<ChangeEvent<int>>(ValueChanged);
            integerField.name = "Field";
            m_Field = integerField;

            Add(m_Slider);
            Add(integerField);
            RegisterCallBack();
        }

        public override bool HasFocus()
        {
            return (m_Field as IntegerField).HasFocus();
        }

        protected override float ValueToFloat(int value)
        {
            return (float)value;
        }

        void ValueChanged(float newValue)
        {
            SetValueAndNotify((int)newValue);
        }
    }
    class VFXLongSliderField : VFXBaseSliderField<long>
    {
        public VFXLongSliderField()
        {
            m_Slider = new Slider(0, 1, ValueChanged, SliderDirection.Horizontal, 0.1f);
            m_Slider.AddToClassList("textfield");
            m_Slider.valueChanged += ValueChanged;

            var integerField = new LongField();
            integerField.RegisterCallback<ChangeEvent<long>>(ValueChanged);
            integerField.name = "Field";
            m_Field = integerField;

            Add(m_Slider);
            Add(integerField);
            RegisterCallBack();
        }

        public override bool HasFocus()
        {
            return (m_Field as LongField).HasFocus();
        }

        protected override float ValueToFloat(long value)
        {
            return (float)value;
        }

        void ValueChanged(float newValue)
        {
            SetValueAndNotify((long)newValue);
        }
    }
}
