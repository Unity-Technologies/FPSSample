#define NOTIFICATION_VALIDATION
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Experimental.VFX;
using UnityEngine;
using UnityEngine.Profiling;

using UnityObject = UnityEngine.Object;
using Branch = UnityEditor.VFX.Operator.VFXOperatorDynamicBranch;

namespace UnityEditor.VFX.UI
{
    internal partial class VFXViewController : Controller<VisualEffectResource>
    {
        private int m_UseCount;
        public int useCount
        {
            get { return m_UseCount; }
            set
            {
                m_UseCount = value;
                if (m_UseCount == 0)
                {
                    RemoveController(this);
                }
            }
        }

        public enum Priorities
        {
            Graph,
            Node,
            Slot,
            Default,
            GroupNode,
            Count
        }

        string m_Name;

        public string name
        {
            get { return m_Name; }
        }

        string ComputeName()
        {
            if (model == null)
                return "";
            string assetPath = AssetDatabase.GetAssetPath(model);
            if (!string.IsNullOrEmpty(assetPath))
            {
                return Path.GetFileNameWithoutExtension(assetPath);
            }
            else
            {
                return model.name;
            }
        }

        static HashSet<ScriptableObject>[] NewPrioritizedHashSet()
        {
            HashSet<ScriptableObject>[] result = new HashSet<ScriptableObject>[(int)Priorities.Count];

            for (int i = 0; i < (int)Priorities.Count; ++i)
            {
                result[i] = new HashSet<ScriptableObject>();
            }

            return result;
        }

        Priorities GetPriority(VFXObject obj)
        {
            if (obj is IVFXSlotContainer)
            {
                return Priorities.Node;
            }
            if (obj is VFXSlot)
            {
                return Priorities.Slot;
            }
            if (obj is VFXUI)
            {
                return Priorities.GroupNode;
            }
            if (obj is VFXGraph)
            {
                return Priorities.Graph;
            }
            return Priorities.Default;
        }

        HashSet<ScriptableObject>[] modifiedModels = NewPrioritizedHashSet();
        HashSet<ScriptableObject>[] otherModifiedModels = NewPrioritizedHashSet();

        public void OnObjectModified(VFXObject obj)
        {
            modifiedModels[(int)GetPriority(obj)].Add(obj);
        }

        Dictionary<ScriptableObject, List<Action>> m_Notified = new Dictionary<ScriptableObject, List<Action>>();


        public void RegisterNotification(VFXObject target, Action action)
        {
            if (target == null)
                return;

            target.onModified += OnObjectModified;
            List<Action> notifieds;
            if (m_Notified.TryGetValue(target, out notifieds))
            {
                #if NOTIFICATION_VALIDATION
                if (notifieds.Contains(action))
                    Debug.LogError("Adding the same notification twice on:" + target.name);
                #endif
                notifieds.Add(action);
            }
            else
            {
                notifieds = new List<Action>();
                notifieds.Add(action);

                m_Notified.Add(target, notifieds);
            }
        }

        public void UnRegisterNotification(VFXObject target, Action action)
        {
            if (object.ReferenceEquals(target, null))
                return;

            target.onModified -= OnObjectModified;
            List<Action> notifieds;
            if (m_Notified.TryGetValue(target, out notifieds))
            {
                #if NOTIFICATION_VALIDATION
                if (!notifieds.Contains(action))
                    Debug.LogError("Removing a non existent notification" + target.name);
                #endif
                notifieds.Remove(action);

                if (m_CurrentlyNotified == target)
                {
                    m_CurrentActions.Remove(action);
                }
            }
        }

        bool m_InNotify = false;

        ScriptableObject m_CurrentlyNotified; //this and the next list are used when in case a notification removes a following modification
        List<Action> m_CurrentActions = new List<Action>();

        public void NotifyUpdate()
        {
            m_InNotify = true;
            Profiler.BeginSample("VFXViewController.NotifyUpdate");
            if (model == null || m_Graph == null || m_Graph != model.graph)
            {
                // In this case the asset has been destroyed or reimported after having changed outside.
                // Lets rebuild everything and clear the undo stack.
                Clear();
                if (model != null && model.graph != null)
                    InitializeUndoStack();
                ModelChanged(model);
            }

            var tmp = modifiedModels;
            modifiedModels = otherModifiedModels;
            otherModifiedModels = tmp;


            int cpt = 0;
            foreach (var objs in otherModifiedModels)
            {
                foreach (var obj in objs)
                {
                    List<Action> notifieds;
                    Profiler.BeginSample("VFXViewController.Notify:" + obj.GetType().Name);
                    if (m_Notified.TryGetValue(obj, out notifieds))
                    {
                        m_CurrentlyNotified = obj;
                        m_CurrentActions.Clear();
                        m_CurrentActions.AddRange(notifieds);
                        m_CurrentActions.Reverse();
                        while (m_CurrentActions.Count > 0)
                        {
                            var action = m_CurrentActions[m_CurrentActions.Count - 1];
                            action();
                            cpt++;
                            m_CurrentActions.RemoveAt(m_CurrentActions.Count - 1);
                        }
                    }
                    Profiler.EndSample();
                }
                m_CurrentlyNotified = null;

                objs.Clear();
            }
            /*
            if (cpt > 0)
                Debug.LogWarningFormat("{0} notification sent this frame", cpt);*/
            Profiler.EndSample();

            m_InNotify = false;

            string newName = ComputeName();
            if (newName != m_Name)
            {
                m_Name = newName;

                if (model != null)
                {
                    model.name = m_Name;
                }
                if (graph != null)
                {
                    (graph as UnityObject).name = m_Name;
                }

                NotifyChange(Change.assetName);
            }

            if (m_DataEdgesMightHaveChangedAsked)
            {
                m_DataEdgesMightHaveChangedAsked = false;
                DataEdgesMightHaveChanged();
            }
        }

        public VFXGraph graph { get {return model != null ? model.graph as VFXGraph : null; }}

        List<VFXFlowAnchorController> m_FlowAnchorController = new List<VFXFlowAnchorController>();

        // Model / Controller synchronization
        private Dictionary<VFXModel, List<VFXNodeController>> m_SyncedModels = new Dictionary<VFXModel, List<VFXNodeController>>();

        List<VFXDataEdgeController> m_DataEdges = new List<VFXDataEdgeController>();
        List<VFXFlowEdgeController> m_FlowEdges = new List<VFXFlowEdgeController>();

        public override IEnumerable<Controller> allChildren
        {
            get
            {
                return m_SyncedModels.Values.SelectMany(t => t).Cast<Controller>().
                    Concat(m_DataEdges.Cast<Controller>()).
                    Concat(m_FlowEdges.Cast<Controller>()).
                    Concat(m_ParameterControllers.Values.Cast<Controller>()).
                    Concat(m_GroupNodeControllers.Cast<Controller>()).
                    Concat(m_StickyNoteControllers.Cast<Controller>())
                ;
            }
        }

        public void LightApplyChanges()
        {
            ModelChanged(model);
            GraphChanged();
        }

        public override void ApplyChanges()
        {
            ModelChanged(model);
            GraphChanged();

            foreach (var controller in allChildren)
            {
                controller.ApplyChanges();
            }
        }

