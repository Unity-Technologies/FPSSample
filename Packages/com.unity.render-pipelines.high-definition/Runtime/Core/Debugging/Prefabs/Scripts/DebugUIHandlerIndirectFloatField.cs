using System;
using UnityEngine.UI;

namespace UnityEngine.Experimental.Rendering.UI
{
    public class DebugUIHandlerIndirectFloatField : DebugUIHandlerWidget
    {
        public Text nameLabel;
        public Text valueLabel;

        public Func<float> getter;
        public Action<float> setter;

        public Func<float> incStepGetter;
        public Func<float> incStepMultGetter;
        public Func<float> decimalsGetter;

        public void Init()
        {
            UpdateValueLabel();
        }

        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            nameLabel.color = colorSelected;
            valueLabel.color = colorSelected;
            return true;
        }

        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
            valueLabel.color = colorDefault;
        }

        public override void OnIncrement(bool fast)
        {
            ChangeValue(fast, 1);
        }

        public override void OnDecrement(bool fast)
        {
            ChangeValue(fast, -1);
        }

        void ChangeValue(bool fast, float multiplier)
        {
            float value = getter();
            value += incStepGetter() * (fast ? incStepMultGetter() : 1f) * multiplier;
            setter(value);
            UpdateValueLabel();
        }

        void UpdateValueLabel()
        {
            if (valueLabel != null)
                valueLabel.text = getter().ToString("N" + decimalsGetter());
        }
    }
}
