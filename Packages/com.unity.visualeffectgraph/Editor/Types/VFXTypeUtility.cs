using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    static class VFXTypeUtility
    {
        public static int GetComponentCount(VFXSlot slot)
        {
            var slotType = slot.refSlot.property.type;
            if (slotType == typeof(float) || slotType == typeof(uint) || slotType == typeof(int))
                return 1;
            else if (slotType == typeof(Vector2))
                return 2;
            else if (slotType == typeof(Vector3))
                return 3;
            else if (slotType == typeof(Vector4) || slotType == typeof(Color))
                return 4;
            return 0;
        }

        public static int GetMaxComponentCount(IEnumerable<VFXSlot> slots)
        {
            int maxNbComponents = 0;
            foreach (var slot in slots)
            {
                int slotNbComponents = GetComponentCount(slot);
                maxNbComponents = Math.Max(slotNbComponents, maxNbComponents);
            }
            return maxNbComponents;
        }

        public static int GetComponentCountDirect(VFXSlot slot)
        {
            var slotType = slot.property.type;
            if (slotType == typeof(float) || slotType == typeof(uint) || slotType == typeof(int))
                return 1;
            else if (slotType == typeof(Vector2))
                return 2;
            else if (slotType == typeof(Vector3))
                return 3;
            else if (slotType == typeof(Vector4) || slotType == typeof(Color))
                return 4;
            return 0;
        }

        public static int GetMaxComponentCountDirect(IEnumerable<VFXSlot> slots)
        {
            int maxNbComponents = 0;
            foreach (var slot in slots)
            {
                int slotNbComponents = GetComponentCountDirect(slot);
                maxNbComponents = Math.Max(slotNbComponents, maxNbComponents);
            }
            return maxNbComponents;
        }

        public static Type GetFloatTypeFromComponentCount(int count)
        {
            switch (count)
            {
                case 1: return typeof(float);
                case 2: return typeof(Vector2);
                case 3: return typeof(Vector3);
                case 4: return typeof(Vector4);
                default: return null;
            }
        }
    }
}
