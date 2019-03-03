using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    //should be base for al material in hdrp. It will add the collapsable mecanisme on them
    abstract class ExpandableAreaMaterial : ShaderGUI
    {
        private const string k_KeyPrefix = "HDRP:Material:UI_State:";
        private string m_StateKey;
        
        protected virtual uint expandedState
        {
            get
            {
                return (uint)EditorPrefs.GetInt(m_StateKey);
            }
            set
            {
                EditorPrefs.SetInt(m_StateKey, (int)value);
            }
        }
        
        protected virtual uint defaultExpandedState { get { return 0xFFFFFFFF; } } //all opened by default

        protected struct HeaderScope : IDisposable
        {
            public readonly bool expanded;
            private bool spaceAtEnd;

            public HeaderScope(string title, uint bitExpanded, ExpandableAreaMaterial owner, bool spaceAtEnd = true, Color colorDot = default(Color))
            {
                bool beforeExpended = owner.GetExpandedAreas(bitExpanded);

                this.spaceAtEnd = spaceAtEnd;
                CoreEditorUtils.DrawSplitter();
                GUILayout.BeginVertical();

                bool saveChangeState = GUI.changed;
                if (colorDot != default(Color))
                    title = "   " + title;
                expanded = CoreEditorUtils.DrawHeaderFoldout(title, beforeExpended);
                if (colorDot != default(Color))
                {
                    Color previousColor = GUI.contentColor;
                    GUI.contentColor = colorDot;
                    Rect headerRect = GUILayoutUtility.GetLastRect();
                    headerRect.xMin += 16f;
                    EditorGUI.LabelField(headerRect, "■");
                    GUI.contentColor = previousColor;
                }
                if (expanded ^ beforeExpended)
                {
                    owner.SetExpandedAreas((uint)bitExpanded, expanded);
                    saveChangeState = true;
                }
                GUI.changed = saveChangeState;

                if (expanded)
                    ++EditorGUI.indentLevel;
            }

            void IDisposable.Dispose()
            {
                if (expanded)
                {
                    if (spaceAtEnd)
                        EditorGUILayout.Space();
                    --EditorGUI.indentLevel;
                }
                GUILayout.EndVertical();
            }
        }

        protected bool GetExpandedAreas(uint mask)
        {
            uint state = expandedState;
            bool result = (state & mask) > 0;
            return result;
        }

        protected void SetExpandedAreas(uint mask, bool value)
        {
            uint state = expandedState;

            if (value)
            {
                state |= mask;
            }
            else
            {
                mask = ~mask;
                state &= mask;
            }

            expandedState = state;
        }

        protected void InitExpandableState(MaterialEditor editor)
        {
            m_StateKey = k_KeyPrefix + ((Material)editor.target).shader.name;
            if(!EditorPrefs.HasKey(m_StateKey))
            {
                EditorPrefs.SetInt(m_StateKey, (int)defaultExpandedState);
            }
        }
    }
}
