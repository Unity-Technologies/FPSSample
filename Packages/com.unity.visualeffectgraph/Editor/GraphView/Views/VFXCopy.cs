using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using System.Reflection;

using NodeID = System.UInt32;

namespace UnityEditor.VFX.UI
{
    class VFXCopy : VFXCopyPasteCommon
    {
        VFXContextController[] contexts;
        VFXOperatorController[] operators;
        VFXParameterNodeController[] parameters;
        VFXData[] datas;
        Dictionary<VFXNodeController, uint> modelIndices = new Dictionary<VFXNodeController, NodeID>();

        static VFXCopy s_Instance;

        public static string SerializeElements(IEnumerable<Controller> elements, Rect bounds)
        {
            if (s_Instance == null)
                s_Instance = new VFXCopy();
            var serializableGraph = s_Instance.CreateCopy(elements, bounds) as SerializableGraph;

            return JsonUtility.ToJson(serializableGraph);
        }

        public object CreateCopy(IEnumerable<Controller> elements, Rect bounds)
        {
            IEnumerable<VFXContextController> contexts = elements.OfType<VFXContextController>();
            IEnumerable<VFXNodeController> nodes = elements.Where(t => t is VFXOperatorController || t is VFXParameterNodeController).Cast<VFXNodeController>();
            IEnumerable<VFXBlockController> blocks = elements.OfType<VFXBlockController>();

            SerializableGraph serializableGraph = new SerializableGraph();

            if (contexts.Count() == 0 && nodes.Count() == 0 && blocks.Count() > 0)
            {
                var copiedBlocks = new List<VFXBlockController>(blocks);
                modelIndices.Clear();
                serializableGraph.operatorsOrBlocks = CopyBlocks(copiedBlocks, 0);
                serializableGraph.blocksOnly = true;
            }
            else
            {
                CopyNodes(serializableGraph, elements, contexts, nodes, bounds);
            }

            return serializableGraph;
        }

        void CopyNodes(SerializableGraph serializableGraph, IEnumerable<Controller> elements, IEnumerable<VFXContextController> copiedContexts, IEnumerable<VFXNodeController> nodes, Rect bounds)
        {
            serializableGraph.bounds = bounds;
            IEnumerable<VFXNodeController> dataEdgeTargets = nodes.Concat(copiedContexts.Cast<VFXNodeController>()).Concat(copiedContexts.SelectMany(t => t.blockControllers).Cast<VFXNodeController>()).ToArray();

            // consider only edges contained in the selection

            IEnumerable<VFXDataEdgeController> dataEdges = elements.OfType<VFXDataEdgeController>().Where(t =>
                dataEdgeTargets.Contains((t.input as VFXDataAnchorController).sourceNode as VFXNodeController) &&
                dataEdgeTargets.Contains((t.output as VFXDataAnchorController).sourceNode as VFXNodeController)).ToArray();
            IEnumerable<VFXFlowEdgeController> flowEdges = elements.OfType<VFXFlowEdgeController>().Where(t =>
                copiedContexts.Contains((t.input as VFXFlowAnchorController).context) &&
                copiedContexts.Contains((t.output as VFXFlowAnchorController).context)
                ).ToArray();


            modelIndices.Clear();
            contexts = copiedContexts.ToArray();
            operators = nodes.OfType<VFXOperatorController>().ToArray();
            parameters = nodes.OfType<VFXParameterNodeController>().ToArray();

            datas = contexts.Select(t => t.model.GetData()).Where(t => t != null).ToArray();

            CopyOperatorsAndContexts(ref serializableGraph);

            CopyParameters(ref serializableGraph);

            CopyGroupNodesAndStickyNotes(ref serializableGraph, elements);

            CopyDatas(ref serializableGraph);

            CopyDataEdge(ref serializableGraph, dataEdges);

            CopyFlowEdges(ref serializableGraph, flowEdges);
        }