        void GraphLost()
        {
            Clear();
            if (!object.ReferenceEquals(m_Graph, null))
            {
                RemoveInvalidateDelegate(m_Graph, InvalidateExpressionGraph);
                RemoveInvalidateDelegate(m_Graph, IncremenentGraphUndoRedoState);

                UnRegisterNotification(m_Graph, GraphChanged);

                m_Graph = null;
            }
            if (!object.ReferenceEquals(m_UI, null))
            {
                UnRegisterNotification(m_UI, UIChanged);
                m_UI = null;
            }
        }

        public override void OnDisable()
        {
            Profiler.BeginSample("VFXViewController.OnDisable");
            GraphLost();
            ReleaseUndoStack();
            Undo.undoRedoPerformed -= SynchronizeUndoRedoState;
            Undo.willFlushUndoRecord -= WillFlushUndoRecord;

            base.OnDisable();
            Profiler.EndSample();
        }

        public IEnumerable<VFXNodeController> AllSlotContainerControllers
        {
            get
            {
                var operatorControllers = m_SyncedModels.Values.SelectMany(t => t).OfType<VFXNodeController>();
                var blockControllers = (contexts.SelectMany(t => t.blockControllers)).Cast<VFXNodeController>();

                return operatorControllers.Concat(blockControllers);
            }
        }

        public bool RecreateNodeEdges()
        {
            bool changed = false;
            HashSet<VFXDataEdgeController> unusedEdges = new HashSet<VFXDataEdgeController>();
            foreach (var e in m_DataEdges)
            {
                unusedEdges.Add(e);
            }

            var nodeToUpdate = new HashSet<VFXNodeController>();

            foreach (var operatorControllers in m_SyncedModels.Values)
            {
                foreach (var nodeController in operatorControllers)
                {
                    bool nodeChanged = false;
                    foreach (var input in nodeController.inputPorts)
                    {
                        nodeChanged |= RecreateInputSlotEdge(unusedEdges, nodeController, input);
                    }
                    if (nodeController is VFXContextController)
                    {
                        VFXContextController contextController = nodeController as VFXContextController;

                        foreach (var block in contextController.blockControllers)
                        {
                            bool blockChanged = false;
                            foreach (var input in block.inputPorts)
                            {
                                blockChanged |= RecreateInputSlotEdge(unusedEdges, block, input);
                            }
                            if (blockChanged)
                                nodeToUpdate.Add(block);
                            changed |= blockChanged;
                        }
                    }
                    if (nodeChanged)
                        nodeToUpdate.Add(nodeController);

                    changed |= nodeChanged;
                }
            }

            foreach (var edge in unusedEdges)
            {
                nodeToUpdate.Add(edge.input.sourceNode);
                edge.OnDisable();

                m_DataEdges.Remove(edge);
                changed = true;
            }

            foreach (var node in nodeToUpdate)
            {
                node.UpdateAllEditable();
            }

            return changed;
        }

        bool m_DataEdgesMightHaveChangedAsked;

        public void DataEdgesMightHaveChanged()
        {
            if (m_Syncing) return;

            if (m_InNotify)
            {
                m_DataEdgesMightHaveChangedAsked = true;
                return;
            }

            Profiler.BeginSample("VFXViewController.DataEdgesMightHaveChanged");

            bool change = RecreateNodeEdges();

            if (change)
            {
                NotifyChange(Change.dataEdge);
            }

            Profiler.EndSample();
        }

        public bool RecreateInputSlotEdge(HashSet<VFXDataEdgeController> unusedEdges, VFXNodeController slotContainer, VFXDataAnchorController input)
        {
            VFXSlot inputSlot = input.model;
            if (inputSlot == null)
                return false;

            bool changed = false;
            if (input.HasLink())
            {
                VFXNodeController operatorControllerFrom = null;

                IVFXSlotContainer targetSlotContainer = inputSlot.refSlot.owner;
                if (targetSlotContainer == null)
                {
                    return false;
                }
                if (targetSlotContainer is VFXParameter)
                {
                    VFXParameterController controller = null;
                    if (m_ParameterControllers.TryGetValue(targetSlotContainer as VFXParameter, out controller))
                    {
                        operatorControllerFrom = controller.GetParameterForLink(inputSlot);
                    }
                }
                else if (targetSlotContainer is VFXBlock)
                {
                    VFXBlock block = targetSlotContainer as VFXBlock;
                    VFXContext context = block.GetParent();
                    List<VFXNodeController> contextControllers = null;
                    if (m_SyncedModels.TryGetValue(context, out contextControllers) && contextControllers.Count > 0)
                    {
                        operatorControllerFrom = (contextControllers[0] as VFXContextController).blockControllers.FirstOrDefault(t => t.model == block);
                    }
                }
                else
                {
                    List<VFXNodeController> nodeControllers = null;
                    if (m_SyncedModels.TryGetValue(targetSlotContainer as VFXModel, out nodeControllers) && nodeControllers.Count > 0)
                    {
                        operatorControllerFrom = nodeControllers[0];
                    }
                }

                var operatorControllerTo = slotContainer;

                if (operatorControllerFrom != null && operatorControllerTo != null)
                {
                    var anchorFrom = operatorControllerFrom.outputPorts.FirstOrDefault(o => (o as VFXDataAnchorController).model == inputSlot.refSlot);
                    var anchorTo = input;

                    var edgController = m_DataEdges.FirstOrDefault(t => t.input == anchorTo && t.output == anchorFrom);

                    if (edgController != null)
                    {
                        unusedEdges.Remove(edgController);
                    }
                    else
                    {
                        if (anchorFrom != null && anchorTo != null)
                        {
                            edgController = new VFXDataEdgeController(anchorTo, anchorFrom);
                            m_DataEdges.Add(edgController);
                            changed = true;
                        }
                    }
                }
            }

            foreach (VFXSlot subSlot in inputSlot.children)
            {
                VFXDataAnchorController subAnchor = slotContainer.inputPorts.FirstOrDefault(t => t.model == subSlot);
                if (subAnchor != null) // Can be null for example for hidden values from Vector3Spaceables
                {
                    changed |= RecreateInputSlotEdge(unusedEdges, slotContainer, subAnchor);
                }
            }

            return changed;
        }

        public IEnumerable<VFXContextController> contexts
        {
            get { return m_SyncedModels.Values.SelectMany(t => t).OfType<VFXContextController>(); }
        }
        public IEnumerable<VFXNodeController> nodes
        {
            get { return m_SyncedModels.Values.SelectMany(t => t); }
        }

        public void FlowEdgesMightHaveChanged()
        {
            if (m_Syncing) return;

            bool change = RecreateFlowEdges();
            if (change)
            {
                UpdateSystems(); // System will change based on flowEdges
                NotifyChange(Change.flowEdge);
            }
        }

        public class Change
        {
            public const int flowEdge = 1;
            public const int dataEdge = 2;

            public const int groupNode = 3;

            public const int assetName = 4;

            public const int destroy = 666;
        }

