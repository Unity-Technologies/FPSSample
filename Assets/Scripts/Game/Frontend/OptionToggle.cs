using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionToggle : OptionUI
{
    public Text title;
    public Toggle toggle;

    public override void UpdateFromConfigVar()
    {
        toggle.isOn = configVar.IntValue != 0;
    }

    public override void UpdateToConfigVar()
    {
        configVar.Value = toggle.isOn ? "1" : "0";
    }
}