        void CopyGroupNodesAndStickyNotes(ref SerializableGraph serializableGraph, IEnumerable<Controller> elements)
        {
            VFXGroupNodeController[] groupNodes = elements.OfType<VFXGroupNodeController>().ToArray();
            VFXStickyNoteController[] stickyNotes = elements.OfType<VFXStickyNoteController>().ToArray();

            if (groupNodes.Length > 0 || stickyNotes.Length > 0)
            {
                var stickyNodeIndexToCopiedIndex = new Dictionary<int, int>();

                if (stickyNotes.Length > 0)
                {
                    serializableGraph.stickyNotes = new VFXUI.StickyNoteInfo[stickyNotes.Length];

                    for (int i = 0; i < stickyNotes.Length; ++i)
                    {
                        VFXStickyNoteController stickyNote = stickyNotes[i];
                        stickyNodeIndexToCopiedIndex[stickyNote.index] = i;
                        VFXUI.StickyNoteInfo info = stickyNote.model.stickyNoteInfos[stickyNote.index];
                        serializableGraph.stickyNotes[i] = new VFXUI.StickyNoteInfo(info);
                    }
                }

                if (groupNodes.Length > 0)
                {
                    serializableGraph.groupNodes = new GroupNode[groupNodes.Length];
                    for (int i = 0; i < groupNodes.Length; ++i)
                    {
                        VFXGroupNodeController groupNode = groupNodes[i];
                        VFXUI.GroupInfo info = groupNode.model.groupInfos[groupNode.index];

                        serializableGraph.groupNodes[i] = new GroupNode { infos = new VFXUI.UIInfo(info)};

                        // only keep nodes and sticky notes that are copied because a element can not be in two groups at the same time.
                        if (info.contents != null)
                        {
                            var nodeIndices = groupNode.nodes.OfType<VFXNodeController>().Where(t => contexts.Contains(t) || operators.Contains(t) || parameters.Contains(t)).Select(t => modelIndices[t]);
                            var stickNoteIndices = info.contents.Where(t => t.isStickyNote && stickyNodeIndexToCopiedIndex.ContainsKey(t.id)).Select(t => (uint)stickyNodeIndexToCopiedIndex[t.id]);

                            serializableGraph.groupNodes[i].contents = nodeIndices.Concat(stickNoteIndices).ToArray();
                            serializableGraph.groupNodes[i].stickNodeCount = stickNoteIndices.Count();
                        }
                    }
                }
            }
        }

        void CopyDataEdge(ref SerializableGraph serializableGraph, IEnumerable<VFXDataEdgeController> dataEdges)
        {
            serializableGraph.dataEdges = new DataEdge[dataEdges.Count()];
            int cpt = 0;
            foreach (var edge in dataEdges)
            {
                DataEdge copyPasteEdge = new DataEdge();

                var inputController = edge.input as VFXDataAnchorController;
                var outputController = edge.output as VFXDataAnchorController;

                copyPasteEdge.input.slotPath = MakeSlotPath(inputController.model, true);
                copyPasteEdge.input.targetIndex = modelIndices[inputController.sourceNode];

                copyPasteEdge.output.slotPath = MakeSlotPath(outputController.model, false);
                copyPasteEdge.output.targetIndex = modelIndices[outputController.sourceNode];
                serializableGraph.dataEdges[cpt++] = copyPasteEdge;
            }
        }

        void CopyFlowEdges(ref SerializableGraph serializableGraph, IEnumerable<VFXFlowEdgeController> flowEdges)
        {
            serializableGraph.flowEdges = new FlowEdge[flowEdges.Count()];
            int cpt = 0;
            foreach (var edge in flowEdges)
            {
                FlowEdge copyPasteEdge = new FlowEdge();

                var inputController = edge.input as VFXFlowAnchorController;
                var outputController = edge.output as VFXFlowAnchorController;

                copyPasteEdge.input.contextIndex = modelIndices[inputController.context];
                copyPasteEdge.input.flowIndex = inputController.slotIndex;
                copyPasteEdge.output.contextIndex = modelIndices[outputController.context];
                copyPasteEdge.output.flowIndex = outputController.slotIndex;

                serializableGraph.flowEdges[cpt++] = copyPasteEdge;
            }
        }

        void CopyDatas(ref SerializableGraph serializableGraph)
        {
            serializableGraph.datas = new Data[datas.Length];
            for (int i = 0; i < datas.Length; ++i)
            {
                CopyModelSettings(ref serializableGraph.datas[i].settings, datas[i]);
            }
        }

        void CopyModelSettings(ref Property[] properties, VFXModel model)
        {
            // Copy all fields that are either VFXSettings or serialized by unity
            Type type = model.GetType();

            var fields = GetFields(type);

            properties = new Property[fields.Count()];

            for (int i = 0; i < properties.Length; ++i)
            {
                properties[i].name = fields[i].Name;
                properties[i].value = new VFXSerializableObject(fields[i].FieldType, fields[i].GetValue(model));
            }
        }

        ParameterNode CopyParameterNode(int parameterIndex, int nodeIndex, VFXParameterNodeController controller)
        {
            ParameterNode n = new ParameterNode();
            n.position = controller.position;
            n.collapsed = controller.superCollapsed;
            n.expandedOutput = controller.infos.expandedSlots.Select(t => t.path).ToArray();

            if (parameterIndex < (1 << 18) && nodeIndex < (1 << 11))
                modelIndices[controller] = GetParameterNodeID((uint)parameterIndex, (uint)nodeIndex);
            else
                modelIndices[controller] = InvalidID;
            return n;
        }

