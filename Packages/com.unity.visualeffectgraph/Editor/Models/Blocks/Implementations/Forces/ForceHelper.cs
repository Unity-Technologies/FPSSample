using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using System;

namespace UnityEditor.VFX.Block
{
    public enum ForceMode
    {
        Absolute,
        Relative
    }

    static class ForceHelper
    {
        public class DragProperties
        {
            [Min(0.0f), Tooltip("Drag coefficient. The higher the drag, the more the force will have influence over the particle velocity")]
            public float Drag = 1.0f;
        }

        public static IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            }
        }

        public static string ApplyForceString(ForceMode mode, string forceStr)
        {
            switch (mode)
            {
                case ForceMode.Absolute: return string.Format("({0} / mass) * deltaTime", forceStr);
                case ForceMode.Relative: return string.Format("({0} - velocity) * min(1.0f,Drag * deltaTime / mass)", forceStr);
                default: throw new NotImplementedException();
            }
        }
    }
}
