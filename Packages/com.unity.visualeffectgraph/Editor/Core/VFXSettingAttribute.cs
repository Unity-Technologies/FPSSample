using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEditor.VFX
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    class VFXSettingAttribute : Attribute
    {
        [Flags]
        public enum VisibleFlags
        {
            InInspector = 1 << 0,
            InGraph = 1 << 1,
            Default = InGraph | InInspector,
            All = 0xFFFF,
            None = 0
        }

        public VFXSettingAttribute(VisibleFlags flags = VisibleFlags.Default)
        {
            visibleFlags = flags;
        }

        public readonly VisibleFlags visibleFlags;
    }
}
