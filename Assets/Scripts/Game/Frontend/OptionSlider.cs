using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class OptionSlider : OptionUI
{
    public Text title;
    public Slider slider;

    public override void UpdateFromConfigVar()
    {
        slider.value = configVar.FloatValue;
    }

    public override void UpdateToConfigVar()
    {
        configVar.Value = slider.value.ToString(CultureInfo.InvariantCulture);
    }
}
