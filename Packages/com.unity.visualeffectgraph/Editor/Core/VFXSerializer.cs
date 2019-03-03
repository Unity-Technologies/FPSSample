using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using System.Globalization;
using TangentMode = UnityEditor.AnimationUtility.TangentMode;

namespace UnityEditor.VFX
{
    [Serializable]
    public class SerializableType : ISerializationCallbackReceiver
    {
        public static implicit operator SerializableType(Type value)
        {
            return new SerializableType(value);
        }

        public static implicit operator Type(SerializableType value)
        {
            return value != null ? value.m_Type : null;
        }

        private SerializableType() {}
        public SerializableType(Type type)
        {
            m_Type = type;
        }

        public SerializableType(string typeText)
        {
            m_SerializableType = typeText;
            OnAfterDeserialize();
        }

        public virtual void OnBeforeSerialize()
        {
            m_SerializableType = m_Type != null ? m_Type.AssemblyQualifiedName : string.Empty;
        }

        public virtual void OnAfterDeserialize()
        {
            m_Type = GetType(m_SerializableType);
        }

        public static Type GetType(string name)
        {
            Type type = Type.GetType(name);

            if (type == null && !string.IsNullOrEmpty(name)) // if type wasn't found, resolve the assembly (to use VFX package assembly name instead)
            {
                string[] splitted = name.Split(',');
                // Replace the assembly with the one containing VFXGraph type which will be either "Unity.VisualEffect.Graph.Editor" or "Unity.VisualEffect.Graph.Editor-testable"
                splitted[1] = typeof(VFXGraph).Assembly.GetName().Name;

                name = string.Join(",", splitted);

                type = Type.GetType(name);

                if (type == null) // resolve runtime type if editor assembly didnt work
                {
                    splitted[1] = splitted[1].Replace(".Editor", ".Runtime");
                    name = string.Join(",", splitted);
                    type = Type.GetType(name);
                }

                // If from here we still haven't found the type, try a last time with the name only.
                if (type == null)
                {
                    AppDomain currentDomain = AppDomain.CurrentDomain;
                    foreach (Assembly assembly in currentDomain.GetAssemblies())
                    {
                        type = assembly.GetType(splitted[0]);
                        if (type != null)
                            return type;
                    }
                }

                if (type == null)
                    Debug.LogErrorFormat("Cannot get Type from name: {0}", name);
            }

            return type;
        }

        public string text
        {
            get { OnBeforeSerialize(); return m_SerializableType; }
        }

        [NonSerialized]
        private Type m_Type;
        [SerializeField]
        private string m_SerializableType;
    }

    [Serializable]
    public class VFXSerializableObject
    {
        private VFXSerializableObject() {}

        public VFXSerializableObject(Type type, object obj) : this(type)
        {
            Set(obj);
        }

        public VFXSerializableObject(Type type)
        {
            m_Type = type;
        }

        public object Get()
        {
            return VFXSerializer.Load(m_Type, m_SerializableObject, m_CachedValue);
        }

        public T Get<T>()
        {
            return (T)Get();
        }

        public bool Set(object obj)
        {
            var newValue = string.Empty;
            if (obj != null)
            {
                Type type = m_Type;

                if (!type.IsAssignableFrom(obj.GetType()))
                {
                    if (obj is UnityEngine.Object && (obj as UnityEngine.Object == null))
                    {
                        // Some object couldn't be loaded. just ignore it.
                    }
                    else if (obj is Texture && typeof(Texture).IsAssignableFrom(type))
                    {
                    }
                    else
                    {
                        throw new ArgumentException(string.Format("Cannot assign an object of type {0} to VFXSerializedObject of type {1}", obj.GetType(), (Type)m_Type));
                    }
                }
                newValue = VFXSerializer.Save(obj);
            }
            m_CachedValue = obj;
            if (m_SerializableObject != newValue)
            {
                m_SerializableObject = newValue;
                return true;
            }
            return false;
        }

        public Type type
        {
            get { return m_Type; }
        }

        [SerializeField]
        private SerializableType m_Type;

        [SerializeField]
        private string m_SerializableObject;


        private object m_CachedValue;
    }