        bool RecreateFlowEdges()
        {
            bool changed = false;
            HashSet<VFXFlowEdgeController> unusedEdges = new HashSet<VFXFlowEdgeController>();
            foreach (var e in m_FlowEdges)
            {
                unusedEdges.Add(e);
            }

            var contextControllers = contexts;
            foreach (var outController in contextControllers.ToArray())
            {
                var output = outController.model;
                for (int slotIndex = 0; slotIndex < output.inputFlowSlot.Length; ++slotIndex)
                {
                    var inputFlowSlot = output.inputFlowSlot[slotIndex];
                    foreach (var link in inputFlowSlot.link)
                    {
                        var inController = contexts.FirstOrDefault(x => x.model == link.context);
                        if (inController == null)
                            break;

                        var outputAnchor = inController.flowOutputAnchors.Where(o => o.slotIndex == link.slotIndex).FirstOrDefault();
                        var inputAnchor = outController.flowInputAnchors.Where(o => o.slotIndex == slotIndex).FirstOrDefault();

                        var edgeController = m_FlowEdges.FirstOrDefault(t => t.input == inputAnchor && t.output == outputAnchor);
                        if (edgeController != null)
                            unusedEdges.Remove(edgeController);
                        else
                        {
                            edgeController = new VFXFlowEdgeController(inputAnchor, outputAnchor);
                            m_FlowEdges.Add(edgeController);
                            changed = true;
                        }
                    }
                }
            }

            foreach (var edge in unusedEdges)
            {
                edge.OnDisable();
                m_FlowEdges.Remove(edge);
                changed = true;
            }

            return changed;
        }

        private enum RecordEvent
        {
            Add,
            Remove
        }

        public ReadOnlyCollection<VFXDataEdgeController> dataEdges
        {
            get { return m_DataEdges.AsReadOnly(); }
        }
        public ReadOnlyCollection<VFXFlowEdgeController> flowEdges
        {
            get { return m_FlowEdges.AsReadOnly(); }
        }

        public bool CreateLink(VFXDataAnchorController input, VFXDataAnchorController output)
        {
            if (input == null)
            {
                return false;
            }
            if (!input.CanLink(output))
            {
                return false;
            }

            VFXParameter.NodeLinkedSlot resulting = input.CreateLinkTo(output);

            if (resulting.inputSlot != null && resulting.outputSlot != null)
            {
                VFXParameterNodeController fromController = output.sourceNode as VFXParameterNodeController;

                if (fromController != null)
                {
                    if (fromController.infos.linkedSlots == null)
                        fromController.infos.linkedSlots = new List<VFXParameter.NodeLinkedSlot>();
                    fromController.infos.linkedSlots.Add(resulting);
                }
                DataEdgesMightHaveChanged();
                return true;
            }
            return false;
        }

        public void AddElement(VFXDataEdgeController edge)
        {
            var fromAnchor = edge.output;
            var toAnchor = edge.input;

            CreateLink(toAnchor, fromAnchor);
            edge.OnDisable();
        }

        public void AddElement(VFXFlowEdgeController edge)
        {
            var flowEdge = (VFXFlowEdgeController)edge;

            var outputFlowAnchor = flowEdge.output as VFXFlowAnchorController;
            var inputFlowAnchor = flowEdge.input as VFXFlowAnchorController;

            var contextOutput = outputFlowAnchor.owner;
            var contextInput = inputFlowAnchor.owner;

            contextOutput.LinkTo(contextInput, outputFlowAnchor.slotIndex, inputFlowAnchor.slotIndex);

            edge.OnDisable();
        }

        public void Remove(IEnumerable<Controller> removedControllers)
        {
            var removedContexts = new HashSet<VFXContextController>(removedControllers.OfType<VFXContextController>());

            //remove all blocks that are in a removed context.
            var removed = removedControllers.Where(t => !(t is VFXBlockController) || !removedContexts.Contains((t as VFXBlockController).contextController)).Distinct().ToArray();

            foreach (var controller in removed)
            {
                RemoveElement(controller);
            }
        }

        public void RemoveElement(Controller element)
        {
            if (element is VFXContextController)
            {
                VFXContextController contextController = ((VFXContextController)element);
                VFXContext context = contextController.model;
                contextController.NodeGoingToBeRemoved();

                // Remove connections from context
                foreach (var slot in context.inputSlots.Concat(context.outputSlots))
                    slot.UnlinkAll(true, true);

                // Remove connections from blocks
                foreach (VFXBlockController blockPres in (element as VFXContextController).blockControllers)
                {
                    foreach (var slot in blockPres.slotContainer.outputSlots.Concat(blockPres.slotContainer.inputSlots))
                    {
                        slot.UnlinkAll(true, true);
                    }
                }

                // remove flow connections from context
                // TODO update data types
                context.UnlinkAll();
                // Detach from graph
                context.Detach();

                RemoveFromGroupNodes(element as VFXNodeController);

                UnityObject.DestroyImmediate(context, true);
            }
            else if (element is VFXBlockController)
            {
                var block = element as VFXBlockController;
                block.NodeGoingToBeRemoved();
                block.contextController.RemoveBlock(block.model);

                UnityObject.DestroyImmediate(block.model, true);
            }
            else if (element is VFXParameterNodeController)
            {
                var parameter = element as VFXParameterNodeController;
                parameter.NodeGoingToBeRemoved();
                parameter.parentController.model.RemoveNode(parameter.infos);
                RemoveFromGroupNodes(element as VFXNodeController);
                DataEdgesMightHaveChanged();
            }
            else if (element is VFXNodeController || element is VFXParameterController)
            {
                IVFXSlotContainer container = null;

                if (element is VFXNodeController)
                {
                    VFXNodeController nodeController = (element as VFXNodeController);
                    container = nodeController.model as IVFXSlotContainer;
                    nodeController.NodeGoingToBeRemoved();
                    RemoveFromGroupNodes(element as VFXNodeController);
                }
                else
                {
                    container = (element as VFXParameterController).model;

                    foreach (var parameterNode in m_SyncedModels[container as VFXModel])
                    {
                        RemoveFromGroupNodes(parameterNode);
                    }
                }

                VFXSlot slotToClean = null;
                do
                {
                    slotToClean = container.inputSlots.Concat(container.outputSlots)
                        .FirstOrDefault(o => o.HasLink(true));
                    if (slotToClean)
                    {
                        slotToClean.UnlinkAll(true, true);
                    }
                }
                while (slotToClean != null);

                graph.RemoveChild(container as VFXModel);

                UnityObject.DestroyImmediate(container as VFXModel, true);
                DataEdgesMightHaveChanged();
            }
            else if (element is VFXFlowEdgeController)
            {
                var flowEdge = element as VFXFlowEdgeController;


                var inputAnchor = flowEdge.input as VFXFlowAnchorController;
                var outputAnchor = flowEdge.output as VFXFlowAnchorController;

                if (inputAnchor != null && outputAnchor != null)
                {
                    var contextInput = inputAnchor.owner as VFXContext;
                    var contextOutput = outputAnchor.owner as VFXContext;

                    if (contextInput != null && contextOutput != null)
                        contextInput.UnlinkFrom(contextOutput, outputAnchor.slotIndex, inputAnchor.slotIndex);
                }
            }
            else if (element is VFXDataEdgeController)
            {
                var edge = element as VFXDataEdgeController;
                var to = edge.input as VFXDataAnchorController;

                if (to != null)
                {
                    to.sourceNode.OnEdgeGoingToBeRemoved(to);
                    var slot = to.model;
                    if (slot != null)
                    {
                        slot.UnlinkAll();
                    }
                }
            }
            else if (element is VFXGroupNodeController)
            {
                RemoveGroupNode(element as VFXGroupNodeController);
            }
            else if (element is VFXStickyNoteController)
            {
                RemoveStickyNote(element as VFXStickyNoteController);
            }
            else
            {
                Debug.LogErrorFormat("Unexpected type : {0}", element.GetType().FullName);
            }
        }

