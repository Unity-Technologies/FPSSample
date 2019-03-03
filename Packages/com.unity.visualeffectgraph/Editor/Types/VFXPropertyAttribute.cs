using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.VFX
{
    // Attribute used to normalize a Vector or float
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class NormalizeAttribute : PropertyAttribute
    {
    }

    // Attribute used to display a float in degrees in the UI
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class AngleAttribute : PropertyAttribute
    {
    }

    // Attribute used to constrain a property to a Regex query
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class RegexAttribute : PropertyAttribute
    {
        public RegexAttribute(string _pattern, int _maxLength = int.MaxValue) { pattern = _pattern; maxLength = _maxLength; }

        public string pattern { get; set; }
        public int maxLength { get; set; }
    }

    [Serializable]
    class VFXPropertyAttribute
    {
        private static readonly Dictionary<System.Type, Func<object, VFXPropertyAttribute>> s_RegisteredAttributes = new Dictionary<System.Type, Func<object, VFXPropertyAttribute>>()
        {
            { typeof(RangeAttribute), o => new VFXPropertyAttribute(Type.kRange, (o as RangeAttribute).min, (o as RangeAttribute).max) },
            { typeof(MinAttribute), o => new VFXPropertyAttribute(Type.kMin, (o as MinAttribute).min) },
            { typeof(NormalizeAttribute), o => new VFXPropertyAttribute(Type.kNormalize) },
            { typeof(TooltipAttribute), o => new VFXPropertyAttribute(Type.kTooltip, (o as TooltipAttribute).tooltip) },
            { typeof(AngleAttribute), o => new VFXPropertyAttribute(Type.kAngle) },
            { typeof(ShowAsColorAttribute), o => new VFXPropertyAttribute(Type.kColor) },
            { typeof(RegexAttribute), o => new VFXPropertyAttribute(Type.kRegex, (o as RegexAttribute).pattern, (o as RegexAttribute).maxLength) },
            { typeof(DelayedAttribute), o => new VFXPropertyAttribute(Type.kDelayed) },
        };

        public static VFXPropertyAttribute[] Create(params object[] attributes)
        {
            return attributes.SelectMany(a => s_RegisteredAttributes.Where(o => o.Key.IsAssignableFrom(a.GetType()))
                .Select(o => o.Value(a))).ToArray();
        }

        public static VFXExpression ApplyToExpressionGraph(VFXPropertyAttribute[] attributes, VFXExpression exp)
        {
            if (attributes != null)
            {
                foreach (VFXPropertyAttribute attribute in attributes)
                {
                    switch (attribute.m_Type)
                    {
                        case Type.kRange:
                            exp = VFXOperatorUtility.Clamp(exp, VFXValue.Constant(attribute.m_Min), VFXValue.Constant(attribute.m_Max));
                            break;
                        case Type.kMin:
                            exp = new VFXExpressionMax(exp, VFXOperatorUtility.CastFloat(VFXValue.Constant(attribute.m_Min), exp.valueType));
                            break;
                        case Type.kNormalize:
                            exp = VFXOperatorUtility.Normalize(exp);
                            break;
                        case Type.kTooltip:
                        case Type.kAngle:
                        case Type.kColor:
                        case Type.kRegex:
                        case Type.kDelayed:
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }

            return exp;
        }

        public static void ApplyToGUI(VFXPropertyAttribute[] attributes, ref string label, ref string tooltip)
        {
            if (attributes != null)
            {
                foreach (VFXPropertyAttribute attribute in attributes)
                {
                    switch (attribute.m_Type)
                    {
                        case Type.kRange:
                            break;
                        case Type.kMin:
                            label += " (Min: " + attribute.m_Min + ")";
                            break;
                        case Type.kNormalize:
                            label += " (Normalized)";
                            break;
                        case Type.kTooltip:
                            tooltip = attribute.m_Tooltip;
                            break;
                        case Type.kAngle:
                            label += " (Angle)";
                            break;
                        case Type.kColor:
                        case Type.kRegex:
                        case Type.kDelayed:
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }

        public static Vector2 FindRange(VFXPropertyAttribute[] attributes)
        {
            if (attributes != null)
            {
                VFXPropertyAttribute attribute = attributes.FirstOrDefault(o => o.m_Type == Type.kRange);
                if (attribute != null)
                    return new Vector2(attribute.m_Min, attribute.m_Max);

                attribute = attributes.FirstOrDefault(o => o.m_Type == Type.kMin);
                if (attribute != null)
                    return new Vector2(attribute.m_Min, Mathf.Infinity);
            }

            return Vector2.zero;
        }

        public static bool IsAngle(VFXPropertyAttribute[] attributes)
        {
            if (attributes != null)
                return attributes.Any(o => o.m_Type == Type.kAngle);
            return false;
        }

        public static bool IsColor(VFXPropertyAttribute[] attributes)
        {
            if (attributes != null)
                return attributes.Any(o => o.m_Type == Type.kColor);
            return false;
        }

        public static bool IsDelayed(VFXPropertyAttribute[] attributes)
        {
            if (attributes != null)
                return attributes.Any(o => o.m_Type == Type.kDelayed);
            return false;
        }

        public static string ApplyRegex(VFXPropertyAttribute[] attributes, object obj)
        {
            if (attributes != null)
            {
                var attrib = attributes.FirstOrDefault(o => o.m_Type == Type.kRegex);
                if (attrib != null)
                {
                    string str = (string)obj;
                    str = Regex.Replace(str, attrib.m_Regex, "");
                    return str.Substring(0, Math.Min(str.Length, attrib.m_RegexMaxLength));
                }
            }

            return null;
        }

        public enum Type
        {
            kRange,
            kMin,
            kNormalize,
            kTooltip,
            kAngle,
            kColor,
            kRegex,
            kDelayed
        }

        public VFXPropertyAttribute(Type type, float min = -Mathf.Infinity, float max = Mathf.Infinity)
        {
            m_Type = type;
            m_Min = min;
            m_Max = max;
        }

        public VFXPropertyAttribute(Type type, string str, int regexMaxLength = int.MaxValue)
        {
            m_Type = type;
            m_Min = -Mathf.Infinity;
            m_Max = Mathf.Infinity;

            if (type == Type.kTooltip)
            {
                m_Tooltip = str;
            }
            else
            {
                m_Regex = str;
                m_RegexMaxLength = regexMaxLength;
            }
        }

        [SerializeField]
        private Type m_Type;
        [SerializeField]
        private float m_Min;
        [SerializeField]
        private float m_Max;
        [SerializeField]
        private string m_Tooltip;
        [SerializeField]
        private string m_Regex;
        [SerializeField]
        private int m_RegexMaxLength;
    }
}