    public static class VFXSerializer
    {
        [System.Serializable]
        public struct TypedSerializedData
        {
            public string data;
            public string type; // TODO This should have used SerializableType!

            public static TypedSerializedData Null = new TypedSerializedData();
        }

        [Serializable]
        private struct ObjectWrapper
        {
            public UnityEngine.Object obj;
        }


        [System.Serializable]
        class AnimCurveWrapper
        {
            [System.Serializable]
            public struct Keyframe
            {
                public float time;
                public float value;
                public float inTangent;
                public float outTangent;

                public int tangentMode;
                public TangentMode leftTangentMode;
                public TangentMode rightTangentMode;

                public bool broken;
            }
            public Keyframe[] frames;
            public WrapMode preWrapMode;
            public WrapMode postWrapMode;
            public int version;
        }

        [System.Serializable]
        class GradientWrapper
        {
            [System.Serializable]
            public struct ColorKey
            {
                public Color color;
                public float time;
            }
            [System.Serializable]
            public struct AlphaKey
            {
                public float alpha;
                public float time;
            }
            public ColorKey[] colorKeys;
            public AlphaKey[] alphaKeys;

            public GradientMode gradientMode;
        }

        public static TypedSerializedData SaveWithType(object obj)
        {
            TypedSerializedData data = new TypedSerializedData();
            data.data = VFXSerializer.Save(obj);
            data.type = obj.GetType().AssemblyQualifiedName;

            return data;
        }

        public static object LoadWithType(TypedSerializedData data, object oldValue)
        {
            if (!string.IsNullOrEmpty(data.data))
            {
                System.Type type = SerializableType.GetType(data.type);
                if (type == null)
                {
                    Debug.LogError("Can't find type " + data.type);
                    return null;
                }

                return VFXSerializer.Load(type, data.data, oldValue);
            }

            return null;
        }

        public static string Save(object obj)
        {
            if (obj == null)
                return string.Empty;

            if (obj.GetType().IsPrimitive)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", obj);
            }
            else if (obj is UnityEngine.Object) //type is a unity object
            {
                if ((obj as UnityEngine.Object) == null)
                {
                    return string.Empty;
                }
                ObjectWrapper wrapper = new ObjectWrapper { obj = obj as UnityEngine.Object };
                var json = EditorJsonUtility.ToJson(wrapper);
                return json;
            }
            else if (obj is AnimationCurve)
            {
                AnimCurveWrapper sac = new AnimCurveWrapper();
                AnimationCurve curve = obj as AnimationCurve;


                sac.frames = new AnimCurveWrapper.Keyframe[curve.keys.Length];
                for (int i = 0; i < curve.keys.Length; ++i)
                {
                    sac.frames[i].time = curve.keys[i].time;
                    sac.frames[i].value = curve.keys[i].value;
                    sac.frames[i].inTangent = curve.keys[i].inTangent;
                    sac.frames[i].outTangent = curve.keys[i].outTangent;
                    sac.frames[i].tangentMode = 0; // Not used
                    sac.frames[i].leftTangentMode = AnimationUtility.GetKeyLeftTangentMode(curve, i);
                    sac.frames[i].rightTangentMode = AnimationUtility.GetKeyRightTangentMode(curve, i);
                    sac.frames[i].broken = AnimationUtility.GetKeyBroken(curve, i);
                }
                sac.preWrapMode = curve.preWrapMode;
                sac.postWrapMode = curve.postWrapMode;
                sac.version = 1;

                return JsonUtility.ToJson(sac);
            }
            else if (obj is Gradient)
            {
                GradientWrapper gw = new GradientWrapper();
                Gradient gradient = obj as Gradient;

                gw.gradientMode = gradient.mode;
                gw.colorKeys = new GradientWrapper.ColorKey[gradient.colorKeys.Length];
                for (int i = 0; i < gradient.colorKeys.Length; ++i)
                {
                    gw.colorKeys[i].color = gradient.colorKeys[i].color;
                    gw.colorKeys[i].time = gradient.colorKeys[i].time;
                }
                gw.alphaKeys = new GradientWrapper.AlphaKey[gradient.alphaKeys.Length];
                for (int i = 0; i < gradient.alphaKeys.Length; ++i)
                {
                    gw.alphaKeys[i].alpha = gradient.alphaKeys[i].alpha;
                    gw.alphaKeys[i].time = gradient.alphaKeys[i].time;
                }
                return JsonUtility.ToJson(gw);
            }
            else if (obj is string)
            {
                return "\"" + ((string)obj).Replace("\"", "\\\"") + "\"";
            }
            else if (obj is SerializableType)
            {
                return "\"" + ((SerializableType)obj).text + "\"";
            }
            else if (obj.GetType().IsArrayOrList())
            {
                IList list = (IList)obj;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append('[');
                for (int i = 0; i < list.Count; ++i)
                {
                    sb.Append(Save(list[i]));
                    sb.Append(',');
                }
                sb.Length = sb.Length - 1;
                sb.Append(']');

                return sb.ToString();
            }
            else
            {
                return EditorJsonUtility.ToJson(obj);
            }
        }

