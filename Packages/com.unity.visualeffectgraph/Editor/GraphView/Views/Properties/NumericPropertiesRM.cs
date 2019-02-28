using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEditor.VFX.UIElements;

namespace UnityEditor.VFX.UI
{
    abstract class NumericPropertyRM<T, U> : SimpleUIPropertyRM<T, U>
    {
        public NumericPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            Vector2 range = VFXPropertyAttribute.FindRange(m_Provider.attributes);
            if (RangeShouldCreateSlider(range))
            {
                return 120;
            }
            return 60;
        }

        protected virtual bool RangeShouldCreateSlider(Vector2 range)
        {
            return range != Vector2.zero && range.y != Mathf.Infinity;
        }

        protected VFXBaseSliderField<U> m_Slider;
        protected TextValueField<U> m_TextField;

        protected abstract INotifyValueChanged<U> CreateSimpleField(out TextValueField<U> textField);
        protected abstract INotifyValueChanged<U> CreateSliderField(out VFXBaseSliderField<U> slider);

        public override INotifyValueChanged<U> CreateField()
        {
            Vector2 range = VFXPropertyAttribute.FindRange(m_Provider.attributes);
            INotifyValueChanged<U> result;
            if (!RangeShouldCreateSlider(range))
            {
                result = CreateSimpleField(out m_TextField);
                m_TextField.RegisterCallback<KeyDownEvent>(OnKeyDown);
                m_TextField.RegisterCallback<BlurEvent>(OnFocusLost);
            }
            else
            {
                result = CreateSliderField(out m_Slider);
                m_Slider.RegisterCallback<BlurEvent>(OnFocusLost);
                m_Slider.range = range;
            }
            return result;
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.character == '\n')
            {
                DelayedNotifyValueChange();
                UpdateGUI(true);
            }
        }

        void OnFocusLost(BlurEvent e)
        {
            DelayedNotifyValueChange();
            UpdateGUI(true);
        }

        protected void DelayedNotifyValueChange()
        {
            if (isDelayed && hasChangeDelayed)
            {
                hasChangeDelayed = false;
                NotifyValueChanged();
            }
        }

        protected override bool HasFocus()
        {
            if (m_Slider != null)
                return m_Slider.HasFocus();
            return m_TextField.HasFocus();
        }

        public override bool IsCompatible(IPropertyRMProvider provider)
        {
            if (!base.IsCompatible(provider)) return false;

            Vector2 range = VFXPropertyAttribute.FindRange(m_Provider.attributes);

            return RangeShouldCreateSlider(range) != (m_Slider == null);
        }

        public override void UpdateGUI(bool force)
        {
            if (m_Slider != null)
            {
                Vector2 range = VFXPropertyAttribute.FindRange(m_Provider.attributes);

                m_Slider.range = range;
            }
            base.UpdateGUI(force);
        }

        public abstract T FilterValue(Vector2 range, T value);
        public override object FilterValue(object value)
        {
            Vector2 range = VFXPropertyAttribute.FindRange(m_Provider.attributes);

            if (range != Vector2.zero)
            {
                value = FilterValue(range, (T)value);
            }

            return value;
        }
    }
    abstract class IntegerPropertyRM<T, U> : NumericPropertyRM<T, U>
    {
        VisualElement m_IndeterminateLabel;
        public IntegerPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_IndeterminateLabel = new Label()
            {
                name = "indeterminate",
                text = VFXControlConstants.indeterminateText
            };
            m_IndeterminateLabel.SetEnabled(false);
        }

        protected override void UpdateIndeterminate()
        {
            VisualElement field = this.field as VisualElement;
            if (indeterminate)
            {
                if (m_IndeterminateLabel.parent == null)
                {
                    field.RemoveFromHierarchy();
                    Add(m_IndeterminateLabel);
                }
            }
            else
            {
                if (field.parent == null)
                {
                    m_IndeterminateLabel.RemoveFromHierarchy();
                    Add(field);
                }
            }
        }
    }

    class UintPropertyRM : IntegerPropertyRM<uint, long>
    {
        public UintPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override bool RangeShouldCreateSlider(Vector2 range)
        {
            return base.RangeShouldCreateSlider(range) && (uint)range.x < (uint)range.y;
        }

        protected override INotifyValueChanged<long> CreateSimpleField(out TextValueField<long> textField)
        {
            var field =  new VFXLabeledField<LongField, long>(m_Label);

            field.onValueDragFinished = t => DelayedNotifyValueChange();
            textField = field.control;
            return field;
        }

        protected override INotifyValueChanged<long> CreateSliderField(out VFXBaseSliderField<long> slider)
        {
            var field = new VFXLabeledField<VFXLongSliderField, long>(m_Label);
            slider = field.control;
            return field;
        }

        public override object FilterValue(object value)
        {
            if ((uint)value < 0)
            {
                value = (uint)0;
            }
            return base.FilterValue(value);
        }

        public override uint Convert(object value)
        {
            long longValue = (long)value;

            if (longValue < 0)
            {
                longValue = 0;
            }

            return (uint)longValue;
        }

        public override uint FilterValue(Vector2 range, uint value)
        {
            uint val = value;
            if (range.x > val)
            {
                val = (uint)range.x;
            }
            if (range.y < val)
            {
                val = (uint)range.y;
            }

            return val;
        }
    }

    class IntPropertyRM : IntegerPropertyRM<int, int>
    {
        public IntPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override bool RangeShouldCreateSlider(Vector2 range)
        {
            return base.RangeShouldCreateSlider(range) && (int)range.x < (int)range.y;
        }

        protected override INotifyValueChanged<int> CreateSimpleField(out TextValueField<int> textField)
        {
            var field = new VFXLabeledField<IntegerField, int>(m_Label);
            textField = field.control;
            field.onValueDragFinished = t => DelayedNotifyValueChange();
            return field;
        }

        protected override INotifyValueChanged<int> CreateSliderField(out VFXBaseSliderField<int> slider)
        {
            var field = new VFXLabeledField<VFXIntSliderField, int>(m_Label);
            slider = field.control;
            return field;
        }

        public override int FilterValue(Vector2 range, int value)
        {
            int val = value;
            if (range.x > val)
            {
                val = (int)range.x;
            }
            if (range.y < val)
            {
                val = (int)range.y;
            }

            return val;
        }
    }

    class FloatPropertyRM : NumericPropertyRM<float, float>
    {
        public FloatPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override bool RangeShouldCreateSlider(Vector2 range)
        {
            return base.RangeShouldCreateSlider(range) && range.x < range.y;
        }

        protected override INotifyValueChanged<float> CreateSimpleField(out TextValueField<float> textField)
        {
            var field = new VFXLabeledField<FloatField, float>(m_Label);
            field.onValueDragFinished = t => DelayedNotifyValueChange();
            textField = field.control;
            return field;
        }

        protected override INotifyValueChanged<float> CreateSliderField(out VFXBaseSliderField<float> slider)
        {
            var field = new VFXLabeledField<VFXFloatSliderField, float>(m_Label);
            slider = field.control;
            return field;
        }

        protected override void UpdateIndeterminate()
        {
            if (m_TextField != null)
            {
                (field as VFXLabeledField<FloatField, float>).indeterminate = indeterminate;
            }

            if (m_Slider != null)
                (m_Slider as VFXFloatSliderField).indeterminate = indeterminate;
        }

        public override float FilterValue(Vector2 range, float value)
        {
            float val = value;
            if (range.x > val)
            {
                val = range.x;
            }
            if (range.y < val)
            {
                val = range.y;
            }

            return val;
        }
    }
}