        protected override void ModelChanged(UnityObject obj)
        {
            if (model == null)
            {
                NotifyChange(Change.destroy);
                GraphLost();

                RemoveController(this);
                return;
            }

            // a standard equals will return true is the m_Graph is a destroyed object with the same instance ID ( like with a source control revert )
            if (!object.ReferenceEquals(m_Graph, model.GetOrCreateGraph()))
            {
                if (!object.ReferenceEquals(m_Graph, null))
                {
                    UnRegisterNotification(m_Graph, GraphChanged);
                    UnRegisterNotification(m_UI, UIChanged);
                }
                if (m_Graph != null)
                {
                    GraphLost();
                }
                else
                {
                    Clear();
                }
                m_Graph =  model.GetOrCreateGraph();
                m_Graph.SanitizeGraph();

                if (m_Graph != null)
                {
                    RegisterNotification(m_Graph, GraphChanged);

                    AddInvalidateDelegate(m_Graph, InvalidateExpressionGraph);
                    AddInvalidateDelegate(m_Graph, IncremenentGraphUndoRedoState);


                    m_UI = m_Graph.UIInfos;

                    RegisterNotification(m_UI, UIChanged);

                    GraphChanged();
                }
            }
        }

        public void AddGroupNode(Vector2 pos)
        {
            PrivateAddGroupNode(pos);

            m_Graph.Invalidate(VFXModel.InvalidationCause.kUIChanged);
        }

        public void AddStickyNote(Vector2 position, VFXGroupNodeController group)
        {
            var ui = graph.UIInfos;

            var stickyNoteInfo = new VFXUI.StickyNoteInfo
            {
                title = "Title",
                position = new Rect(position, Vector2.one * 100),
                contents = "type something here",
                theme = StickyNote.Theme.Classic.ToString(),
                textSize = StickyNote.TextSize.Small.ToString()
            };

            if (ui.stickyNoteInfos != null)
                ui.stickyNoteInfos = ui.stickyNoteInfos.Concat(Enumerable.Repeat(stickyNoteInfo, 1)).ToArray();
            else
                ui.stickyNoteInfos = new VFXUI.StickyNoteInfo[] { stickyNoteInfo };

            if (group != null)
            {
                LightApplyChanges();

                group.AddStickyNote(m_StickyNoteControllers[ui.stickyNoteInfos.Length - 1]);
            }

            m_Graph.Invalidate(VFXModel.InvalidationCause.kUIChanged);
        }

        void RemoveGroupNode(VFXGroupNodeController groupNode)
        {
            var ui = graph.UIInfos;

            int index = groupNode.index;

            ui.groupInfos = ui.groupInfos.Where((t, i) => i != index).ToArray();

            groupNode.Remove();
            m_GroupNodeControllers.RemoveAt(index);

            for (int i = index; i < m_GroupNodeControllers.Count; ++i)
            {
                m_GroupNodeControllers[i].index = i;
            }
            m_Graph.Invalidate(VFXModel.InvalidationCause.kUIChanged);
        }

        void RemoveStickyNote(VFXStickyNoteController stickyNote)
        {
            var ui = graph.UIInfos;

            int index = stickyNote.index;

            ui.stickyNoteInfos = ui.stickyNoteInfos.Where((t, i) => i != index).ToArray();

            stickyNote.Remove();
            m_StickyNoteControllers.RemoveAt(index);

            for (int i = index; i < m_StickyNoteControllers.Count; ++i)
            {
                m_StickyNoteControllers[i].index = i;
            }

            //Patch group nodes, removing this sticky note and fixing ids that are bigger than index
            if (ui.groupInfos != null)
            {
                for (int i = 0; i < ui.groupInfos.Length; ++i)
                {
                    for (int j = 0; j < ui.groupInfos[i].contents.Length; ++j)
                    {
                        if (ui.groupInfos[i].contents[j].isStickyNote)
                        {
                            if (ui.groupInfos[i].contents[j].id == index)
                            {
                                ui.groupInfos[i].contents = ui.groupInfos[i].contents.Where((t, idx) => idx != j).ToArray();
                                j--;
                            }
                            else if (ui.groupInfos[i].contents[j].id > index)
                            {
                                --(ui.groupInfos[i].contents[j].id);
                            }
                        }
                    }
                }
            }

            m_Graph.Invalidate(VFXModel.InvalidationCause.kUIChanged);
        }

        void RemoveFromGroupNodes(VFXNodeController node)
        {
            foreach (var groupNode in m_GroupNodeControllers)
            {
                if (groupNode.ContainsNode(node))
                {
                    groupNode.RemoveNode(node);
                }
            }
        }

        protected void GraphChanged()
        {
            if (m_Graph == null)
            {
                if (model != null)
                {
                    ModelChanged(model);
                }
                return;
            }

            VFXGraphValidation validation = new VFXGraphValidation(m_Graph);
            validation.ValidateGraph();

            bool groupNodeChanged = false;

            Profiler.BeginSample("VFXViewController.GraphChanged:SyncControllerFromModel");
            SyncControllerFromModel(ref groupNodeChanged);
            Profiler.EndSample();

            Profiler.BeginSample("VFXViewController.GraphChanged:NotifyChange(AnyThing)");
            NotifyChange(AnyThing);
            Profiler.EndSample();

            //if( groupNodeChanged)
            {
                Profiler.BeginSample("VFXViewController.GraphChanged:NotifyChange(Change.groupNode)");
                NotifyChange(Change.groupNode);
                Profiler.EndSample();
            }
        }

        protected void UIChanged()
        {
            if (m_UI == null) return;
            if (m_Graph == null) return; // OnModelChange or OnDisable will take care of that later

            bool groupNodeChanged = false;
            RecreateUI(ref groupNodeChanged);

            NotifyChange(AnyThing);
        }

        public void NotifyParameterControllerChange()
        {
            DataEdgesMightHaveChanged();
            if (!m_Syncing)
                NotifyChange(AnyThing);
        }

        public void RegisterFlowAnchorController(VFXFlowAnchorController controller)
        {
            if (!m_FlowAnchorController.Contains(controller))
                m_FlowAnchorController.Add(controller);
        }

        public void UnregisterFlowAnchorController(VFXFlowAnchorController controller)
        {
            m_FlowAnchorController.Remove(controller);
        }