        const int kBrokenMask = 1 << 0;
        const int kLeftTangentMask = 1 << 1 | 1 << 2 | 1 << 3 | 1 << 4;
        const int kRightTangentMask = 1 << 5 | 1 << 6 | 1 << 7 | 1 << 8;

        public static object Load(System.Type type, string text, object oldValue)
        {
            if (type == null)
                return null;

            if (type.IsPrimitive)
            {
                if (string.IsNullOrEmpty(text))
                    try
                    {
                        return Activator.CreateInstance(type);
                    }
                    catch (MissingMethodException e)
                    {
                        Debug.LogError(type.Name + " Doesn't seem to have a default constructor");

                        throw e;
                    }

                return Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                object obj = new ObjectWrapper();
                EditorJsonUtility.FromJsonOverwrite(text, obj);

                return ((ObjectWrapper)obj).obj;
            }
            else if (type.IsAssignableFrom(typeof(AnimationCurve)))
            {
                AnimCurveWrapper sac = new AnimCurveWrapper();

                JsonUtility.FromJsonOverwrite(text, sac);

                AnimationCurve curve = oldValue != null ? (AnimationCurve)oldValue : new AnimationCurve();

                if (sac.frames != null)
                {
                    Keyframe[] keys = new UnityEngine.Keyframe[sac.frames.Length];
                    for (int i = 0; i < sac.frames.Length; ++i)
                    {
                        keys[i].time = sac.frames[i].time;
                        keys[i].value = sac.frames[i].value;
                        keys[i].inTangent = sac.frames[i].inTangent;
                        keys[i].outTangent = sac.frames[i].outTangent;
                        if (sac.version == 1)
                        {
                            AnimationUtility.SetKeyLeftTangentMode(ref keys[i], sac.frames[i].leftTangentMode);
                            AnimationUtility.SetKeyRightTangentMode(ref keys[i], sac.frames[i].rightTangentMode);
                            AnimationUtility.SetKeyBroken(ref keys[i], sac.frames[i].broken);
                        }
                        else
                        {
                            AnimationUtility.SetKeyLeftTangentMode(ref keys[i], (TangentMode)((sac.frames[i].tangentMode & kLeftTangentMask) >> 1));
                            AnimationUtility.SetKeyRightTangentMode(ref keys[i], (TangentMode)((sac.frames[i].tangentMode & kRightTangentMask) >> 5));
                            AnimationUtility.SetKeyBroken(ref keys[i], (sac.frames[i].tangentMode & kBrokenMask) != 0);
                        }
                    }
                    curve.keys = keys;
                    curve.preWrapMode = sac.preWrapMode;
                    curve.postWrapMode = sac.postWrapMode;
                }

                return curve;
            }
            else if (type.IsAssignableFrom(typeof(Gradient)))
            {
                GradientWrapper gw = new GradientWrapper();
                Gradient gradient = oldValue != null ? (Gradient)oldValue : new Gradient();

                JsonUtility.FromJsonOverwrite(text, gw);

                gradient.mode = gw.gradientMode;

                GradientColorKey[] colorKeys = null;
                if (gw.colorKeys != null)
                {
                    colorKeys = new GradientColorKey[gw.colorKeys.Length];
                    for (int i = 0; i < gw.colorKeys.Length; ++i)
                    {
                        colorKeys[i].color = gw.colorKeys[i].color;
                        colorKeys[i].time = gw.colorKeys[i].time;
                    }
                }
                else
                    colorKeys = new GradientColorKey[0];

                GradientAlphaKey[] alphaKeys = null;

                if (gw.alphaKeys != null)
                {
                    alphaKeys = new GradientAlphaKey[gw.alphaKeys.Length];
                    for (int i = 0; i < gw.alphaKeys.Length; ++i)
                    {
                        alphaKeys[i].alpha = gw.alphaKeys[i].alpha;
                        alphaKeys[i].time = gw.alphaKeys[i].time;
                    }
                }
                else
                    alphaKeys = new GradientAlphaKey[0];

                gradient.SetKeys(colorKeys, alphaKeys);
                return gradient;
            }
            else if (type == typeof(string))
            {
                return text.Substring(1, text.Length - 2).Replace("\\\"", "\"");
            }
            else if (type == typeof(SerializableType))
            {
                var obj = new SerializableType(text.Substring(1, text.Length - 2));
                return obj;
            }
            else if (type.IsArrayOrList())
            {
                List<string> elements = ParseArray(text);

                if (elements == null)
                    return null;
                if (type.IsArray)
                {
                    int listCount = elements.Count;

                    Array arrayObj = (Array)Activator.CreateInstance(type, new object[] { listCount });

                    for (int index = 0; index < listCount; index++)
                    {
                        arrayObj.SetValue(Load(type.GetElementType(), elements[index], null), index);
                    }

                    return arrayObj;
                }
                else //List
                {
                    int listCount = elements.Count;
                    IList listObj = (Array)Activator.CreateInstance(type, new object[] { listCount });
                    for (int index = 0; index < listCount; index++)
                    {
                        listObj.Add(Load(type.GetElementType(), elements[index], null));
                    }

                    return listObj;
                }
            }
            else
            {
                try
                {
                    object obj = Activator.CreateInstance(type);
                    EditorJsonUtility.FromJsonOverwrite(text, obj);
                    return obj;
                }
                catch (MissingMethodException e)
                {
                    Debug.LogError(type.Name + " Doesn't seem to have a default constructor");

                    throw e;
                }
            }
        }

