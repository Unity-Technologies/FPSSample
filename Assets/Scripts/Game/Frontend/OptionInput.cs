using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionInput : OptionUI
{
    public TMPro.TextMeshProUGUI title;
    public TMPro.TMP_InputField input;

    public override void UpdateFromConfigVar()
    {
        if(!input.isFocused)
            input.text = configVar.Value;
    }

    public override void UpdateToConfigVar()
    {
        configVar.Value = input.text;
    }
}
