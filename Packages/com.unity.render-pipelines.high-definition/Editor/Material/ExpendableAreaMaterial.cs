using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    //should be base for al material in hdrp. It will add the collapsable mecanisme on them
    public abstract class ExpendableAreaMaterial : ShaderGUI
    {
        protected interface IExpendableArea
        {
            bool GetExpendedAreas(uint mask);
            void SetExpendedAreas(uint mask, bool value);
        }

        protected abstract uint expendedState { get; set; }

        protected struct HeaderScope : IDisposable
        {
            public readonly bool expended;
            private bool spaceAtEnd;

            public HeaderScope(string title, uint bitExpended, ExpendableAreaMaterial owner, bool spaceAtEnd = true, Color colorDot = default(Color))
            {
                bool beforeExpended = owner.GetExpendedAreas(bitExpended);

                this.spaceAtEnd = spaceAtEnd;
                CoreEditorUtils.DrawSplitter();
                GUILayout.BeginVertical();

                bool saveChangeState = GUI.changed;
                if (colorDot != default(Color))
                    title = "   " + title;
                expended = CoreEditorUtils.DrawHeaderFoldout(title, beforeExpended);
                if (colorDot != default(Color))
                {
                    Color previousColor = GUI.contentColor;
                    GUI.contentColor = colorDot;
                    Rect headerRect = GUILayoutUtility.GetLastRect();
                    headerRect.xMin += 16f;
                    EditorGUI.LabelField(headerRect, "■");
                    GUI.contentColor = previousColor;
                }
                if (expended ^ beforeExpended)
                {
                    owner.SetExpendedAreas((uint)bitExpended, expended);
                    saveChangeState = true;
                }
                GUI.changed = saveChangeState;

                if (expended)
                    ++EditorGUI.indentLevel;
            }

            void IDisposable.Dispose()
            {
                if (expended)
                {
                    if (spaceAtEnd)
                        EditorGUILayout.Space();
                    --EditorGUI.indentLevel;
                }
                GUILayout.EndVertical();
            }
        }

        protected bool GetExpendedAreas(uint mask)
        {
            uint state = expendedState;
            bool result = (state & mask) > 0;
            return result;
        }

        protected void SetExpendedAreas(uint mask, bool value)
        {
            uint state = expendedState;

            if (value)
            {
                state |= mask;
            }
            else
            {
                mask = ~mask;
                state &= mask;
            }

            expendedState = state;
        }
    }
}
