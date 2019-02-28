using System;
using System.Linq;
using UnityEngine;

// TODO: Remove after migration
namespace UnityEditor.VFX
{
    class VFXCurrentAttributeParameter : VFXAttributeParameter
    {
        VFXCurrentAttributeParameter()
        {
            location = VFXAttributeLocation.Current;
        }

        public override void Sanitize(int version)
        {
            // Create new operator
            var attrib = ScriptableObject.CreateInstance<VFXAttributeParameter>();
            attrib.SetSettingValue("location", VFXAttributeLocation.Current);
            attrib.SetSettingValue("attribute", attribute);
            attrib.position = position;

            VFXSlot.CopyLinksAndValue(attrib.GetOutputSlot(0), GetOutputSlot(0), true);
            ReplaceModel(attrib, this);
        }
    }
}