        public static void CollectAncestorOperator(IVFXSlotContainer operatorInput, HashSet<IVFXSlotContainer> hashParents)
        {
            foreach (var slotInput in operatorInput.inputSlots)
            {
                var linkedSlots = slotInput.AllChildrenWithLink();
                foreach (var linkedSlot in linkedSlots)
                {
                    RecurseCollectAncestorOperator(linkedSlot.refSlot.owner, hashParents);
                }
            }
        }

        public static void RecurseCollectAncestorOperator(IVFXSlotContainer operatorInput, HashSet<IVFXSlotContainer> hashParents)
        {
            if (hashParents.Contains(operatorInput))
                return;

            hashParents.Add(operatorInput);

            foreach (var slotInput in operatorInput.inputSlots)
            {
                var linkedSlots = slotInput.AllChildrenWithLink();
                foreach (var linkedSlot in linkedSlots)
                {
                    RecurseCollectAncestorOperator(linkedSlot.refSlot.owner, hashParents);
                }
            }
        }

        public static void CollectDescendantOperator(IVFXSlotContainer operatorInput, HashSet<IVFXSlotContainer> hashChildren)
        {
            foreach (var slotOutput in operatorInput.outputSlots)
            {
                var linkedSlots = slotOutput.AllChildrenWithLink();
                foreach (var linkedSlot in linkedSlots)
                {
                    foreach (var link in linkedSlot.LinkedSlots)
                    {
                        RecurseCollectDescendantOperator(link.owner, hashChildren);
                    }
                }
            }
        }

        public static void RecurseCollectDescendantOperator(IVFXSlotContainer operatorInput, HashSet<IVFXSlotContainer> hashChildren)
        {
            if (hashChildren.Contains(operatorInput))
                return;

            hashChildren.Add(operatorInput);
            foreach (var slotOutput in operatorInput.outputSlots)
            {
                var linkedSlots = slotOutput.AllChildrenWithLink();
                foreach (var linkedSlot in linkedSlots)
                {
                    foreach (var link in linkedSlot.LinkedSlots)
                    {
                        RecurseCollectDescendantOperator(link.owner, hashChildren);
                    }
                }
            }
        }

        public IEnumerable<VFXDataAnchorController> GetCompatiblePorts(VFXDataAnchorController startAnchorController, NodeAdapter nodeAdapter)
        {
            var cacheLinkData = new VFXDataAnchorController.CanLinkCache();

            var direction = startAnchorController.direction;
            foreach (var slotContainer in AllSlotContainerControllers)
            {
                var sourceSlot = direction == Direction.Input ? slotContainer.outputPorts : slotContainer.inputPorts;
                foreach (var slot in sourceSlot)
                {
                    if (startAnchorController.CanLink(slot, cacheLinkData))
                    {
                        yield return slot;
                    }
                }
            }
        }

        public List<VFXFlowAnchorController> GetCompatiblePorts(VFXFlowAnchorController startAnchorController, NodeAdapter nodeAdapter)
        {
            var res = new List<VFXFlowAnchorController>();

            var startFlowAnchorController = (VFXFlowAnchorController)startAnchorController;
            foreach (var anchorController in m_FlowAnchorController)
            {
                VFXContext owner = anchorController.owner;
                if (owner == null ||
                    startAnchorController == anchorController ||
                    startAnchorController.direction == anchorController.direction ||
                    owner == startFlowAnchorController.owner)
                    continue;

                var from = startFlowAnchorController.owner;
                var to = owner;
                if (startAnchorController.direction == Direction.Input)
                {
                    from = owner;
                    to = startFlowAnchorController.owner;
                }

                if (VFXContext.CanLink(from, to))
                    res.Add(anchorController);
            }
            return res;
        }

        private void AddVFXModel(Vector2 pos, VFXModel model)
        {
            model.position = pos;
            this.graph.AddChild(model);
        }

        public VFXContext AddVFXContext(Vector2 pos, VFXModelDescriptor<VFXContext> desc)
        {
            VFXContext model = desc.CreateInstance();
            AddVFXModel(pos, model);
            return model;
        }

        public VFXOperator AddVFXOperator(Vector2 pos, VFXModelDescriptor<VFXOperator> desc)
        {
            var model = desc.CreateInstance();
            AddVFXModel(pos, model);
            return model;
        }

        public VFXParameter AddVFXParameter(Vector2 pos, VFXModelDescriptorParameters desc)
        {
            var model = desc.CreateInstance();
            AddVFXModel(pos, model);

            VFXParameter parameter = model as VFXParameter;

            Type type = parameter.type;

            parameter.collapsed = true;

            int order = 0;
            if (m_ParameterControllers.Count > 0)
            {
                order = m_ParameterControllers.Keys.Select(t => t.order).Max() + 1;
            }
            parameter.order = order;
            parameter.SetSettingValue("m_exposedName", string.Format("New {0}", type.UserFriendlyName()));

            if (!type.IsPrimitive)
            {
                parameter.value = VFXTypeExtension.GetDefaultField(type);
            }

            return model;
        }

        public VFXNodeController AddNode(Vector2 tPos, object modelDescriptor, VFXGroupNodeController groupNode)
        {
            VFXModel newNode = null;
            if (modelDescriptor is VFXModelDescriptor<VFXOperator>)
            {
                newNode = AddVFXOperator(tPos, (modelDescriptor as VFXModelDescriptor<VFXOperator>));
            }
            else if (modelDescriptor is VFXModelDescriptor<VFXContext>)
            {
                newNode = AddVFXContext(tPos, modelDescriptor as VFXModelDescriptor<VFXContext>);
            }
            else if (modelDescriptor is VFXModelDescriptorParameters)
            {
                newNode = AddVFXParameter(tPos, modelDescriptor as VFXModelDescriptorParameters);
            }
            if (newNode != null)
            {
                bool groupNodeChanged = false;
                SyncControllerFromModel(ref groupNodeChanged);

                List<VFXNodeController> nodeControllers = null;
                m_SyncedModels.TryGetValue(newNode, out nodeControllers);

                if (newNode is VFXParameter)
                {
                    // Set an exposed name on a new parameter so that uncity is ensured
                    VFXParameter newParameter = newNode as VFXParameter;
                    m_ParameterControllers[newParameter].exposedName = string.Format("New {0}", newParameter.type.UserFriendlyName());
                }

                NotifyChange(AnyThing);

                if (groupNode != null)
                {
                    groupNode.AddNode(nodeControllers.First());
                }

                return nodeControllers[0];
            }

            return null;
        }

        public VFXNodeController AddVFXParameter(Vector2 pos, VFXParameterController parameterController, VFXGroupNodeController groupNode)
        {
            int id = parameterController.model.AddNode(pos);

            LightApplyChanges();

            var nodeController = GetRootNodeController(parameterController.model, id);

            if (groupNode != null)
            {
                if (nodeController != null)
                {
                    groupNode.AddNode(nodeController);
                }
            }

            return nodeController;
        }

        public void Clear()
        {
            foreach (var element in allChildren)
            {
                element.OnDisable();
            }

            m_FlowAnchorController.Clear();
            m_SyncedModels.Clear();
            m_ParameterControllers.Clear();
            m_DataEdges.Clear();
            m_FlowEdges.Clear();
            m_GroupNodeControllers.Clear();
            m_StickyNoteControllers.Clear();
        }

