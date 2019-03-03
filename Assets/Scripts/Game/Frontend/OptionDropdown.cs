using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionDropdown : OptionUI
{
    public TMPro.TextMeshProUGUI title;
    public TMPro.TMP_Dropdown dropdown;
    internal List<string> values;

    public override void UpdateFromConfigVar()
    {
        if (dropdown.value < values.Count && configVar.Value == values[dropdown.value])
            return;

        bool found = false;
        for(int i = 0; i < values.Count; i++)
        {
            if(configVar.Value == values[i])
            {
                dropdown.value = i;
                found = true;
                break;
            }
        }
        // If not found set to one past last valid value, if the dropdown has one such
        if(!found && dropdown.options.Count > values.Count)
        {
            dropdown.value = dropdown.options.Count - 1;
        }
    }

    public override void UpdateToConfigVar()
    {
        if(dropdown.value < values.Count)
            configVar.Value = values[dropdown.value];
    }
}
