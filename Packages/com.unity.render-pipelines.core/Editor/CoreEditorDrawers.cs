using System;
using System.Collections.Generic;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    [Flags]
    public enum FoldoutOption
    {
        None = 0,
        Indent = 1 << 0,
        Animate = 1 << 1,
        Boxed = 1 << 2
    }

    [Flags]
    public enum FadeOption
    {
        None = 0,
        Indent = 1 << 0,
        Animate = 1 << 1
    }

    public static class CoreEditorDrawer<TUIState, TData>
    {
        public interface IDrawer
        {
            void Draw(TUIState s, TData p, Editor owner);
        }

        public delegate T2UIState StateSelect<T2UIState>(TUIState s, TData d, Editor o);
        public delegate T2Data DataSelect<T2Data>(TUIState s, TData d, Editor o);

        public delegate void ActionDrawer(TUIState s, TData p, Editor owner);
        public delegate AnimBool AnimBoolItemGetter(TUIState s, TData p, Editor owner, int i);
        public delegate AnimBool AnimBoolGetter(TUIState s, TData p, Editor owner);

        public static readonly IDrawer space = Action((state, data, owner) => EditorGUILayout.Space());
        public static readonly IDrawer noop = Action((state, data, owner) => {});

        public static IDrawer Group(params IDrawer[] drawers)
        {
            return new GroupDrawerInternal(drawers);
        }

        public static IDrawer LabelWidth(float width, params IDrawer[] drawers)
        {
            return Action((s, d, o) =>
                {
                    var l = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = width;
                    for (var i = 0; i < drawers.Length; ++i)
                        drawers[i].Draw(s, d, o);
                    EditorGUIUtility.labelWidth = l;
                }
                );
        }

        public static IDrawer Action(params ActionDrawer[] drawers)
        {
            return new ActionDrawerInternal(drawers);
        }

        public static IDrawer FadeGroup(AnimBoolItemGetter fadeGetter, FadeOption options, params IDrawer[] groupDrawers)
        {
            return new FadeGroupsDrawerInternal(fadeGetter, options, groupDrawers);
        }

        public static IDrawer FoldoutGroup(string title, AnimBoolGetter root, FoldoutOption options, params IDrawer[] bodies)
        {
            return new FoldoutDrawerInternal(title, root, options, bodies);
        }

        public static IDrawer Select<T2UIState, T2Data>(
            StateSelect<T2UIState> stateSelect,
            DataSelect<T2Data> dataSelect,
            params CoreEditorDrawer<T2UIState, T2Data>.IDrawer[] otherDrawers)
        {
            return new SelectDrawerInternal<T2UIState, T2Data>(stateSelect, dataSelect, otherDrawers);
        }

        class GroupDrawerInternal : IDrawer
        {
            IDrawer[] drawers { get; set; }
            public GroupDrawerInternal(params IDrawer[] drawers)
            {
                this.drawers = drawers;
            }

            void IDrawer.Draw(TUIState s, TData p, Editor owner)
            {
                for (var i = 0; i < drawers.Length; i++)
                    drawers[i].Draw(s, p, owner);
            }
        }

        class SelectDrawerInternal<T2UIState, T2Data> : IDrawer
        {
            StateSelect<T2UIState> m_StateSelect;
            DataSelect<T2Data> m_DataSelect;
            CoreEditorDrawer<T2UIState, T2Data>.IDrawer[] m_SourceDrawers;

            public SelectDrawerInternal(StateSelect<T2UIState> stateSelect,
                                        DataSelect<T2Data> dataSelect,
                                        params CoreEditorDrawer<T2UIState, T2Data>.IDrawer[] otherDrawers)
            {
                m_SourceDrawers = otherDrawers;
                m_StateSelect = stateSelect;
                m_DataSelect = dataSelect;
            }

            void IDrawer.Draw(TUIState s, TData p, Editor o)
            {
                var s2 = m_StateSelect(s, p, o);
                var p2 = m_DataSelect(s, p, o);
                for (var i = 0; i < m_SourceDrawers.Length; i++)
                    m_SourceDrawers[i].Draw(s2, p2, o);
            }
        }

        class ActionDrawerInternal : IDrawer
        {
            ActionDrawer[] actionDrawers { get; set; }
            public ActionDrawerInternal(params ActionDrawer[] actionDrawers)
            {
                this.actionDrawers = actionDrawers;
            }

            void IDrawer.Draw(TUIState s, TData p, Editor owner)
            {
                for (var i = 0; i < actionDrawers.Length; i++)
                    actionDrawers[i](s, p, owner);
            }
        }

        class FadeGroupsDrawerInternal : IDrawer
        {
            IDrawer[] m_GroupDrawers;
            AnimBoolItemGetter m_Getter;
            FadeOption m_Options;

            bool indent { get { return (m_Options & FadeOption.Indent) != 0; } }
            bool animate { get { return (m_Options & FadeOption.Animate) != 0; } }

            public FadeGroupsDrawerInternal(AnimBoolItemGetter getter, FadeOption options, params IDrawer[] groupDrawers)
            {
                m_GroupDrawers = groupDrawers;
                m_Getter = getter;
                m_Options = options;
            }

            void IDrawer.Draw(TUIState s, TData p, Editor owner)
            {
                // We must start with a layout group here
                // Otherwise, nested FadeGroup won't work
                GUILayout.BeginVertical();
                for (var i = 0; i < m_GroupDrawers.Length; ++i)
                {
                    var b = m_Getter(s, p, owner, i);
                    if (animate && EditorGUILayout.BeginFadeGroup(b.faded)
                        || !animate && b.target)
                    {
                        if (indent)
                            ++EditorGUI.indentLevel;
                        m_GroupDrawers[i].Draw(s, p, owner);
                        if (indent)
                            --EditorGUI.indentLevel;
                    }
                    if (animate)
                        EditorGUILayout.EndFadeGroup();
                }
                GUILayout.EndVertical();
            }
        }

        class FoldoutDrawerInternal : IDrawer
        {
            IDrawer[] m_Bodies;
            AnimBoolGetter m_IsExpanded;
            string m_Title;
            FoldoutOption m_Options;

            bool animate { get { return (m_Options & FoldoutOption.Animate) != 0; } }
            bool indent { get { return (m_Options & FoldoutOption.Indent) != 0; } }
            bool boxed { get { return (m_Options & FoldoutOption.Boxed) != 0; } }

            public FoldoutDrawerInternal(string title, AnimBoolGetter isExpanded, FoldoutOption options, params IDrawer[] bodies)
            {
                m_Title = title;
                m_IsExpanded = isExpanded;
                m_Bodies = bodies;
                m_Options = options;
            }

            public void Draw(TUIState s, TData p, Editor owner)
            {
                var r = m_IsExpanded(s, p, owner);
                CoreEditorUtils.DrawSplitter(boxed);
                r.target = CoreEditorUtils.DrawHeaderFoldout(m_Title, r.target, boxed);
                // We must start with a layout group here
                // Otherwise, nested FadeGroup won't work
                GUILayout.BeginVertical();
                if (animate && EditorGUILayout.BeginFadeGroup(r.faded)
                    || !animate && r.target)
                {
                    if (indent)
                        ++EditorGUI.indentLevel;
                    for (var i = 0; i < m_Bodies.Length; i++)
                        m_Bodies[i].Draw(s, p, owner);
                    if (indent)
                        --EditorGUI.indentLevel;
                }
                if (animate)
                    EditorGUILayout.EndFadeGroup();
                GUILayout.EndVertical();
            }
        }
    }

    public static class CoreEditorDrawersExtensions
    {
        public static void Draw<TUIState, TData>(this IEnumerable<CoreEditorDrawer<TUIState, TData>.IDrawer> drawers, TUIState s, TData p, Editor o)
        {
            foreach (var drawer in drawers)
                drawer.Draw(s, p, o);
        }
    }
}
