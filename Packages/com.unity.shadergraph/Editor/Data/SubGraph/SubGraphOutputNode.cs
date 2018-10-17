using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public class SubGraphOutputControlAttribute : Attribute, IControlAttribute
    {
        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (!(node is SubGraphOutputNode))
                throw new ArgumentException("Node must inherit from AbstractSubGraphIONode.", "node");
            return new SubGraphOutputControlView((SubGraphOutputNode)node);
        }
    }

    public class SubGraphOutputControlView : VisualElement
    {
        SubGraphOutputNode m_Node;

        public SubGraphOutputControlView(SubGraphOutputNode node)
        {
            m_Node = node;
            Add(new Button(OnAdd) { text = "Add Slot" });
            Add(new Button(OnRemove) { text = "Remove Slot" });
        }

        void OnAdd()
        {
            m_Node.AddSlot();
        }

        void OnRemove()
        {
            // tell the user that they might cchange things up.
            if (EditorUtility.DisplayDialog("Sub Graph Will Change", "If you remove a slot and save the sub graph, you might change other graphs that are using this sub graph.\n\nDo you want to continue?", "Yes", "No"))
            {
                m_Node.owner.owner.RegisterCompleteObjectUndo("Removing Slot");
                m_Node.RemoveSlot();
            }
        }
    }

    public class SubGraphOutputNode : AbstractMaterialNode
    {
        [SubGraphOutputControl]
        int controlDummy { get; set; }

        public SubGraphOutputNode()
        {
            name = "SubGraphOutputs";
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public ShaderStageCapability effectiveShaderStage
        {
            get
            {
                List<MaterialSlot> slots = new List<MaterialSlot>();
                GetInputSlots(slots);

                foreach(MaterialSlot slot in slots)
                {
                    ShaderStageCapability stage = NodeUtils.GetEffectiveShaderStageCapability(slot, true);

                    if(stage != ShaderStageCapability.All)
                        return stage;
                }
                
                return ShaderStageCapability.All;
            }
        }

        private void ValidateShaderStage()
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetInputSlots(slots);

            foreach(MaterialSlot slot in slots)
                slot.stageCapability = ShaderStageCapability.All;

            var effectiveStage = effectiveShaderStage;
            
            foreach(MaterialSlot slot in slots)
                slot.stageCapability = effectiveStage;
        }

        public override void ValidateNode()
        {
            ValidateShaderStage();

            base.ValidateNode();
        }

        public virtual int AddSlot()
        {
            var index = this.GetInputSlots<ISlot>().Count() + 1;
            AddSlot(new Vector4MaterialSlot(index, "Output " + index, "Output" + index, SlotType.Input, Vector4.zero));
            return index;
        }

        public virtual void RemoveSlot()
        {
            var index = this.GetInputSlots<ISlot>().Count();
            if (index == 0)
                return;

            RemoveSlot(index);
        }

        public void RemapOutputs(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var slot in graphOutputs)
                visitor.AddShaderChunk(string.Format("{0} = {1};", slot.shaderOutputName, GetSlotValue(slot.id, generationMode)), true);
        }

        public IEnumerable<MaterialSlot> graphOutputs
        {
            get
            {
                return NodeExtensions.GetInputSlots<MaterialSlot>(this).OrderBy(x => x.id);
            }
        }
    }
}