        internal static List<string> ParseArray(string arrayText)
        {
            List<string> elements = new List<string>();


            int cur = 0;
            bool isInString = false;
            bool ignoreNext = false;
            int depth = 0; // depth of []
            int bracketDepth = 0; //depth of {}

            int prevElementStart = 0;


            foreach (char c in arrayText)
            {
                switch (c)
                {
                    case '{':
                        ignoreNext = false;
                        if (!isInString)
                            bracketDepth++;
                        break;
                    case '}':
                        ignoreNext = false;
                        if (!isInString)
                            bracketDepth--;
                        break;
                    case '[':
                        ignoreNext = false;
                        if (!isInString && bracketDepth == 0)
                        {
                            depth++;
                            if (depth == 1)
                                prevElementStart = cur + 1;
                        }
                        break;
                    case ']':
                        ignoreNext = false;
                        if (!isInString && bracketDepth == 0)
                        {
                            depth--;
                            if (depth < 0)
                                goto error;
                            if (depth == 0)
                                elements.Add(arrayText.Substring(prevElementStart, cur - prevElementStart));
                        }
                        return elements;
                    case ',':
                        ignoreNext = false;
                        if (!isInString && bracketDepth == 0)
                        {
                            elements.Add(arrayText.Substring(prevElementStart, cur - prevElementStart));
                            prevElementStart = cur + 1;
                        }
                        break;
                    case '"':
                        if (!isInString)
                            isInString = true;
                        else if (!ignoreNext)
                            isInString = false;
                        break;
                    case '\\':
                        if (isInString)
                        {
                            ignoreNext = !ignoreNext;
                        }
                        break;
                    default:
                        ignoreNext = false;
                        break;
                }
                ++cur;
            }
        error:
            Debug.LogError("Couln't parse array" + arrayText + " from " + cur);

            return null;
        }
    }
}
