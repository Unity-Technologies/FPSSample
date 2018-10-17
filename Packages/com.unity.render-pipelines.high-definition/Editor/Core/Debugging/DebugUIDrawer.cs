using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    public class DebugUIDrawerAttribute : Attribute
    {
        public readonly Type type;

        public DebugUIDrawerAttribute(Type type)
        {
            this.type = type;
        }
    }

    public class DebugUIDrawer
    {
        protected T Cast<T>(object o)
            where T : class
        {
            var casted = o as T;
            string typeName = o == null ? "null" : o.GetType().ToString();

            if (casted == null)
                throw new InvalidOperationException("Can't cast " + typeName + " to " + typeof(T));

            return casted;
        }

        public virtual void Begin(DebugUI.Widget widget, DebugState state)
        {}

        public virtual bool OnGUI(DebugUI.Widget widget, DebugState state)
        {
            return true;
        }

        public virtual void End(DebugUI.Widget widget, DebugState state)
        {}

        protected void Apply(DebugUI.IValueField widget, DebugState state, object value)
        {
            Undo.RegisterCompleteObjectUndo(state, "Debug Property Change");
            state.SetValue(value, widget);
            widget.SetValue(value);
            EditorUtility.SetDirty(state);
            DebugState.m_CurrentDirtyState = state;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        protected Rect PrepareControlRect(float height = -1)
        {
            if (height < 0)
                height = EditorGUIUtility.singleLineHeight;
            var rect = GUILayoutUtility.GetRect(1f, 1f, height, height);
            rect.width -= 2f;
            rect.xMin += 2f;
            EditorGUIUtility.labelWidth = rect.width / 2f;
            return rect;
        }
    }
}
