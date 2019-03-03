using System;

namespace UnityEditor.VFX
{
    interface IVFXSubRenderer
    {
        bool hasShadowCasting { get; }
        // TODO Add other per output rendering settings here
        int sortPriority { get; set; }
    }
}
