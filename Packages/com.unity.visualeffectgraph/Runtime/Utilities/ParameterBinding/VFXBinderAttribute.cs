using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.VFX.Utils
{
    [AttributeUsage(AttributeTargets.Class)]
    public class VFXBinderAttribute : PropertyAttribute
    {
        public string MenuPath;

        public VFXBinderAttribute(string menuPath)
        {
            MenuPath = menuPath;
        }
    }
}
