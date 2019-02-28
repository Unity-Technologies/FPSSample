using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.VFX.UIElements
{
    class VFXStringFieldPushButton : VFXStringField
    {
        Action<string> m_fnOnClicked;

        public Action<string> pushButtonProvider
        {
            get { return m_fnOnClicked; }
        }

        public VFXStringFieldPushButton(string label, Action<string> fnClicked, string buttonName) : base(label)
        {
            m_fnOnClicked = fnClicked;
            Add(new Button(() => m_fnOnClicked(m_TextField.text)) {text = buttonName});
        }

        public VFXStringFieldPushButton(Label existingLabel, Action<string> fnClicked, string buttonName) : base(existingLabel)
        {
            m_fnOnClicked = fnClicked;
            Add(new Button(() => m_fnOnClicked(m_TextField.text)) {text = buttonName});
        }
    }
}
