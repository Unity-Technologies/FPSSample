
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Entities.Editor
{
    public static class ComponentGroupGUI
    {
        public static void CalculateDrawingParts(List<ComponentType> types, bool archetypeQueryMode, float width, out float height, out List<GUIStyle> styles, out List<GUIContent> names, out List<Rect> rects)
        {
            types.Sort((Comparison<ComponentType>) CompareTypes);
            styles = new List<GUIStyle>(types.Count);
            names = new List<GUIContent>(types.Count);
            rects = new List<Rect>(types.Count);
            var x = 0f;
            var y = 0f;
            foreach (var type in types)
            {
                var style = StyleForAccessMode(type.AccessModeType, archetypeQueryMode);
                var content = new GUIContent((string) SpecifiedTypeName(type.GetManagedType()));
                var rect = new Rect(new Vector2(x, y), style.CalcSize(content));
                if (rect.xMax > width && x != 0f)
                {
                    rect.x = 0f;
                    rect.y += rect.height + 2f;
                }

                x = rect.xMax + 2f;
                y = rect.y;

                styles.Add(style);
                names.Add(content);
                rects.Add(rect);
            }

            height = rects.Last().yMax;
        }

        public static void DrawComponentList(Rect wholeRect, List<GUIStyle> styles, List<GUIContent> names, List<Rect> rects) 
        {
            if (Event.current.type == EventType.Repaint)
            {
                for (var i = 0; i < rects.Count; ++i)
                {
                    var rect = rects[i];
                    rect.position += wholeRect.position;
                    styles[i].Draw(rect, names[i], false, false, false, false);
                }
            }
        }

        public static void ComponentListGUILayout(ComponentType[] types, float width)
        {
            CalculateDrawingParts(types.ToList(), false, width, out var height, out var styles, out var names, out var rects);

            var wholeRect = GUILayoutUtility.GetRect(width, height);
            DrawComponentList(wholeRect, styles, names, rects);
        }

        internal static int CompareTypes(ComponentType x, ComponentType y)
        {
            var accessModeOrder = SortOrderFromAccessMode(x.AccessModeType).CompareTo(SortOrderFromAccessMode(y.AccessModeType));
            return accessModeOrder != 0 ? accessModeOrder : String.Compare(x.GetManagedType().Name, y.GetManagedType().Name, StringComparison.InvariantCulture);
        }

        private static int SortOrderFromAccessMode(ComponentType.AccessMode mode)
        {
            switch (mode)
            {
                case ComponentType.AccessMode.ReadOnly:
                    return 0;
                case ComponentType.AccessMode.ReadWrite:
                    return 1;
                case ComponentType.AccessMode.Subtractive:
                    return 2;
                default:
                    throw new ArgumentException("Unrecognized AccessMode");
            }
        }

        public static string SpecifiedTypeName(Type type)
        {
            var name = type.Name;
            if (type.IsGenericType)
            {
                name = name.Remove(name.IndexOf('`'));
                var genericTypes = type.GetGenericArguments();
                var genericTypeNames = String.Join(", ", genericTypes.Select(SpecifiedTypeName));
                name = $"{name}<{genericTypeNames}>";
            }

            return name;
        }

        internal static GUIStyle StyleForAccessMode(ComponentType.AccessMode mode, bool archetypeQueryMode)
        {
            switch (mode)
            {
                case ComponentType.AccessMode.ReadOnly:
                    return archetypeQueryMode ? EntityDebuggerStyles.ComponentRequired : EntityDebuggerStyles.ComponentReadOnly;
                case ComponentType.AccessMode.ReadWrite:
                    return archetypeQueryMode ? EntityDebuggerStyles.ComponentRequired : EntityDebuggerStyles.ComponentReadWrite;
                case ComponentType.AccessMode.Subtractive:
                    return EntityDebuggerStyles.ComponentSubtractive;
                default:
                    throw new ArgumentException("Unrecognized access mode");
            }
        }
    }
}
