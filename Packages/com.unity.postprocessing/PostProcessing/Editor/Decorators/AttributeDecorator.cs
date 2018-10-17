using System;
using UnityEngine;

namespace UnityEditor.Rendering.PostProcessing
{
    public abstract class AttributeDecorator
    {
        // Override this and return false if you want to customize the override checkbox position,
        // else it'll automatically draw it and put the property content in a horizontal scope.
        public virtual bool IsAutoProperty()
        {
            return true;
        }

        public abstract bool OnGUI(SerializedProperty property, SerializedProperty overrideState, GUIContent title, Attribute attribute);
    }
}