        private Dictionary<VFXModel, List<VFXModel.InvalidateEvent>> m_registeredEvent = new Dictionary<VFXModel, List<VFXModel.InvalidateEvent>>();
        public void AddInvalidateDelegate(VFXModel model, VFXModel.InvalidateEvent evt)
        {
            model.onInvalidateDelegate += evt;
            if (!m_registeredEvent.ContainsKey(model))
            {
                m_registeredEvent.Add(model, new List<VFXModel.InvalidateEvent>());
            }
            m_registeredEvent[model].Add(evt);
        }

        public void RemoveInvalidateDelegate(VFXModel model, VFXModel.InvalidateEvent evt)
        {
            List<VFXModel.InvalidateEvent> evtList;
            if (model != null && m_registeredEvent.TryGetValue(model, out evtList))
            {
                model.onInvalidateDelegate -= evt;
                evtList.Remove(evt);
                if (evtList.Count == 0)
                {
                    m_registeredEvent.Remove(model);
                }
            }
        }

        static Dictionary<VisualEffectResource, VFXViewController> s_Controllers = new Dictionary<VisualEffectResource, VFXViewController>();

        public static VFXViewController GetController(VisualEffectResource resource, bool forceUpdate = false)
        {
            //TRANSITION : delete VFXAsset as it should be in Library
            resource.ValidateAsset();

            VFXViewController controller;
            if (!s_Controllers.TryGetValue(resource, out controller))
            {
                controller = new VFXViewController(resource);
                s_Controllers[resource] = controller;
            }
            else
            {
                if (forceUpdate)
                {
                    controller.ForceReload();
                }
            }

            return controller;
        }

        static void RemoveController(VFXViewController controller)
        {
            if (s_Controllers.ContainsKey(controller.model))
            {
                controller.OnDisable();
                s_Controllers.Remove(controller.model);
            }
        }

        VFXViewController(VisualEffectResource vfx) : base(vfx)
        {
            ModelChanged(vfx); // This will initialize the graph from the vfx asset.

            if (m_FlowAnchorController == null)
                m_FlowAnchorController = new List<VFXFlowAnchorController>();

            Undo.undoRedoPerformed += SynchronizeUndoRedoState;
            Undo.willFlushUndoRecord += WillFlushUndoRecord;

            string fileName = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(vfx));
            vfx.name = fileName;

            if (m_Graph != null)
                m_Graph.BuildParameterInfo();

            InitializeUndoStack();
            GraphChanged();

            Sanitize();
        }

        void Sanitize()
        {
            VFXParameter[] parameters = m_ParameterControllers.Keys.OrderBy(t => t.order).ToArray();
            if (parameters.Length > 0)
            {
                var existingNames = new HashSet<string>();

                existingNames.Add(parameters[0].exposedName);
                m_ParameterControllers[parameters[0]].order = 0;

                for (int i = 1; i < parameters.Length; ++i)
                {
                    var controller = m_ParameterControllers[parameters[i]];
                    controller.order = i;

                    controller.CheckNameUnique(existingNames);

                    existingNames.Add(parameters[i].exposedName);
                }
            }
        }

        public ReadOnlyCollection<VFXGroupNodeController> groupNodes
        {
            get {return m_GroupNodeControllers.AsReadOnly(); }
        }
        public ReadOnlyCollection<VFXStickyNoteController> stickyNotes
        {
            get {return m_StickyNoteControllers.AsReadOnly(); }
        }

        List<VFXGroupNodeController> m_GroupNodeControllers = new List<VFXGroupNodeController>();
        List<VFXStickyNoteController> m_StickyNoteControllers = new List<VFXStickyNoteController>();

        public bool RecreateUI(ref bool groupNodeChanged)
        {
            bool changed = false;
            var ui = graph.UIInfos;

            if (ui.groupInfos != null)
            {
                HashSet<VFXNodeID> usedNodeIds = new HashSet<VFXNodeID>();
                // first make sure that nodesID are at most in one groupnode.

                for (int i = 0; i < ui.groupInfos.Length; ++i)
                {
                    if (ui.groupInfos[i].contents != null)
                    {
                        for (int j = 0; j < ui.groupInfos[i].contents.Length; ++j)
                        {
                            if (usedNodeIds.Contains(ui.groupInfos[i].contents[j]))
                            {
                                Debug.Log("Element present in multiple groupnodes");
                                --j;
                                ui.groupInfos[i].contents = ui.groupInfos[i].contents.Where((t, k) => k != j).ToArray();
                            }
                            else
                            {
                                usedNodeIds.Add(ui.groupInfos[i].contents[j]);
                            }
                        }
                    }
                }

                for (int i = m_GroupNodeControllers.Count; i < ui.groupInfos.Length; ++i)
                {
                    VFXGroupNodeController groupNodeController = new VFXGroupNodeController(this, ui, i);
                    m_GroupNodeControllers.Add(groupNodeController);
                    changed = true;
                    groupNodeChanged = true;
                }

                while (ui.groupInfos.Length < m_GroupNodeControllers.Count)
                {
                    m_GroupNodeControllers.Last().OnDisable();
                    m_GroupNodeControllers.RemoveAt(m_GroupNodeControllers.Count - 1);
                    changed = true;
                    groupNodeChanged = true;
                }
            }
            if (ui.stickyNoteInfos != null)
            {
                for (int i = m_StickyNoteControllers.Count; i < ui.stickyNoteInfos.Length; ++i)
                {
                    VFXStickyNoteController stickyNoteController = new VFXStickyNoteController(this, ui, i);
                    m_StickyNoteControllers.Add(stickyNoteController);
                    stickyNoteController.ApplyChanges();
                    changed = true;
                }

                while (ui.stickyNoteInfos.Length < m_StickyNoteControllers.Count)
                {
                    m_StickyNoteControllers.Last().OnDisable();
                    m_StickyNoteControllers.RemoveAt(m_StickyNoteControllers.Count - 1);
                    changed = true;
                }
            }

            return changed;
        }

        public void ValidateCategoryList()
        {
            if (!m_Syncing)
            {
                var ui = graph.UIInfos;
                // Validate category list
                var categories = ui.categories != null ? ui.categories : new List<VFXUI.CategoryInfo>();

                string[] missingCategories = m_ParameterControllers.Select(t => t.Key.category).Where(t => !string.IsNullOrEmpty(t)).Except(categories.Select(t => t.name)).ToArray();

                HashSet<string> foundCategories = new HashSet<string>();

                for (int i = 0; i < categories.Count; ++i)
                {
                    string category = categories[i].name;
                    if (string.IsNullOrEmpty(category) || foundCategories.Contains(category))
                    {
                        categories.RemoveAt(i);
                        --i;
                    }
                    foundCategories.Add(category);
                }

                if (missingCategories.Length > 0)
                {
                    categories.AddRange(missingCategories.Select(t => new VFXUI.CategoryInfo { name = t}));
                    ui.categories = categories;
                    ui.Modified();
                }
            }
        }

        public void ForceReload()
        {
            Clear();
            ModelChanged(model);
            GraphChanged();
        }

        bool m_Syncing;

