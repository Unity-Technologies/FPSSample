using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Type = System.Type;
using Convert = System.Convert;
using System.Linq;
using UnityObject = UnityEngine.Object;
using UnityEditor.VFX.UI;

namespace UnityEditor.VFX
{
    static class VFXConverter
    {
        public static T ConvertTo<T>(object value)
        {
            return (T)ConvertTo(value, typeof(T));
        }

        static readonly Dictionary<System.Type, Dictionary<System.Type, System.Func<object, object>>> s_Converters = new Dictionary<System.Type, Dictionary<System.Type, System.Func<object, object>>>();

        static VFXConverter()
        {
            //Register conversion that you only want in the UI here
            RegisterCustomConverter<Vector3, Vector4>(t => new Vector4(t.x, t.y, t.z));
            RegisterCustomConverter<Vector2, Vector4>(t => new Vector4(t.x, t.y, 0));
            RegisterCustomConverter<Vector2, Vector3>(t => new Vector3(t.x, t.y, 0));
            RegisterCustomConverter<Vector2, Color>(t => new Color(t.x, t.y, 0));
            RegisterCustomConverter<Vector3, Color>(t => new Color(t.x, t.y, t.z));
            RegisterCustomConverter<Vector4, Color>(t => new Color(t.x, t.y, t.z, t.w));
            RegisterCustomConverter<Matrix4x4, Transform>(MakeTransformFromMatrix4x4);
            RegisterCustomConverter<Vector2, float>(t => t.x);
            RegisterCustomConverter<Vector3, float>(t => t.x);
            RegisterCustomConverter<Vector4, float>(t => t.x);
            RegisterCustomConverter<Color, Vector2>(t => new Vector2(t.r, t.g));
            RegisterCustomConverter<Color, Vector3>(t => new Vector3(t.r, t.g, t.b));
            RegisterCustomConverter<Color, float>(t => t.a);
            RegisterCustomConverter<Transform, OrientedBox>(MakeOrientedBoxFromTransform);
            RegisterCustomConverter<Matrix4x4, OrientedBox>(t => MakeOrientedBoxFromTransform(MakeTransformFromMatrix4x4(t)));
        }

        static Transform MakeTransformFromMatrix4x4(Matrix4x4 mat)
        {
            var result = new Transform
            {
                position = mat.MultiplyPoint(Vector3.zero),
                angles = mat.rotation.eulerAngles,
                scale = mat.lossyScale
            };

            return result;
        }

        static OrientedBox MakeOrientedBoxFromTransform(Transform t)
        {
            var result = new OrientedBox
            {
                center = t.position,
                angles = t.angles,
                size = t.scale
            };

            return result;
        }

        static void RegisterCustomConverter<TFrom, TTo>(System.Func<TFrom, TTo> func)
        {
            Dictionary<System.Type, System.Func<object, object>> converters = null;
            if (!s_Converters.TryGetValue(typeof(TFrom), out converters))
            {
                converters = new Dictionary<System.Type, System.Func<object, object>>();
                s_Converters.Add(typeof(TFrom), converters);
            }

            converters.Add(typeof(TTo), t => func((TFrom)t));
        }

        static object ConvertUnityObject(object value, Type toType)
        {
            var castedValue = (UnityObject)value;
            if (castedValue == null) // null object don't have necessarly the correct type
                return null;

            if (!toType.IsInstanceOfType(value))
            {
                Debug.LogErrorFormat("Cannot cast from {0} to {1}", value.GetType(), toType);
                return null;
            }

            return value;
        }

        static object TryConvertPrimitiveType(object value, Type toType)
        {
            try
            {
                return Convert.ChangeType(value, toType);
            }
            catch (InvalidCastException)
            {
            }
            catch (OverflowException)
            {
            }

            return System.Activator.CreateInstance(toType);
        }

        static System.Func<object, object> GetConverter(Type fromType, Type toType)
        {
            if (typeof(UnityObject).IsAssignableFrom(fromType))
            {
                return t => ConvertUnityObject(t, toType);
            }

            Dictionary<System.Type, System.Func<object, object>> converters = null;
            if (!s_Converters.TryGetValue(fromType, out converters))
            {
                converters = new Dictionary<System.Type, System.Func<object, object>>();
                s_Converters.Add(fromType, converters);
            }


            System.Func<object, object> converter = null;
            if (!converters.TryGetValue(toType, out converter))
            {
                if (fromType == toType || toType.IsAssignableFrom(fromType))
                {
                    converter = t => t;
                }
                else if (toType.IsEnum && (fromType == typeof(uint) || fromType == typeof(int)))
                {
                    if (fromType == typeof(uint))
                        converter = t => Enum.ToObject(toType, (uint)t);
                    else
                        converter = t => Enum.ToObject(toType, (int)t);
                }
                else if (fromType.IsEnum && (toType == typeof(uint) || toType == typeof(int)))
                {
                    converter = t => Convert.ChangeType(t, toType);
                }
                else
                {
                    var implicitMethod = fromType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                        .FirstOrDefault(m => m.Name == "op_Implicit" && m.ReturnType == toType);
                    if (implicitMethod != null)
                    {
                        converter = t => implicitMethod.Invoke(null, new object[] { t });
                    }
                    else
                    {
                        implicitMethod = toType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                            .FirstOrDefault(m => m.Name == "op_Implicit" && m.GetParameters()[0].ParameterType == fromType && m.ReturnType == toType);
                        if (implicitMethod != null)
                        {
                            converter = t => implicitMethod.Invoke(null, new object[] { t });
                        }
                    }
                    if (converter == null)
                    {
                        if (toType.IsPrimitive)
                        {
                            if (fromType.IsPrimitive)
                                converter = t => TryConvertPrimitiveType(t, toType);
                            else if (toType != typeof(float))
                            {
                                var floatConverter = GetConverter(fromType, typeof(float));
                                if (floatConverter != null)
                                {
                                    converter = t => TryConvertPrimitiveType(floatConverter(t), toType);
                                }
                            }
                        }
                    }
                }
                converters.Add(toType, converter);
            }

            return converter;
        }

        public static object ConvertTo(object value, Type type)
        {
            if (value == null)
                return null;
            var fromType = value.GetType();

            var converter = GetConverter(fromType, type);

            if (converter == null)
            {
                Debug.LogErrorFormat("Cannot cast from {0} to {1}", fromType, type);
                return null;
            }

            return converter(value);
        }

        public static bool TryConvertTo(object value, Type type, out object result)
        {
            if (value == null)
            {
                result = null;
                return true;
            }
            var fromType = value.GetType();

            var converter = GetConverter(fromType, type);

            if (converter == null)
            {
                result = null;
                return false;
            }

            result = converter(value);

            return true;
        }

        public static bool CanConvertTo(Type from,Type to)
        {
            return GetConverter(from, to) != null;
        }
    }
}
