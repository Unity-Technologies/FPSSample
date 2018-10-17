using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class VolumeParameterDrawerAttribute : Attribute
    {
        public readonly Type parameterType;

        public VolumeParameterDrawerAttribute(Type parameterType)
        {
            this.parameterType = parameterType;
        }
    }

    // Default parameter drawer - simply displays the serialized property as Unity would
    public abstract class VolumeParameterDrawer
    {
        // Override this and return false if you want to customize the override checkbox position,
        // else it'll automatically draw it and put the property content in a horizontal scope.
        public virtual bool IsAutoProperty()
        {
            return true;
        }

        // Return false is the input parameter is invalid - unity will display the default editor
        // for this control then
        public abstract bool OnGUI(SerializedDataParameter parameter, GUIContent title);
    }
}
