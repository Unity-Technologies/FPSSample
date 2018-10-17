using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class LabelSlotControlView : VisualElement
    {
        public LabelSlotControlView(string label)
        {
            var labelField = new Label(label);
            Add(labelField);
        }
    }
}