        void CopyParameters(ref SerializableGraph serializableGraph)
        {
            int cpt = 0;
            serializableGraph.parameters = parameters.GroupBy(t => t.parentController, t => t, (p, c) =>
            {
                ++cpt;

                return new Parameter()
                {
                    originalInstanceID = p.model.GetInstanceID(),
                    name = p.model.exposedName,
                    value = new VFXSerializableObject(p.model.type, p.model.value),
                    exposed = p.model.exposed,
                    range = p.hasRange,
                    min = p.hasRange ? p.model.m_Min : null,
                    max = p.hasRange ? p.model.m_Max : null,
                    tooltip = p.model.tooltip,
                    nodes = c.Select((u, i) => CopyParameterNode(cpt - 1, i, u)).ToArray()
                };
            }
                ).ToArray();
        }

        void CopyOperatorsAndContexts(ref SerializableGraph serializableGraph)
        {
            serializableGraph.contexts = new Context[contexts.Length];

            for (int i = 0; i < contexts.Length; ++i)
            {
                NodeID id = CopyContext(ref serializableGraph.contexts[i], contexts[i], i);
                modelIndices[contexts[i]] = id;
            }

            serializableGraph.operatorsOrBlocks = new Node[operators.Length];

            for (int i = 0; i < operators.Length; ++i)
            {
                uint id = CopyNode(ref serializableGraph.operatorsOrBlocks[i], operators[i].model, (NodeID)i);
                modelIndices[operators[i]] = id;
            }
        }

        NodeID CopyNode(ref Node node, VFXModel model, uint index)
        {
            // Copy node infos
            node.position = model.position;
            node.type = model.GetType();
            node.flags = 0;
            if (model.collapsed)
                node.flags = Node.Flags.Collapsed;
            if (model.superCollapsed)
                node.flags = Node.Flags.SuperCollapsed;

            uint id = 0;
            if (model is VFXOperator)
            {
                id = OperatorFlag;
            }
            else if (model is VFXContext)
            {
                id = ContextFlag;
            }

            id |= (uint)index;

            //Copy settings value
            CopyModelSettings(ref node.settings, model);

            var inputSlots = (model as IVFXSlotContainer).inputSlots;
            node.inputSlots = new Property[inputSlots.Count];
            for (int i = 0; i < inputSlots.Count; i++)
            {
                node.inputSlots[i].name = inputSlots[i].name;
                node.inputSlots[i].value = new VFXSerializableObject(inputSlots[i].property.type, inputSlots[i].value);
            }

            node.expandedInputs = AllSlots(inputSlots).Where(t => !t.collapsed).Select(t => t.path).ToArray();
            node.expandedOutputs = AllSlots((model as IVFXSlotContainer).outputSlots).Where(t => !t.collapsed).Select(t => t.path).ToArray();

            return id;
        }

        Node[] CopyBlocks(IList<VFXBlockController> blocks, int contextIndex)
        {
            var newBlocks = new Node[blocks.Count];
            for (uint i = 0; i < newBlocks.Length; ++i)
            {
                CopyNode(ref newBlocks[(int)i], blocks[(int)i].model, i);
                if (blocks[(int)i].model.enabled)
                {
                    newBlocks[i].flags |= Node.Flags.Enabled;
                }

                if (contextIndex < (1 << 18) && i < (1 << 11))
                    modelIndices[blocks[(int)i]] = BlockFlag | (i << 18) | (uint)contextIndex;
                else
                    modelIndices[blocks[(int)i]] = InvalidID;
            }

            return newBlocks;
        }

        NodeID CopyContext(ref Context context, VFXContextController controller, int index)
        {
            NodeID id = CopyNode(ref context.node, controller.model, (NodeID)index);

            var blocks = controller.blockControllers;

            context.label = controller.model.label;

            if (controller.model.GetData() != null)
                context.dataIndex = Array.IndexOf(datas, controller.model.GetData());
            else
                context.dataIndex = -1;
            context.blocks = CopyBlocks(controller.blockControllers, index);

            return id;
        }

        static int[] MakeSlotPath(VFXSlot slot, bool input)
        {
            List<int> slotPath = new List<int>(slot.depth + 1);
            while (slot.GetParent() != null)
            {
                slotPath.Add(slot.GetParent().GetIndex(slot));
                slot = slot.GetParent();
            }
            slotPath.Add((input ? (slot.owner as IVFXSlotContainer).inputSlots : (slot.owner as IVFXSlotContainer).outputSlots).IndexOf(slot));

            return slotPath.ToArray();
        }
    }
}