        public bool SyncControllerFromModel(ref bool groupNodeChanged)
        {
            m_Syncing = true;
            bool changed = false;
            var toRemove = m_SyncedModels.Keys.Except(graph.children).ToList();
            foreach (var m in toRemove)
            {
                RemoveControllersFromModel(m);
                changed = true;
            }

            var toAdd = graph.children.Except(m_SyncedModels.Keys).ToList();
            foreach (var m in toAdd)
            {
                AddControllersFromModel(m);
                changed = true;
            }


            // make sure every parameter instance is created before we look for edges
            foreach (var parameter in m_ParameterControllers.Values)
            {
                parameter.UpdateControllers();
            }

            changed |= RecreateNodeEdges();
            changed |= RecreateFlowEdges();

            changed |= RecreateUI(ref groupNodeChanged);

            m_Syncing = false;
            ValidateCategoryList();
            UpdateSystems();
            return changed;
        }

        Dictionary<VFXParameter, VFXParameterController> m_ParameterControllers = new Dictionary<VFXParameter, VFXParameterController>();

        public IEnumerable<VFXParameterController> parameterControllers
        {
            get { return m_ParameterControllers.Values; }
        }


        public void MoveCategory(string category, int index)
        {
            if (graph.UIInfos.categories == null)
                return;
            int oldIndex = graph.UIInfos.categories.FindIndex(t => t.name == category);

            if (oldIndex == -1 || oldIndex == index)
                return;
            graph.UIInfos.categories.RemoveAt(oldIndex);
            if (index < graph.UIInfos.categories.Count)
                graph.UIInfos.categories.Insert(index, new VFXUI.CategoryInfo { name = category });
            else
                graph.UIInfos.categories.Add(new VFXUI.CategoryInfo { name = category });

            graph.Invalidate(VFXModel.InvalidationCause.kUIChanged);
        }

        public bool SetCategoryName(int category, string newName)
        {
            if (category >= 0 && graph.UIInfos.categories != null && category < graph.UIInfos.categories.Count)
            {
                if (graph.UIInfos.categories[category].name == newName)
                {
                    return false;
                }
                if (!graph.UIInfos.categories.Any(t => t.name == newName))
                {
                    var oldName = graph.UIInfos.categories[category].name;

                    foreach (var parameter in m_ParameterControllers)
                    {
                        if (parameter.Key.category == oldName)
                        {
                            parameter.Key.category = newName;
                        }
                    }

                    var catInfo = graph.UIInfos.categories[category];
                    catInfo.name = newName;
                    graph.UIInfos.categories[category] = catInfo;

                    graph.Invalidate(VFXModel.InvalidationCause.kUIChanged);
                    return true;
                }
                else
                {
                    Debug.LogError("Can't change name, category with the same name already exists");
                }
            }
            else
            {
                Debug.LogError("Can't change name, category not found");
            }

            return false;
        }

        public void RemoveCategory(string name)
        {
            int index = graph.UIInfos.categories.FindIndex(t => t.name == name);

            if (index > -1)
            {
                var parametersToRemove = RemoveCategory(index);

                Remove(parametersToRemove.Cast<Controller>());
            }
        }

        public IEnumerable<VFXParameterController> RemoveCategory(int category)
        {
            if (category >= 0 && graph.UIInfos.categories != null && category < graph.UIInfos.categories.Count)
            {
                string name = graph.UIInfos.categories[category].name;

                graph.UIInfos.categories.RemoveAt(category);
                graph.Invalidate(VFXModel.InvalidationCause.kUIChanged);

                return m_ParameterControllers.Values.Where(t => t.model.category == name);
            }
            return Enumerable.Empty<VFXParameterController>();
        }

        public void SetParametersOrder(VFXParameterController controller, int index, string category)
        {
            var orderedParameters = m_ParameterControllers.Where(t => t.Key.category == category).OrderBy(t => t.Value.order).Select(t => t.Value).ToList();

            int oldIndex = orderedParameters.IndexOf(controller);


            if (oldIndex != -1)
            {
                orderedParameters.RemoveAt(oldIndex);

                if (oldIndex < index)
                {
                    --index;
                }
            }

            controller.model.category = category;

            if (index < orderedParameters.Count)
            {
                orderedParameters.Insert(index, controller);
            }
            else
            {
                orderedParameters.Add(controller);
            }

            for (int i = 0; i < orderedParameters.Count; ++i)
            {
                orderedParameters[i].order = i;
            }
            NotifyChange(AnyThing);
        }

        public void SetCategoryExpanded(string category, bool expanded)
        {
            if (graph.UIInfos.categories != null)
            {
                for (int i = 0; i < graph.UIInfos.categories.Count; ++i)
                {
                    if (graph.UIInfos.categories[i].name == category)
                    {
                        graph.UIInfos.categories[i] = new VFXUI.CategoryInfo { name = category, collapsed = !expanded };
                    }
                }
            }
            NotifyChange(AnyThing);
        }

        private void AddControllersFromModel(VFXModel model)
        {
            List<VFXNodeController> newControllers = new List<VFXNodeController>();
            if (model is VFXOperator)
            {
                if (model is VFXOperatorNumericCascadedUnified)
                    newControllers.Add(new VFXCascadedOperatorController(model, this));
                else if (model is VFXOperatorNumericUniform)
                {
                    newControllers.Add(new VFXNumericUniformOperatorController(model, this));
                }
                else if (model is VFXOperatorNumericUnified)
                {
                    if (model is IVFXOperatorNumericUnifiedConstrained)
                        newControllers.Add(new VFXUnifiedConstraintOperatorController(model, this));
                    else
                        newControllers.Add(new VFXUnifiedOperatorController(model, this));
                }
                else if (model is Branch)
                {
                    newControllers.Add(new VFXBranchOperatorController(model, this));
                }
                else
                    newControllers.Add(new VFXOperatorController(model, this));
            }
            else if (model is VFXContext)
            {
                newControllers.Add(new VFXContextController(model, this));
            }
            else if (model is VFXParameter)
            {
                VFXParameter parameter = model as VFXParameter;
                parameter.ValidateNodes();

                m_ParameterControllers[parameter] = new VFXParameterController(parameter, this);

                m_SyncedModels[model] = new List<VFXNodeController>();
            }

            if (newControllers.Count > 0)
            {
                List<VFXNodeController> existingControllers;
                if (m_SyncedModels.TryGetValue(model, out existingControllers))
                {
                    Debug.LogError("adding a model to controllers twice");
                }
                m_SyncedModels[model] = newControllers;
                foreach (var controller in newControllers)
                {
                    controller.ForceUpdate();
                }
            }
        }

        public void AddControllerToModel(VFXModel model, VFXNodeController controller)
        {
            m_SyncedModels[model].Add(controller);
        }

        public void RemoveControllerFromModel(VFXModel model, VFXNodeController controller)
        {
            m_SyncedModels[model].Remove(controller);
        }

        private void RemoveControllersFromModel(VFXModel model)
        {
            List<VFXNodeController> controllers = null;
            if (m_SyncedModels.TryGetValue(model, out controllers))
            {
                foreach (var controller in controllers)
                {
                    controller.OnDisable();
                }
                m_SyncedModels.Remove(model);
            }
            if (model is VFXParameter)
            {
                m_ParameterControllers[model as VFXParameter].OnDisable();
                m_ParameterControllers.Remove(model as VFXParameter);
            }
        }

