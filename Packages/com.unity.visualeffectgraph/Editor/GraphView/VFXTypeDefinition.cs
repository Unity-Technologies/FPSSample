using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    static class VFXTypeDefinition
    {
        public static readonly Type[] potentialTypes = new Type[]
        {
            typeof(bool),
            typeof(int),
            typeof(uint),
            typeof(float),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Color),
            typeof(Texture2D),
            typeof(Texture2DArray),
            typeof(Texture3D),
            typeof(Cubemap),
            typeof(CubemapArray),
            typeof(Mesh),
            typeof(Vector),
            typeof(Position),
            typeof(FlipBook),
            typeof(AnimationCurve),
            typeof(Object)
        };
        private static readonly string[] cssClasses = null;


        static VFXTypeDefinition()
        {
            cssClasses = new string[potentialTypes.Length + 1];
            for (int i = 0; i < potentialTypes.Length; ++i)
            {
                cssClasses[i] = "type" + potentialTypes[i].Name.ToLower();
            }

            cssClasses[potentialTypes.Length] = "typeStruct";
        }

        public static string GetTypeCSSClass(Type type)
        {
            if (type == null)
            {
                return "typeUnknown";
            }
            int index = MatchType(type);
            if (index >= 0)
            {
                return cssClasses[index];
            }
            return "";
        }

        public static string[] GetTypeCSSClasses()
        {
            return cssClasses;
        }

        static int MatchType(Type type)
        {
            for (int i = 0; i < potentialTypes.Length; ++i)
            {
                if (potentialTypes[i].IsAssignableFrom(type))
                    return i;
            }
            if (type.IsValueType && !type.IsPrimitive && !type.IsEnum)
            {
                return potentialTypes.Length - 1;
            }
            return -1;
        }
    }
}
