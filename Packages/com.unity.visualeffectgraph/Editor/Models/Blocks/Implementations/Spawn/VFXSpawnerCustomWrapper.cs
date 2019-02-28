using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class CustomSpawnerVariant : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "m_customType", VFXLibrary.FindConcreteSubclasses(typeof(VFXSpawnerCallbacks)).Select(o => new SerializableType(o) as object).ToArray() }
                };
            }
        }
    }

    [VFXInfo(category = "Spawn/Custom", variantProvider = typeof(CustomSpawnerVariant))]
    class VFXSpawnerCustomWrapper : VFXAbstractSpawner
    {
        [SerializeField, VFXSetting]
        protected SerializableType m_customType;

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                yield return "m_customType";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                return customBehavior == null ? Enumerable.Empty<VFXPropertyWithValue>() : PropertiesFromType(customBehavior.GetRecursiveNestedType(GetInputPropertiesTypeName()));
            }
        }

        public override sealed string name { get { return customBehavior == null ? "null" : ObjectNames.NicifyVariableName((customBehavior).Name); } }
        public override sealed Type customBehavior { get { return (Type)m_customType; } }
        public override sealed VFXTaskType spawnerType { get { return VFXTaskType.CustomCallbackSpawner; } }
    }
}
