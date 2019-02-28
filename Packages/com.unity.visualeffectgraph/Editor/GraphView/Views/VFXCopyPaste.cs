using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.VFX.UI
{
    class VFXCopyPaste
    {
        [System.Serializable]
        struct DataAnchor
        {
            public int targetIndex;
            public int[] slotPath;
        }

        [System.Serializable]
        struct DataEdge
        {
            public bool inputContext;
            public bool outputParameter;
            public int inputBlockIndex;
            public int outputParameterIndex;
            public int outputParameterNodeIndex;
            public DataAnchor input;
            public DataAnchor output;
        }

        [System.Serializable]
        struct FlowAnchor
        {
            public int contextIndex;
            public int flowIndex;
        }


        [System.Serializable]
        struct FlowEdge
        {
            public FlowAnchor input;
            public FlowAnchor output;
        }

        [System.Serializable]
        struct DataAndContexts
        {
            public int dataIndex;
            public int[] contextsIndexes;
        }

        [System.Serializable]
        struct Parameter
        {
            public int originalInstanceID;
            [NonSerialized]
            public VFXParameter parameter;
            [NonSerialized]
            public VFXParameter copiedParameter;
            public int index;
            public int infoIndexOffset;
            public VFXParameter.Node[] infos;
            [NonSerialized]
            public Dictionary<int, int> idMap;
        }

        [System.Serializable]
        class Data
        {
            public string serializedObjects;
            public Rect bounds;

            public bool blocksOnly;

            [NonSerialized]
            public VFXContext[] contexts;


            [NonSerialized]
            public VFXModel[] slotContainers;
            [NonSerialized]
            public VFXBlock[] blocks;

            public Parameter[] parameters;

            public DataAndContexts[] dataAndContexts;
            public DataEdge[] dataEdges;
            public FlowEdge[] flowEdges;


            public void CollectDependencies(HashSet<ScriptableObject> objects)
            {
                if (contexts != null)
                {
                    foreach (var context in contexts)
                    {
                        objects.Add(context);
                        context.CollectDependencies(objects);
                    }
                }
                if (slotContainers != null)
                {
                    foreach (var slotContainer in slotContainers)
                    {
                        objects.Add(slotContainer);
                        slotContainer.CollectDependencies(objects);
                    }
                }
                if (blocks != null)
                {
                    foreach (var block in blocks)
                    {
                        objects.Add(block);
                        block.CollectDependencies(objects);
                    }
                }
            }
        }

        static ScriptableObject[] PrepareSerializedObjects(Data copyData, VFXUI optionalUI)
        {
            var objects = new HashSet<ScriptableObject>();
            copyData.CollectDependencies(objects);

            if (optionalUI != null)
            {
                objects.Add(optionalUI);
            }

            ScriptableObject[] allSerializedObjects = objects.OfType<ScriptableObject>().ToArray();

            copyData.serializedObjects = VFXMemorySerializer.StoreObjects(allSerializedObjects);

            return allSerializedObjects;
        }

        static VFXUI CopyGroupNodesAndStickyNotes(IEnumerable<Controller> elements, VFXContext[] copiedContexts, VFXModel[] copiedSlotContainers)
        {
            VFXGroupNodeController[] groupNodes = elements.OfType<VFXGroupNodeController>().ToArray();
            VFXStickyNoteController[] stickyNotes = elements.OfType<VFXStickyNoteController>().ToArray();

            VFXUI copiedGroupUI = null;
            if (groupNodes.Length > 0 || stickyNotes.Length > 0)
            {
                copiedGroupUI = ScriptableObject.CreateInstance<VFXUI>();

                var stickyNodeIndexToCopiedIndex = new Dictionary<int, int>();

                if (stickyNotes.Length > 0)
                {
                    copiedGroupUI.stickyNoteInfos = new VFXUI.StickyNoteInfo[stickyNotes.Length];

                    for (int i = 0; i < stickyNotes.Length; ++i)
                    {
                        VFXStickyNoteController stickyNote = stickyNotes[i];
                        stickyNodeIndexToCopiedIndex[stickyNote.index] = i;
                        VFXUI.StickyNoteInfo info = stickyNote.model.stickyNoteInfos[stickyNote.index];
                        copiedGroupUI.stickyNoteInfos[i] = new VFXUI.StickyNoteInfo(info);
                    }
                }

                if (groupNodes.Length > 0)
                {
                    copiedGroupUI.groupInfos = new VFXUI.GroupInfo[groupNodes.Length];

                    for (int i = 0; i < groupNodes.Length; ++i)
                    {
                        VFXGroupNodeController groupNode = groupNodes[i];
                        VFXUI.GroupInfo info = groupNode.model.groupInfos[groupNode.index];
                        copiedGroupUI.groupInfos[i] = new VFXUI.GroupInfo(info);

                        // only keep nodes and sticky notes that are copied because a element can not be in two groups at the same time.
                        if (info.contents != null)
                        {
                            var groupInfo = copiedGroupUI.groupInfos[i];
                            groupInfo.contents = info.contents.Where(t => copiedContexts.Contains(t.model) || copiedSlotContainers.Contains(t.model) || (t.isStickyNote && stickyNodeIndexToCopiedIndex.ContainsKey(t.id))).ToArray();

                            for (int j = 0; j < groupInfo.contents.Length; ++j)
                            {
                                if (groupInfo.contents[j].isStickyNote)
                                {
                                    groupInfo.contents[j].id = stickyNodeIndexToCopiedIndex[groupInfo.contents[j].id];
                                }
                            }
                        }
                    }
                }
            }
            return copiedGroupUI;
        }

        static void CopyDataEdge(Data copyData, IEnumerable<VFXDataEdgeController> dataEdges, ScriptableObject[] allSerializedObjects)
        {
            copyData.dataEdges = new DataEdge[dataEdges.Count()];
            int cpt = 0;

            var orderedEdges = new List<VFXDataEdgeController>();

            var edges = new HashSet<VFXDataEdgeController>(dataEdges);

            // Ensure that operators that can change shape always all their input edges created before their output edges and in the same order
            bool sortFailed = false;
            try
            {
                while (edges.Count > 0)
                {
                    var edgeInputs = edges.GroupBy(t => t.input.sourceNode).ToDictionary(t => t.Key, t => t.Select(u => u));

                    //Select the edges that have an input node which all its input edges have an output node that have no input edge
                    // Order them by index

                    var edgesWithoutParent = edges.Where(t => !edgeInputs[t.input.sourceNode].Any(u => edgeInputs.ContainsKey(u.output.sourceNode))).OrderBy(t => t.input.model.GetMasterSlot().owner.GetSlotIndex(t.input.model.GetMasterSlot())).ToList();
                    /*foreach(var gen in edgesWithoutParent)
                    {
                        int index = gen.input.model.GetMasterSlot().owner.GetSlotIndex(gen.input.model.GetMasterSlot());
                        Debug.Log("Edge with input:" + gen.input.sourceNode.title + "index"+ index);
                    }*/
                    orderedEdges.AddRange(edgesWithoutParent);

                    int count = edges.Count;
                    foreach (var e in edgesWithoutParent)
                    {
                        edges.Remove(e);
                    }
                    if (edges.Count >= count)
                    {
                        sortFailed = true;
                        Debug.LogError("Sorting of data edges failed. Please provide a screenshot of the graph with the selected node to @tristan");
                        break;
                    }
                    //Debug.Log("------------------------------");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Sorting of data edges threw. Please provide a screenshot of the graph with the selected node to @tristan" + e.Message);
                sortFailed = true;
            }

            IEnumerable<VFXDataEdgeController> usedEdges = sortFailed ? dataEdges : orderedEdges;

            foreach (var edge in usedEdges)
            {
                DataEdge copyPasteEdge = new DataEdge();

                var inputController = edge.input as VFXDataAnchorController;
                var outputController = edge.output as VFXDataAnchorController;

                copyPasteEdge.input.slotPath = MakeSlotPath(inputController.model, true);

                if (inputController.model.owner is VFXContext)
                {
                    VFXContext context = inputController.model.owner as VFXContext;
                    copyPasteEdge.inputContext = true;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(allSerializedObjects, context);
                    copyPasteEdge.inputBlockIndex = -1;
                }
                else if (inputController.model.owner is VFXBlock)
                {
                    VFXBlock block = inputController.model.owner as VFXBlock;
                    copyPasteEdge.inputContext = true;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(allSerializedObjects, block.GetParent());
                    copyPasteEdge.inputBlockIndex = block.GetParent().GetIndex(block);
                }
                else
                {
                    copyPasteEdge.inputContext = false;
                    copyPasteEdge.input.targetIndex = System.Array.IndexOf(allSerializedObjects, inputController.model.owner as VFXModel);
                    copyPasteEdge.inputBlockIndex = -1;
                }

                if (outputController.model.owner is VFXParameter)
                {
                    copyPasteEdge.outputParameter = true;
                    copyPasteEdge.outputParameterIndex = System.Array.FindIndex(copyData.parameters, t => (IVFXSlotContainer)t.parameter == outputController.model.owner);
                    copyPasteEdge.outputParameterNodeIndex = System.Array.IndexOf(copyData.parameters[copyPasteEdge.outputParameterIndex].infos, (outputController.sourceNode as VFXParameterNodeController).infos);
                }
                else
                {
                    copyPasteEdge.outputParameter = false;
                }

                copyPasteEdge.output.slotPath = MakeSlotPath(outputController.model, false);
                copyPasteEdge.output.targetIndex = System.Array.IndexOf(allSerializedObjects, outputController.model.owner as VFXModel);

                copyData.dataEdges[cpt++] = copyPasteEdge;
            }
            // Sort the edge so the one that links the node that have the least links
        }

        static void CopyFlowEdges(Data copyData, IEnumerable<VFXFlowEdgeController> flowEdges, ScriptableObject[] allSerializedObjects)
        {
            copyData.flowEdges = new FlowEdge[flowEdges.Count()];
            int cpt = 0;
            foreach (var edge in flowEdges)
            {
                FlowEdge copyPasteEdge = new FlowEdge();

                var inputController = edge.input as VFXFlowAnchorController;
                var outputController = edge.output as VFXFlowAnchorController;

                copyPasteEdge.input.contextIndex = System.Array.IndexOf(allSerializedObjects, inputController.owner);
                copyPasteEdge.input.flowIndex = inputController.slotIndex;
                copyPasteEdge.output.contextIndex = System.Array.IndexOf(allSerializedObjects, outputController.owner);
                copyPasteEdge.output.flowIndex = outputController.slotIndex;

                copyData.flowEdges[cpt++] = copyPasteEdge;
            }
        }

        static void CopyVFXData(Data copyData, VFXData[] datas, ScriptableObject[] allSerializedObjects, ref VFXContext[] copiedContexts)
        {
            copyData.dataAndContexts = new DataAndContexts[datas.Length];
            for (int i = 0; i < datas.Length; ++i)
            {
                copyData.dataAndContexts[i].dataIndex = System.Array.IndexOf(allSerializedObjects, datas[i]);
                copyData.dataAndContexts[i].contextsIndexes = copiedContexts.Where(t => t.GetData() == datas[i]).Select(t => System.Array.IndexOf(allSerializedObjects, t)).ToArray();
            }
        }

        static void CopyNodes(Data copyData, IEnumerable<Controller> elements, IEnumerable<VFXContextController> contexts, IEnumerable<VFXNodeController> slotContainers, Rect bounds)
        {
            copyData.bounds = bounds;
            IEnumerable<VFXNodeController> dataEdgeTargets = slotContainers.Concat(contexts.Cast<VFXNodeController>()).Concat(contexts.SelectMany(t => t.blockControllers).Cast<VFXNodeController>()).ToArray();

            // consider only edges contained in the selection

            IEnumerable<VFXDataEdgeController> dataEdges = elements.OfType<VFXDataEdgeController>().Where(t => dataEdgeTargets.Contains((t.input as VFXDataAnchorController).sourceNode as VFXNodeController) && dataEdgeTargets.Contains((t.output as VFXDataAnchorController).sourceNode as VFXNodeController)).ToArray();
            IEnumerable<VFXFlowEdgeController> flowEdges = elements.OfType<VFXFlowEdgeController>().Where(t =>
                contexts.Contains((t.input as VFXFlowAnchorController).context) &&
                contexts.Contains((t.output as VFXFlowAnchorController).context)
                ).ToArray();


            VFXContext[] copiedContexts = contexts.Select(t => t.model).ToArray();
            copyData.contexts = copiedContexts;
            VFXModel[] copiedSlotContainers = slotContainers.Select(t => t.model).ToArray();
            copyData.slotContainers = copiedSlotContainers;


            VFXParameterNodeController[] parameters = slotContainers.OfType<VFXParameterNodeController>().ToArray();

            copyData.parameters = parameters.GroupBy(t => t.parentController, t => t.infos, (p, i) => new Parameter() { originalInstanceID = p.model.GetInstanceID(), parameter = p.model, infos = i.ToArray() }).ToArray();

            VFXData[] datas = copiedContexts.Select(t => t.GetData()).Where(t => t != null).ToArray();

            VFXUI copiedGroupUI = CopyGroupNodesAndStickyNotes(elements, copiedContexts, copiedSlotContainers);

            ScriptableObject[] allSerializedObjects = PrepareSerializedObjects(copyData, copiedGroupUI);

            for (int i = 0; i < copyData.parameters.Length; ++i)
            {
                copyData.parameters[i].index = System.Array.IndexOf(allSerializedObjects, copyData.parameters[i].parameter);
            }

            CopyVFXData(copyData, datas, allSerializedObjects, ref copiedContexts);

            CopyDataEdge(copyData, dataEdges, allSerializedObjects);

            CopyFlowEdges(copyData, flowEdges, allSerializedObjects);
        }

        public static object CreateCopy(IEnumerable<Controller> elements, Rect bounds)
        {
            IEnumerable<VFXContextController> contexts = elements.OfType<VFXContextController>();
            IEnumerable<VFXNodeController> slotContainers = elements.Where(t => t is VFXOperatorController || t is VFXParameterNodeController).Cast<VFXNodeController>();
            IEnumerable<VFXBlockController> blocks = elements.OfType<VFXBlockController>();

            Data copyData = new Data();

            if (contexts.Count() == 0 && slotContainers.Count() == 0 && blocks.Count() > 0)
            {
                VFXBlock[] copiedBlocks = blocks.Select(t => t.model).ToArray();
                copyData.blocks = copiedBlocks;
                PrepareSerializedObjects(copyData, null);
                copyData.blocksOnly = true;
            }
            else
            {
                CopyNodes(copyData, elements, contexts, slotContainers, bounds);
            }

            return copyData;
        }

        public static string SerializeElements(IEnumerable<Controller> elements, Rect bounds)
        {
            var copyData = CreateCopy(elements, bounds) as Data;

            return JsonUtility.ToJson(copyData);
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

        static VFXSlot FetchSlot(IVFXSlotContainer container, int[] slotPath, bool input)
        {
            int containerSlotIndex = slotPath[slotPath.Length - 1];

            VFXSlot slot = null;
            if (input)
            {
                if (container.GetNbInputSlots() > containerSlotIndex)
                {
                    slot = container.GetInputSlot(slotPath[slotPath.Length - 1]);
                }
            }
            else
            {
                if (container.GetNbOutputSlots() > containerSlotIndex)
                {
                    slot = container.GetOutputSlot(slotPath[slotPath.Length - 1]);
                }
            }
            if (slot == null)
            {
                return null;
            }

            for (int i = slotPath.Length - 2; i >= 0; --i)
            {
                if (slot.GetNbChildren() > slotPath[i])
                {
                    slot = slot[slotPath[i]];
                }
                else
                {
                    return null;
                }
            }

            return slot;
        }

        public static void UnserializeAndPasteElements(VFXViewController viewController, Vector2 center, string data, VFXView view = null, VFXGroupNodeController groupNode = null)
        {
            var copyData = JsonUtility.FromJson<Data>(data);

            ScriptableObject[] allSerializedObjects = VFXMemorySerializer.ExtractObjects(copyData.serializedObjects, true);

            copyData.contexts = allSerializedObjects.OfType<VFXContext>().ToArray();
            copyData.slotContainers = allSerializedObjects.OfType<IVFXSlotContainer>().Cast<VFXModel>().Where(t => !(t is VFXContext)).ToArray();
            if (copyData.contexts.Length == 0 && copyData.slotContainers.Length == 0)
            {
                copyData.contexts = null;
                copyData.slotContainers = null;
                copyData.blocks = allSerializedObjects.OfType<VFXBlock>().ToArray();
            }

            PasteCopy(viewController, center, copyData, allSerializedObjects, view, groupNode);
        }

        public static void PasteCopy(VFXViewController viewController, Vector2 center, object data, ScriptableObject[] allSerializedObjects, VFXView view, VFXGroupNodeController groupNode)
        {
            Data copyData = (Data)data;

            if (copyData.blocksOnly)
            {
                if (view != null)
                {
                    copyData.blocks = allSerializedObjects.OfType<VFXBlock>().ToArray();
                    PasteBlocks(view, copyData);
                }
            }
            else
            {
                PasteNodes(viewController, center, copyData, allSerializedObjects, view, groupNode);
            }
        }

        static readonly GUIContent m_BlockPasteError = EditorGUIUtility.TextContent("To paste blocks, please select one target block or one target context.");

        static void PasteBlocks(VFXView view, Data copyData)
        {
            var selectedContexts = view.selection.OfType<VFXContextUI>();
            var selectedBlocks = view.selection.OfType<VFXBlockUI>();

            VFXBlockUI targetBlock = null;
            VFXContextUI targetContext = null;

            if (selectedBlocks.Count() > 0)
            {
                targetBlock = selectedBlocks.OrderByDescending(t => t.context.controller.model.GetIndex(t.controller.model)).First();
                targetContext = targetBlock.context;
            }
            else if (selectedContexts.Count() == 1)
            {
                targetContext = selectedContexts.First();
            }
            else
            {
                Debug.LogError(m_BlockPasteError.text);
                return;
            }

            VFXContext targetModelContext = targetContext.controller.model;

            int targetIndex = -1;
            if (targetBlock != null)
            {
                targetIndex = targetModelContext.GetIndex(targetBlock.controller.model) + 1;
            }

            var newBlocks = new HashSet<VFXBlock>();

            foreach (var block in copyData.blocks)
            {
                if (targetModelContext.AcceptChild(block, targetIndex))
                {
                    newBlocks.Add(block);

                    foreach (var slot in block.inputSlots)
                    {
                        slot.UnlinkAll(true, false);
                    }
                    foreach (var slot in block.outputSlots)
                    {
                        slot.UnlinkAll(true, false);
                    }
                    targetModelContext.AddChild(block, targetIndex, false); // only notify once after all blocks have been added
                }
            }

            targetModelContext.Invalidate(VFXModel.InvalidationCause.kStructureChanged);

            // Create all ui based on model
            view.controller.LightApplyChanges();

            view.ClearSelection();

            foreach (var uiBlock in targetContext.Query().OfType<VFXBlockUI>().Where(t => newBlocks.Contains(t.controller.model)).ToList())
            {
                view.AddToSelection(uiBlock);
            }
        }

        static void ClearLinks(VFXContext container)
        {
            ClearLinks(container as IVFXSlotContainer);

            foreach (var block in container.children)
            {
                ClearLinks(block);
            }
            container.UnlinkAll();
            container.SetDefaultData(false);
        }

        static void ClearLinks(IVFXSlotContainer container)
        {
            foreach (var slot in container.inputSlots)
            {
                slot.UnlinkAll(true, false);
            }
            foreach (var slot in container.outputSlots)
            {
                slot.UnlinkAll(true, false);
            }
        }

        private static void CopyDataEdges(Data copyData, ScriptableObject[] allSerializedObjects)
        {
            if (copyData.dataEdges != null)
            {
                foreach (var dataEdge in copyData.dataEdges)
                {
                    VFXSlot inputSlot = null;
                    if (dataEdge.inputContext)
                    {
                        VFXContext targetContext = allSerializedObjects[dataEdge.input.targetIndex] as VFXContext;
                        if (dataEdge.inputBlockIndex == -1)
                        {
                            inputSlot = FetchSlot(targetContext, dataEdge.input.slotPath, true);
                        }
                        else
                        {
                            inputSlot = FetchSlot(targetContext[dataEdge.inputBlockIndex], dataEdge.input.slotPath, true);
                        }
                    }
                    else
                    {
                        VFXModel model = allSerializedObjects[dataEdge.input.targetIndex] as VFXModel;
                        inputSlot = FetchSlot(model as IVFXSlotContainer, dataEdge.input.slotPath, true);
                    }

                    IVFXSlotContainer outputContainer = null;
                    if (dataEdge.outputParameter)
                    {
                        var parameter = copyData.parameters[dataEdge.outputParameterIndex];
                        outputContainer = parameter.parameter;
                    }
                    else
                    {
                        outputContainer = allSerializedObjects[dataEdge.output.targetIndex] as IVFXSlotContainer;
                    }

                    VFXSlot outputSlot = FetchSlot(outputContainer, dataEdge.output.slotPath, false);

                    if (inputSlot != null && outputSlot != null)
                    {
                        if (inputSlot.Link(outputSlot) && dataEdge.outputParameter)
                        {
                            var parameter = copyData.parameters[dataEdge.outputParameterIndex];
                            var node = parameter.parameter.nodes[dataEdge.outputParameterNodeIndex + parameter.infoIndexOffset];
                            if (node.linkedSlots == null)
                                node.linkedSlots = new List<VFXParameter.NodeLinkedSlot>();
                            node.linkedSlots.Add(new VFXParameter.NodeLinkedSlot() { inputSlot = inputSlot, outputSlot = outputSlot });
                        }
                    }
                }
            }
        }

        static void PasteNodes(VFXViewController viewController, Vector2 center, Data copyData, ScriptableObject[] allSerializedObjects, VFXView view, VFXGroupNodeController groupNode)
        {
            var graph = viewController.graph;
            Vector2 pasteOffset = (copyData.bounds.width > 0 && copyData.bounds.height > 0) ? center - copyData.bounds.center : Vector2.zero;

            // look if pasting there will result in the first element beeing exactly on top of other
            while (true)
            {
                bool foundSamePosition = false;
                if (copyData.contexts != null && copyData.contexts.Length > 0)
                {
                    VFXContext firstContext = copyData.contexts[0];

                    foreach (var existingContext in viewController.graph.children.OfType<VFXContext>())
                    {
                        if ((firstContext.position + pasteOffset - existingContext.position).sqrMagnitude < 1)
                        {
                            foundSamePosition = true;
                            break;
                        }
                    }
                }
                else if (copyData.slotContainers != null && copyData.slotContainers.Length > 0)
                {
                    VFXModel firstContainer = copyData.slotContainers[0];

                    foreach (var existingSlotContainer in viewController.graph.children.Where(t => t is IVFXSlotContainer))
                    {
                        if ((firstContainer.position + pasteOffset - existingSlotContainer.position).sqrMagnitude < 1)
                        {
                            foundSamePosition = true;
                            break;
                        }
                    }
                }
                else
                {
                    VFXUI ui = allSerializedObjects.OfType<VFXUI>().First();

                    if (ui != null)
                    {
                        if (ui.stickyNoteInfos != null && ui.stickyNoteInfos.Length > 0)
                        {
                            foreach (var stickyNote in viewController.stickyNotes)
                            {
                                if ((ui.stickyNoteInfos[0].position.position + pasteOffset - stickyNote.position.position).sqrMagnitude < 1)
                                {
                                    foundSamePosition = true;
                                    break;
                                }
                            }
                        }
                        else if (ui.groupInfos != null && ui.groupInfos.Length > 0)
                        {
                            foreach (var gn in viewController.groupNodes)
                            {
                                if ((ui.groupInfos[0].position.position + pasteOffset - gn.position.position).sqrMagnitude < 1)
                                {
                                    foundSamePosition = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (foundSamePosition)
                {
                    pasteOffset += Vector2.one * 30;
                }
                else
                {
                    break;
                }
            }


            if (copyData.contexts != null)
            {
                foreach (var slotContainer in copyData.contexts)
                {
                    var newContext = slotContainer;
                    newContext.position += pasteOffset;
                    ClearLinks(newContext);
                }
            }

            if (copyData.slotContainers != null)
            {
                foreach (var slotContainer in copyData.slotContainers)
                {
                    var newSlotContainer = slotContainer;
                    newSlotContainer.position += pasteOffset;
                    ClearLinks(newSlotContainer as IVFXSlotContainer);
                }
            }


            for (int i = 0; i < allSerializedObjects.Length; ++i)
            {
                ScriptableObject obj = allSerializedObjects[i];

                if (obj is VFXContext || obj is VFXOperator)
                {
                    graph.AddChild(obj as VFXModel);
                }
                else if (obj is VFXParameter)
                {
                    int paramIndex = System.Array.FindIndex(copyData.parameters, t => t.index == i);

                    VFXParameter existingParameter = graph.children.OfType<VFXParameter>().FirstOrDefault(t => t.GetInstanceID() == copyData.parameters[paramIndex].originalInstanceID);
                    if (existingParameter != null)
                    {
                        // The original parameter is from the current graph, add the nodes to the original
                        copyData.parameters[paramIndex].parameter = existingParameter;
                        copyData.parameters[paramIndex].copiedParameter = obj as VFXParameter;

                        copyData.parameters[paramIndex].infoIndexOffset = existingParameter.nodes.Count;

                        foreach (var info in copyData.parameters[paramIndex].infos)
                        {
                            info.position += pasteOffset;
                        }

                        var oldIDs = copyData.parameters[paramIndex].infos.ToDictionary(t => t, t => t.id);

                        existingParameter.AddNodeRange(copyData.parameters[paramIndex].infos);

                        //keep track of new ids for groupnodes
                        copyData.parameters[paramIndex].idMap = copyData.parameters[paramIndex].infos.ToDictionary(t => oldIDs[t], t => t.id);
                    }
                    else
                    {
                        // The original parameter is from another graph : create the parameter in the other graph, but replace the infos with only the ones copied.
                        copyData.parameters[paramIndex].parameter = obj as VFXParameter;
                        copyData.parameters[paramIndex].parameter.SetNodes(copyData.parameters[paramIndex].infos);

                        graph.AddChild(obj as VFXModel);
                    }
                }
            }


            VFXUI copiedUI = allSerializedObjects.OfType<VFXUI>().FirstOrDefault();
            int firstCopiedGroup = -1;
            int firstCopiedStickyNote = -1;
            if (copiedUI != null)
            {
                VFXUI ui = viewController.graph.UIInfos;
                firstCopiedStickyNote = ui.stickyNoteInfos != null ? ui.stickyNoteInfos.Length : 0;

                if (copiedUI.groupInfos != null && copiedUI.groupInfos.Length > 0)
                {
                    if (ui.groupInfos == null)
                    {
                        ui.groupInfos = new VFXUI.GroupInfo[0];
                    }
                    firstCopiedGroup = ui.groupInfos.Length;

                    foreach (var groupInfos in copiedUI.groupInfos)
                    {
                        for (int i = 0; i < groupInfos.contents.Length; ++i)
                        {
                            // if we link the parameter node to an existing parameter instead of the copied parameter we have to patch the groupnode content to point the that parameter with the correct id.
                            if (groupInfos.contents[i].model is VFXParameter)
                            {
                                VFXParameter parameter = groupInfos.contents[i].model as VFXParameter;
                                var paramInfo = copyData.parameters.FirstOrDefault(t => t.copiedParameter == parameter);
                                if (paramInfo.parameter != null) // parameter will not be null unless the struct returned is the default.
                                {
                                    groupInfos.contents[i].model = paramInfo.parameter;
                                    groupInfos.contents[i].id = paramInfo.idMap[groupInfos.contents[i].id];
                                }
                            }
                            else if (groupInfos.contents[i].isStickyNote)
                            {
                                groupInfos.contents[i].id += firstCopiedStickyNote;
                            }
                        }
                    }

                    ui.groupInfos = ui.groupInfos.Concat(copiedUI.groupInfos.Select(t => new VFXUI.GroupInfo(t) { position = new Rect(t.position.position + pasteOffset, t.position.size) })).ToArray();
                }
                if (copiedUI.stickyNoteInfos != null && copiedUI.stickyNoteInfos.Length > 0)
                {
                    if (ui.stickyNoteInfos == null)
                    {
                        ui.stickyNoteInfos = new VFXUI.StickyNoteInfo[0];
                    }
                    ui.stickyNoteInfos = ui.stickyNoteInfos.Concat(copiedUI.stickyNoteInfos.Select(t => new VFXUI.StickyNoteInfo(t) { position = new Rect(t.position.position + pasteOffset, t.position.size) })).ToArray();
                }
            }

            CopyDataEdges(copyData, allSerializedObjects);


            if (copyData.flowEdges != null)
            {
                foreach (var flowEdge in copyData.flowEdges)
                {
                    VFXContext inputContext = allSerializedObjects[flowEdge.input.contextIndex] as VFXContext;
                    VFXContext outputContext = allSerializedObjects[flowEdge.output.contextIndex] as VFXContext;

                    inputContext.LinkFrom(outputContext, flowEdge.input.flowIndex, flowEdge.output.flowIndex);
                }
            }

            foreach (var dataAndContexts in copyData.dataAndContexts)
            {
                VFXData data = allSerializedObjects[dataAndContexts.dataIndex] as VFXData;

                foreach (var contextIndex in dataAndContexts.contextsIndexes)
                {
                    VFXContext context = allSerializedObjects[contextIndex] as VFXContext;
                    data.CopySettings(context.GetData());
                }
            }

            // Create all ui based on model
            viewController.LightApplyChanges();

            if (view != null)
            {
                view.ClearSelection();

                var elements = view.graphElements.ToList();


                List<VFXNodeUI> newSlotContainerUIs = new List<VFXNodeUI>();
                List<VFXContextUI> newContextUIs = new List<VFXContextUI>();

                foreach (var slotContainer in allSerializedObjects.OfType<VFXContext>())
                {
                    VFXContextUI contextUI = elements.OfType<VFXContextUI>().FirstOrDefault(t => t.controller.model == slotContainer);
                    if (contextUI != null)
                    {
                        newSlotContainerUIs.Add(contextUI);
                        newSlotContainerUIs.AddRange(contextUI.GetAllBlocks().Cast<VFXNodeUI>());
                        newContextUIs.Add(contextUI);
                        view.AddToSelection(contextUI);
                    }
                }
                foreach (var slotContainer in allSerializedObjects.OfType<VFXOperator>())
                {
                    VFXOperatorUI slotContainerUI = elements.OfType<VFXOperatorUI>().FirstOrDefault(t => t.controller.model == slotContainer);
                    if (slotContainerUI != null)
                    {
                        newSlotContainerUIs.Add(slotContainerUI);
                        view.AddToSelection(slotContainerUI);
                    }
                }

                foreach (var param in copyData.parameters)
                {
                    foreach (var parameterUI in elements.OfType<VFXParameterUI>().Where(t => t.controller.model == param.parameter && param.parameter.nodes.IndexOf(t.controller.infos) >= param.infoIndexOffset))
                    {
                        newSlotContainerUIs.Add(parameterUI);
                        view.AddToSelection(parameterUI);
                    }
                }

                // Simply selected all data edge with the context or slot container, they can be no other than the copied ones
                foreach (var dataEdge in elements.OfType<VFXDataEdge>())
                {
                    if (newSlotContainerUIs.Contains(dataEdge.input.GetFirstAncestorOfType<VFXNodeUI>()))
                    {
                        view.AddToSelection(dataEdge);
                    }
                }
                // Simply selected all data edge with the context or slot container, they can be no other than the copied ones
                foreach (var flowEdge in elements.OfType<VFXFlowEdge>())
                {
                    if (newContextUIs.Contains(flowEdge.input.GetFirstAncestorOfType<VFXContextUI>()))
                    {
                        view.AddToSelection(flowEdge);
                    }
                }

                if (groupNode != null)
                {
                    foreach (var newSlotContainerUI in newSlotContainerUIs)
                    {
                        groupNode.AddNode(newSlotContainerUI.controller);
                    }
                }

                //Select all groups that are new
                if (firstCopiedGroup >= 0)
                {
                    foreach (var gn in elements.OfType<VFXGroupNode>())
                    {
                        if (gn.controller.index >= firstCopiedGroup)
                        {
                            view.AddToSelection(gn);
                        }
                    }
                }

                //Select all groups that are new
                if (firstCopiedStickyNote >= 0)
                {
                    foreach (var gn in elements.OfType<VFXStickyNote>())
                    {
                        if (gn.controller.index >= firstCopiedStickyNote)
                        {
                            view.AddToSelection(gn);
                        }
                    }
                }
            }
        }
    }
}
