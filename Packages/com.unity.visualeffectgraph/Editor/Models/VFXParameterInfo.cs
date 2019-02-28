using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;
using UnityEngine.Profiling;
using System.Reflection;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [Serializable]
    struct VFXParameterInfo
    {
        public VFXParameterInfo(string exposedName, string realType)
        {
            name = exposedName;
            this.realType = realType;
            path = null;
            tooltip = null;
            defaultValue = null;
            min = Mathf.NegativeInfinity;
            max = Mathf.Infinity;
            descendantCount = 0;
            sheetType = null;
        }

        public string name;
        public string path;
        public string tooltip;

        public string sheetType;

        public string realType;
        public VFXSerializableObject defaultValue;

        public float min;
        public float max;

        public int descendantCount;


        public static VFXParameterInfo[] BuildParameterInfo(VFXGraph graph)
        {
            var categories = graph.UIInfos.categories;
            if (categories == null)
                categories = new List<VFXUI.CategoryInfo>();


            var parameters = graph.children.OfType<VFXParameter>().Where(t => t.exposed && (string.IsNullOrEmpty(t.category) || !categories.Any(u => u.name == t.category))).OrderBy(t => t.order).ToArray();

            var infos = new List<VFXParameterInfo>();
            BuildCategoryParameterInfo(parameters, infos);

            foreach (var cat in categories)
            {
                parameters = graph.children.OfType<VFXParameter>().Where(t => t.exposed && t.category == cat.name).OrderBy(t => t.order).ToArray();
                if (parameters.Length > 0)
                {
                    VFXParameterInfo paramInfo = new VFXParameterInfo(cat.name, "");

                    paramInfo.descendantCount = 0;//parameters.Length;
                    infos.Add(paramInfo);
                    BuildCategoryParameterInfo(parameters, infos);
                }
            }


            return infos.ToArray();
        }

        static void BuildCategoryParameterInfo(VFXParameter[] parameters, List<VFXParameterInfo> infos)
        {
            var subList = new List<VFXParameterInfo>();
            foreach (var parameter in parameters)
            {
                string rootFieldName = VisualEffectSerializationUtility.GetTypeField(parameter.type);

                VFXParameterInfo paramInfo = new VFXParameterInfo(parameter.exposedName, parameter.type.Name);
                paramInfo.tooltip = parameter.tooltip;
                if (rootFieldName != null)
                {
                    paramInfo.sheetType = rootFieldName;
                    paramInfo.path = paramInfo.name;
                    if (parameter.hasRange)
                    {
                        float min = (float)System.Convert.ChangeType(parameter.m_Min.Get(), typeof(float));
                        float max = (float)System.Convert.ChangeType(parameter.m_Max.Get(), typeof(float));
                        paramInfo.min = min;
                        paramInfo.max = max;
                    }
                    paramInfo.defaultValue = new VFXSerializableObject(parameter.type, parameter.value);

                    paramInfo.descendantCount = 0;
                }
                else
                {
                    paramInfo.descendantCount = RecurseBuildParameterInfo(subList, parameter.type, parameter.exposedName, parameter.value);
                }


                infos.Add(paramInfo);
                infos.AddRange(subList);
                subList.Clear();
            }
        }

        static int RecurseBuildParameterInfo(List<VFXParameterInfo> infos, System.Type type, string path, object obj)
        {
            if (!type.IsValueType) return 0;

            int count = 0;
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);

            var subList = new List<VFXParameterInfo>();
            foreach (var field in fields)
            {
                var info = new VFXParameterInfo(field.Name, field.FieldType.Name);
                var subObj = field.GetValue(obj);

                info.path = path + "_" + field.Name;

                var tooltip = field.GetCustomAttributes(typeof(TooltipAttribute), true).FirstOrDefault() as TooltipAttribute;
                if (tooltip != null)
                {
                    info.tooltip = tooltip.tooltip;
                }

                var fieldName = VisualEffectSerializationUtility.GetTypeField(field.FieldType);

                if (fieldName != null)
                {
                    info.sheetType = fieldName;
                    RangeAttribute attr = field.GetCustomAttributes(true).OfType<RangeAttribute>().FirstOrDefault();
                    if (attr != null)
                    {
                        info.min = attr.min;
                        info.max = attr.max;
                    }
                    info.descendantCount = 0;
                    info.defaultValue = new VFXSerializableObject(field.FieldType, subObj);
                }
                else
                {
                    if (field.FieldType == typeof(VFXCoordinateSpace)) // For space
                        continue;
                    info.descendantCount = RecurseBuildParameterInfo(subList, field.FieldType, info.path, subObj);
                }
                infos.Add(info);
                infos.AddRange(subList);
                subList.Clear();
                count++;
            }
            return count;
        }
    }
}
