using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.VFX.Utils
{
    [AttributeUsage(AttributeTargets.Field)]
    public class VFXParameterBindingAttribute : PropertyAttribute
    {
        public string[] EditorTypes;

        public VFXParameterBindingAttribute(params string[] editorTypes)
        {
            EditorTypes = editorTypes;
        }
    }
}