        public VFXNodeController GetNodeController(VFXModel model, int id)
        {
            if (model is VFXBlock)
            {
                VFXContextController controller = GetRootNodeController(model.GetParent(), 0) as VFXContextController;
                if (controller == null)
                    return null;
                return controller.blockControllers.FirstOrDefault(t => t.model == model);
            }
            else
            {
                return GetRootNodeController(model, id);
            }
        }

        public void ChangeEventName(string oldName, string newName)
        {
            foreach (var context in m_SyncedModels.Keys.OfType<VFXBasicEvent>())
            {
                if (context.eventName == oldName)
                    context.SetSettingValue("eventName", newName);
            }
        }

        public VFXNodeController GetRootNodeController(VFXModel model, int id)
        {
            List<VFXNodeController> controller = null;
            m_SyncedModels.TryGetValue(model, out controller);
            if (controller == null) return null;

            return controller.FirstOrDefault(t => t.id == id);
        }

        public VFXStickyNoteController GetStickyNoteController(int index)
        {
            return m_StickyNoteControllers[index];
        }

        public VFXParameterController GetParameterController(VFXParameter parameter)
        {
            VFXParameterController controller = null;
            m_ParameterControllers.TryGetValue(parameter, out controller);
            return controller;
        }

        VFXUI.GroupInfo PrivateAddGroupNode(Vector2 position)
        {
            var ui = graph.UIInfos;

            var newGroupInfo = new VFXUI.GroupInfo { title = "New Group Node", position = new Rect(position, Vector2.one * 100) };

            if (ui.groupInfos != null)
                ui.groupInfos = ui.groupInfos.Concat(Enumerable.Repeat(newGroupInfo, 1)).ToArray();
            else
                ui.groupInfos = new VFXUI.GroupInfo[] { newGroupInfo };

            return ui.groupInfos.Last();
        }

        public void GroupNodes(IEnumerable<VFXNodeController> nodes)
        {

            foreach( var g in groupNodes) // remove nodes from other exisitings groups
            {
                g.RemoveNodes(nodes);
            }
            VFXUI.GroupInfo info = PrivateAddGroupNode(Vector2.zero);

            info.contents = nodes.Select(t => new VFXNodeID(t.model, t.id)).ToArray();

            m_Graph.Invalidate(VFXModel.InvalidationCause.kUIChanged);
        }

        public void PutInSameGroupNodeAs(VFXNodeController target, VFXNodeController example)
        {
            var ui = graph.UIInfos;
            if (ui.groupInfos == null) return;

            foreach (var groupNode in m_GroupNodeControllers)
            {
                if (groupNode.nodes.Contains(example))
                {
                    groupNode.AddNode(target);
                    break;
                }
            }
        }


        List<VFXSystemController> m_Systems = new List<VFXSystemController>();

        public ReadOnlyCollection<VFXSystemController> systems
        {
            get { return m_Systems.AsReadOnly(); }
        }

        public void UpdateSystems()
        {
            VFXContext[] contexts = graph.children.OfType<VFXContext>().ToArray();

            HashSet<VFXContext> initializes = new HashSet<VFXContext>(contexts.Where(t => t.contextType == VFXContextType.kInit).ToArray());
            HashSet<VFXContext> updates = new HashSet<VFXContext>(contexts.Where(t => t.contextType == VFXContextType.kUpdate).ToArray());

            List<Dictionary<VFXContext, int>> systems = new List<Dictionary<VFXContext, int>>();


            while (initializes.Count > 0 || updates.Count > 0)
            {
                int generation = 0;

                VFXContext currentContext;
                if (initializes.Count > 0)
                {
                    currentContext = initializes.First();
                    initializes.Remove(currentContext);
                }
                else
                {
                    currentContext = updates.First();
                    updates.Remove(currentContext);
                }


                Dictionary<VFXContext, int> system = new Dictionary<VFXContext, int>();

                system.Add(currentContext, generation);

                var allChildren = currentContext.outputFlowSlot.Where(t => t != null).SelectMany(t => t.link.Select(u => u.context)).Where(t => t != null).ToList();
                while (allChildren.Count() > 0)
                {
                    ++generation;

                    foreach (var child in allChildren)
                    {
                        initializes.Remove(child);
                        updates.Remove(child);
                        system.Add(child, generation);
                    }

                    var allSubChildren = allChildren.SelectMany(t => t.outputFlowSlot.Where(u => u != null).SelectMany(u => u.link.Select(v => v.context).Where(v => v != null)));
                    var allPreChildren = allChildren.SelectMany(t => t.inputFlowSlot.Where(u => u != null).SelectMany(u => u.link.Select(v => v.context).Where(v => v != null && v.contextType != VFXContextType.kSpawner && v.contextType != VFXContextType.kSpawnerGPU)));

                    allChildren = allSubChildren.Concat(allPreChildren).Except(system.Keys).ToList();
                }

                if (system.Count > 1)
                    systems.Add(system);
            }

            while (m_Systems.Count() < systems.Count())
            {
                VFXSystemController systemController = new VFXSystemController(this,graph.UIInfos);
                m_Systems.Add(systemController);
            }

            while (m_Systems.Count() > systems.Count())
            {
                VFXSystemController systemController = m_Systems.Last();
                m_Systems.RemoveAt(m_Systems.Count - 1);
                systemController.OnDisable();
            }

            for (int i = 0; i < systems.Count(); ++i)
            {
                var contextToController = systems[i].Keys.Select(t => new KeyValuePair<VFXContextController, VFXContext>((VFXContextController)GetNodeController(t, 0), t)).Where(t => t.Key != null).ToDictionary(t => t.Value, t => t.Key);
                m_Systems[i].contexts = contextToController.Values.ToArray();
                m_Systems[i].title = graph.UIInfos.GetNameOfSystem(systems[i].Keys);

                VFXContextType type = VFXContextType.kNone;
                VFXContext prevContext = null;
                var orderedContexts = systems[i].Keys.OrderBy(t => t.contextType).ThenBy(t => systems[i][t]).ThenBy(t => t.position.x).ThenBy(t => t.position.y).ToArray();

                char letter = 'A';
                foreach (var context in orderedContexts)
                {
                    if (context.contextType == type)
                    {
                        if (prevContext != null)
                        {
                            letter = 'A';
                            contextToController[prevContext].letter = letter;
                            prevContext = null;
                        }

                        if (letter == 'Z') // loop back to A in the unlikely event that there are more than 26 contexts
                            letter = 'a';
                        else if( letter == 'z')
                            letter = 'α';
                        else if( letter == 'ω')
                            letter = 'A';
                        contextToController[context].letter = ++letter;
                    }
                    else
                    {
                        contextToController[context].letter = '\0';
                        prevContext = context;
                    }
                    type = context.contextType;
                }

            }
        }

        private VFXGraph m_Graph;

        private VFXUI m_UI;

        private VFXView m_View; // Don't call directly as it is lazy initialized
    }
}
