using System;
using System.Globalization;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class MultiFloatSlotControlView : VisualElement
    {
        readonly INode m_Node;
        readonly Func<Vector4> m_Get;
        readonly Action<Vector4> m_Set;
        int m_UndoGroup = -1;

        public MultiFloatSlotControlView(INode node, string[] labels, Func<Vector4> get, Action<Vector4> set)
        {
            AddStyleSheetPath("Styles/Controls/MultiFloatSlotControlView");
            m_Node = node;
            m_Get = get;
            m_Set = set;
            var initialValue = get();
            for (var i = 0; i < labels.Length; i++)
                AddField(initialValue, i, labels[i]);
        }

        void AddField(Vector4 initialValue, int index, string subLabel)
        {
            var dummy = new VisualElement { name = "dummy" };
            var label = new Label(subLabel);
            dummy.Add(label);
            Add(dummy);
            var field = new FloatField { userData = index, value = initialValue[index] };
            var dragger = new FieldMouseDragger<double>(field);
            dragger.SetDragZone(label);
            field.OnValueChanged(evt =>
                {
                    var value = m_Get();
                    value[index] = (float)evt.newValue;
                    m_Set(value);
                    m_Node.Dirty(ModificationScope.Node);
                    m_UndoGroup = -1;
                });
            field.RegisterCallback<InputEvent>(evt =>
                {
                    if (m_UndoGroup == -1)
                    {
                        m_UndoGroup = Undo.GetCurrentGroup();
                        m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                    }
                    float newValue;
                    if (!float.TryParse(evt.newData, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out newValue))
                        newValue = 0f;
                    var value = m_Get();
                    if (Math.Abs(value[index] - newValue) > 1e-9)
                    {
                        value[index] = newValue;
                        m_Set(value);
                        m_Node.Dirty(ModificationScope.Node);
                    }
                });
            field.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Escape && m_UndoGroup > -1)
                    {
                        Undo.RevertAllDownToGroup(m_UndoGroup);
                        m_UndoGroup = -1;
                        evt.StopPropagation();
                    }
                    this.MarkDirtyRepaint();
                });
            Add(field);
        }
    }
}
