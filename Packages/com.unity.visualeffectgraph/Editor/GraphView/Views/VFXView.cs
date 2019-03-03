//#define OLD_COPY_PASTE
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Profiling;
using System.Reflection;

namespace UnityEditor.VFX.UI
{
    class VFXView : GraphView, IDropTarget, IControlledElement<VFXViewController>, IControllerListener
    {
        public HashSet<VFXEditableDataAnchor> allDataAnchors = new HashSet<VFXEditableDataAnchor>();

        void IControllerListener.OnControllerEvent(ControllerEvent e)
        {
            if (e is VFXRecompileEvent)
            {
                var recompileEvent = e as VFXRecompileEvent;
                foreach (var anchor in allDataAnchors)
                {
                    anchor.OnRecompile(recompileEvent.valueOnly);
                }
            }
        }

        VisualElement m_NoAssetLabel;

        VFXViewController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }


        void DisconnectController()
        {
            if (controller.model && controller.graph)
                controller.graph.SetCompilationMode(VFXCompilationMode.Runtime);

            m_Controller.UnregisterHandler(this);
            m_Controller.useCount--;

            serializeGraphElements = null;
            unserializeAndPaste = null;
            deleteSelection = null;
            nodeCreationRequest = null;

            elementsAddedToGroup = null;
            elementsRemovedFromGroup = null;
            groupTitleChanged = null;

            m_GeometrySet = false;

            // Remove all in view now that the controller has been disconnected.
            foreach (var element in rootGroupNodeElements.Values)
            {
                RemoveElement(element);
            }
            foreach (var element in groupNodes.Values)
            {
                RemoveElement(element);
            }
            foreach (var element in dataEdges.Values)
            {
                RemoveElement(element);
            }
            foreach (var element in flowEdges.Values)
            {
                RemoveElement(element);
            }
            foreach (var system in m_Systems)
            {
                RemoveElement(system);
            }

            groupNodes.Clear();
            stickyNotes.Clear();
            rootNodes.Clear();
            rootGroupNodeElements.Clear();
            m_Systems.Clear();
            VFXExpression.ClearCache();
            m_NodeProvider = null;
        }

        void ConnectController()
        {
            schedule.Execute(() =>
            {
                if (controller != null && controller.graph)
                    controller.graph.SetCompilationMode(m_IsRuntimeMode ? VFXCompilationMode.Runtime : VFXCompilationMode.Edition);
            }).ExecuteLater(1);

            m_Controller.RegisterHandler(this);
            m_Controller.useCount++;

            serializeGraphElements = SerializeElements;
            unserializeAndPaste = UnserializeAndPasteElements;
            deleteSelection = Delete;
            nodeCreationRequest = OnCreateNode;

            elementsAddedToGroup = ElementAddedToGroupNode;
            elementsRemovedFromGroup = ElementRemovedFromGroupNode;
            groupTitleChanged = GroupNodeTitleChanged;

            m_NodeProvider = new VFXNodeProvider(controller, (d, mPos) => AddNode(d, mPos));
        }

        public VisualEffect attachedComponent
        {
            get
            {
                return m_ComponentBoard != null ? m_ComponentBoard.GetAttachedComponent() : null;
            }

            set
            {
                if (m_ComponentBoard == null)
                    ShowComponentBoard();
                if (m_ComponentBoard != null)
                    m_ComponentBoard.Attach(value);
            }
        }

        public VFXViewController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        DisconnectController();
                    }
                    m_Controller = value;
                    if (m_Controller != null)
                    {
                        ConnectController();
                    }
                    NewControllerSet();
                }
            }
        }

        public VFXGroupNode GetPickedGroupNode(Vector2 panelPosition)
        {
            List<VisualElement> picked = new List<VisualElement>();
            panel.PickAll(panelPosition, picked);

            return picked.OfType<VFXGroupNode>().FirstOrDefault();
        }

        public VFXNodeController AddNode(VFXNodeProvider.Descriptor d, Vector2 mPos)
        {
            var groupNode = GetPickedGroupNode(mPos);

            mPos = this.ChangeCoordinatesTo(contentViewContainer, mPos);


            if (d.modelDescriptor is string)
            {
                string path = d.modelDescriptor as string;

                CreateTemplateSystem(path, mPos, groupNode);
            }
            else if (d.modelDescriptor is GroupNodeAdder)
            {
                controller.AddGroupNode(mPos);
            }
            else if (d.modelDescriptor is VFXParameterController)
            {
                var parameter = d.modelDescriptor as VFXParameterController;

                return controller.AddVFXParameter(mPos, parameter, groupNode != null ? groupNode.controller : null);
            }
            else
                return controller.AddNode(mPos, d.modelDescriptor, groupNode != null ? groupNode.controller : null);
            return null;
        }

        VFXNodeProvider m_NodeProvider;

        VisualElement m_Toolbar;

        public VFXView()
        {
            SetupZoom(0.125f, 8);

            //this.AddManipulator(new SelectionSetter(this));
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            AddStyleSheetPath("VFXView");

            AddLayer(-1);
            AddLayer(1);
            AddLayer(2);

            focusIndex = 0;

            m_Toolbar = new VisualElement();
            m_Toolbar.AddToClassList("toolbar");


            Button button = new Button(() => { Resync(); });
            button.text = "Refresh";
            button.AddToClassList("toolbarItem");
            m_Toolbar.Add(button);
            button = new Button(() => { SelectAsset(); });
            button.text = "Select Asset";
            button.AddToClassList("toolbarItem");
            m_Toolbar.Add(button);

            VisualElement spacer = new VisualElement();
            spacer.style.width = 10;
            m_Toolbar.Add(spacer);

            Toggle toggleBlackboard = new Toggle();
            toggleBlackboard.text = "Blackboard";
            toggleBlackboard.AddToClassList("toolbarItem");
            toggleBlackboard.RegisterCallback<ChangeEvent<bool>>(ToggleBlackboard);
            m_Toolbar.Add(toggleBlackboard);

            Toggle toggleComponentBoard = new Toggle();
            toggleComponentBoard.text = "Target GameObject";
            toggleComponentBoard.AddToClassList("toolbarItem");
            toggleComponentBoard.RegisterCallback<ChangeEvent<bool>>(ToggleComponentBoard);
            m_Toolbar.Add(toggleComponentBoard);


            spacer = new VisualElement();
            spacer.style.flex = new Flex(1);
            m_Toolbar.Add(spacer);

            Toggle toggleRuntimeMode = new Toggle();
            toggleRuntimeMode.text = "Force Runtime Mode";
            toggleRuntimeMode.SetValueWithoutNotify(m_IsRuntimeMode);
            toggleRuntimeMode.RegisterCallback<ChangeEvent<bool>>(OnToggleRuntimeMode);
            m_Toolbar.Add(toggleRuntimeMode);
            toggleRuntimeMode.AddToClassList("toolbarItem");

            Toggle toggleAutoCompile = new Toggle();
            toggleAutoCompile.text = "Auto Compile";
            toggleAutoCompile.SetValueWithoutNotify(true);
            toggleAutoCompile.RegisterCallback<ChangeEvent<bool>>(OnToggleCompile);
            m_Toolbar.Add(toggleAutoCompile);
            toggleAutoCompile.AddToClassList("toolbarItem");

            button = new Button(OnCompile);
            button.text = "Compile";
            button.AddToClassList("toolbarItem");
            m_Toolbar.Add(button);


            m_NoAssetLabel = new Label("Please Select An Asset");
            m_NoAssetLabel.style.positionType = PositionType.Absolute;
            m_NoAssetLabel.style.positionLeft = 0;
            m_NoAssetLabel.style.positionRight = 0;
            m_NoAssetLabel.style.positionTop = 0;
            m_NoAssetLabel.style.positionBottom = 0;
            m_NoAssetLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_NoAssetLabel.style.fontSize = 72;
            m_NoAssetLabel.style.color = Color.white * 0.75f;

            Add(m_NoAssetLabel);

            m_Blackboard = new VFXBlackboard(this);


            bool blackboardVisible = BoardPreferenceHelper.IsVisible(BoardPreferenceHelper.Board.blackboard, true);
            if (blackboardVisible)
                Add(m_Blackboard);
            toggleBlackboard.value = blackboardVisible;


            bool componentBoardVisible = BoardPreferenceHelper.IsVisible(BoardPreferenceHelper.Board.blackboard, false);
            if (componentBoardVisible)
                ShowComponentBoard();
            toggleComponentBoard.value = componentBoardVisible;

            Add(m_Toolbar);

            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<ValidateCommandEvent>(ValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(ExecuteCommand);

            graphViewChanged = VFXGraphViewChanged;

            elementResized = VFXElementResized;

            Undo.undoRedoPerformed = OnUndoPerformed;

            persistenceKey = "VFXView";


            RegisterCallback<GeometryChangedEvent>(OnFirstResize);
        }

        public void SetBoardToFront(GraphElement board)
        {
            board.SendToBack();
            board.PlaceBehind(m_Toolbar);
        }

        void OnUndoPerformed()
        {
            foreach (var anchor in allDataAnchors)
            {
                anchor.ForceUpdate();
            }
        }

        void ToggleBlackboard(ChangeEvent<bool> e)
        {
            if (m_Blackboard.parent == null)
            {
                Insert(childCount - 1, m_Blackboard);
                BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
                m_Blackboard.RegisterCallback<GeometryChangedEvent>(OnFirstBlackboardGeometryChanged);
                m_Blackboard.style.positionType = PositionType.Absolute;
            }
            else
            {
                m_Blackboard.RemoveFromHierarchy();
                BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, false);
            }
        }

        void ShowComponentBoard()
        {
            if (m_ComponentBoard == null)
            {
                m_ComponentBoard = new VFXComponentBoard(this);

                m_ComponentBoard.controller = controller;
            }
            Insert(childCount - 1, m_ComponentBoard);

            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.componentBoard, true);

            m_ComponentBoard.RegisterCallback<GeometryChangedEvent>(OnFirstComponentBoardGeometryChanged);
        }

        void OnFirstComponentBoardGeometryChanged(GeometryChangedEvent e)
        {
            if (m_FirstResize)
            {
                m_ComponentBoard.ValidatePosition();
                m_ComponentBoard.UnregisterCallback<GeometryChangedEvent>(OnFirstComponentBoardGeometryChanged);
            }
        }

        void OnFirstBlackboardGeometryChanged(GeometryChangedEvent e)
        {
            if (m_FirstResize)
            {
                m_Blackboard.ValidatePosition();
                m_Blackboard.UnregisterCallback<GeometryChangedEvent>(OnFirstBlackboardGeometryChanged);
            }
        }

        public bool m_FirstResize = false;

        void OnFirstResize(GeometryChangedEvent e)
        {
            m_FirstResize = true;
            if (m_ComponentBoard != null)
                m_ComponentBoard.ValidatePosition();
            if (m_Blackboard != null)
                m_Blackboard.ValidatePosition();

            UnregisterCallback<GeometryChangedEvent>(OnFirstResize);
        }

        void ToggleComponentBoard(ChangeEvent<bool> e)
        {
            if (m_ComponentBoard == null || m_ComponentBoard.parent == null)
            {
                ShowComponentBoard();
            }
            else
            {
                m_ComponentBoard.RemoveFromHierarchy();
                BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.componentBoard, false);
            }
        }

        void Delete(string cmd, AskUser askUser)
        {
            var selection = this.selection.ToArray();


            var parametersToRemove = Enumerable.Empty<VFXParameterController>();

            foreach (var category in selection.OfType<VFXBlackboardCategory>())
            {
                parametersToRemove = parametersToRemove.Concat(controller.RemoveCategory(m_Blackboard.GetCategoryIndex(category)));
            }
            controller.Remove(selection.OfType<IControlledElement>().Select(t => t.controller).Concat(parametersToRemove.Cast<Controller>()));
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                ControllerChanged(e.change);
            }
            else if (e.controller is VFXNodeController)
            {
                UpdateUIBounds();
                if (e.controller is VFXContextController)
                {
                    if (m_ComponentBoard != null)
                    {
                        m_ComponentBoard.UpdateEventList();
                    }
                }
            }
        }

        bool m_InControllerChanged;

        void ControllerChanged(int change)
        {
            if (change == VFXViewController.Change.assetName)
                return;

            m_InControllerChanged = true;

            if (change == VFXViewController.Change.groupNode)
            {
                Profiler.BeginSample("VFXView.SyncGroupNodes");
                SyncGroupNodes();
                Profiler.EndSample();

                var groupNodes = this.groupNodes;
                foreach (var groupNode in groupNodes.Values)
                {
                    Profiler.BeginSample("VFXGroupNode.SelfChange");
                    groupNode.SelfChange();
                    Profiler.EndSample();
                }
                return;
            }
            if (change == VFXViewController.Change.destroy)
            {
                m_Blackboard.controller = null;
                controller = null;
                return;
            }
            Profiler.BeginSample("VFXView.ControllerChanged");
            if (change == VFXViewController.AnyThing)
            {
                SyncNodes();
            }

            Profiler.BeginSample("VFXView.SyncStickyNotes");
            SyncStickyNotes();
            Profiler.EndSample();
            Profiler.BeginSample("VFXView.SyncEdges");
            SyncEdges(change);
            Profiler.EndSample();
            Profiler.BeginSample("VFXView.SyncGroupNodes");
            SyncGroupNodes();
            Profiler.EndSample();

            if (controller != null)
            {
                if (change == VFXViewController.AnyThing)
                {
                    // if the asset is destroyed somehow, fox example if the user delete the asset, update the controller and update the window.
                    var asset = controller.model;
                    if (asset == null)
                    {
                        this.controller = null;
                        return;
                    }
                }
            }

            m_InControllerChanged = false;
            if(change != VFXViewController.Change.dataEdge)
                UpdateSystems();

            if (m_UpdateUIBounds)
            {
                Profiler.BeginSample("VFXView.UpdateUIBounds");
                UpdateUIBounds();
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }

        public override void OnPersistentDataReady()
        {
            // warning : default could messes with view restoration from the VFXViewWindow (TODO : check this)
            base.OnPersistentDataReady();
        }

        void NewControllerSet()
        {
            m_Blackboard.controller = controller;

            if (m_ComponentBoard != null)
            {
                m_ComponentBoard.controller = controller;
            }
            if (controller != null)
            {
                m_NoAssetLabel.RemoveFromHierarchy();

                pasteOffset = Vector2.zero; // if we change asset we want to paste exactly at the same place as the original asset the first time.
            }
            else
            {
                if (m_NoAssetLabel.parent == null)
                {
                    Add(m_NoAssetLabel);
                }
            }
        }

        public void FrameNewController()
        {
            if (panel != null)
            {
                FrameAfterAWhile();
            }
            else
            {
                RegisterCallback<GeometryChangedEvent>(OnFrameNewControllerWithPanel);
            }
        }

        void FrameAfterAWhile()
        {
            var rectToFit = contentViewContainer.layout;
            var frameTranslation = Vector3.zero;
            var frameScaling = Vector3.one;

            rectToFit = controller.graph.UIInfos.uiBounds;

            if (rectToFit.width <= 50 || rectToFit.height <= 50)
            {
                return;
            }

            CalculateFrameTransform(rectToFit, layout, 30, out frameTranslation, out frameScaling);

            Matrix4x4.TRS(frameTranslation, Quaternion.identity, frameScaling);

            UpdateViewTransform(frameTranslation, frameScaling);

            contentViewContainer.MarkDirtyRepaint();
        }

        bool m_GeometrySet = false;

        void OnFrameNewControllerWithPanel(GeometryChangedEvent e)
        {
            m_GeometrySet = true;
            FrameAfterAWhile();
            UnregisterCallback<GeometryChangedEvent>(OnFrameNewControllerWithPanel);
        }

        Dictionary<VFXNodeController, VFXNodeUI> rootNodes = new Dictionary<VFXNodeController, VFXNodeUI>();
        Dictionary<Controller, GraphElement> rootGroupNodeElements = new Dictionary<Controller, GraphElement>();


        public GraphElement GetGroupNodeElement(Controller controller)
        {
            GraphElement result = null;
            rootGroupNodeElements.TryGetValue(controller, out result);
            return result;
        }

        Dictionary<VFXGroupNodeController, VFXGroupNode> groupNodes = new Dictionary<VFXGroupNodeController, VFXGroupNode>();
        Dictionary<VFXStickyNoteController, VFXStickyNote> stickyNotes = new Dictionary<VFXStickyNoteController, VFXStickyNote>();

        void OnOneNodeGeometryChanged(GeometryChangedEvent e)
        {
            m_GeometrySet = true;
            (e.target as GraphElement).UnregisterCallback<GeometryChangedEvent>(OnOneNodeGeometryChanged);
        }

        void SyncNodes()
        {
            Profiler.BeginSample("VFXView.SyncNodes");
            if (controller == null)
            {
                foreach (var element in rootNodes.Values.ToArray())
                {
                    SafeRemoveElement(element);
                }
                rootNodes.Clear();
                rootGroupNodeElements.Clear();
            }
            else
            {
                elementsAddedToGroup = null;
                elementsRemovedFromGroup = null;

                Profiler.BeginSample("VFXView.SyncNodes:Delete");
                var deletedControllers = rootNodes.Keys.Except(controller.nodes).ToArray();

                foreach (var deletedController in deletedControllers)
                {
                    SafeRemoveElement(rootNodes[deletedController]);
                    rootNodes.Remove(deletedController);
                    rootGroupNodeElements.Remove(deletedController);
                }
                Profiler.EndSample();
                bool needOneListenToGeometry = !m_GeometrySet;

                Profiler.BeginSample("VFXView.SyncNodes:Create");
                foreach (var newController in controller.nodes.Except(rootNodes.Keys).ToArray())
                {
                    VFXNodeUI newElement = null;
                    if (newController is VFXContextController)
                    {
                        newElement = new VFXContextUI();
                    }
                    else if (newController is VFXOperatorController)
                    {
                        newElement = new VFXOperatorUI();
                    }
                    else if (newController is VFXParameterNodeController)
                    {
                        newElement = new VFXParameterUI();
                    }
                    else
                    {
                        throw new InvalidOperationException("Can't find right ui for controller" + newController.GetType().Name);
                    }
                    Profiler.BeginSample("VFXView.SyncNodes:AddElement");
                    FastAddElement(newElement);
                    Profiler.EndSample();
                    rootNodes[newController] = newElement;
                    rootGroupNodeElements[newController] = newElement;
                    (newElement as ISettableControlledElement<VFXNodeController>).controller = newController;
                    if (needOneListenToGeometry)
                    {
                        needOneListenToGeometry = false;
                        newElement.RegisterCallback<GeometryChangedEvent>(OnOneNodeGeometryChanged);
                    }
                }
                Profiler.EndSample();

                elementsAddedToGroup = ElementAddedToGroupNode;
                elementsRemovedFromGroup = ElementRemovedFromGroupNode;
            }

            Profiler.EndSample();
        }

        static FieldInfo s_Member_ContainerLayer = typeof(GraphView).GetField("m_ContainerLayers", BindingFlags.NonPublic | BindingFlags.Instance);
        static MethodInfo s_Method_GetLayer = typeof(GraphView).GetMethod("GetLayer", BindingFlags.NonPublic | BindingFlags.Instance);

        public void FastAddElement(GraphElement graphElement)
        {
            if (graphElement.IsResizable())
            {
                graphElement.shadow.Add(new Resizer());
                graphElement.style.borderBottomWidth = 6;
            }

            int newLayer = graphElement.layer;
            if (!(s_Member_ContainerLayer.GetValue(this) as IDictionary).Contains(newLayer))
            {
                AddLayer(newLayer);
            }
            (s_Method_GetLayer.Invoke(this, new object[] { newLayer }) as VisualElement).Add(graphElement);
        }

        bool m_UpdateUIBounds = false;
        void UpdateUIBounds()
        {
            if (!m_GeometrySet) return;
            if (m_InControllerChanged)
            {
                m_UpdateUIBounds = true;
                return;
            }
            m_UpdateUIBounds = false;

            if (panel != null)
            {
                panel.InternalValidateLayout();
                controller.graph.UIInfos.uiBounds = GetElementsBounds(rootGroupNodeElements.Values.Concat(groupNodes.Values.Cast<GraphElement>()));
            }
        }

        void SyncGroupNodes()
        {
            if (controller == null)
            {
                foreach (var kv in groupNodes)
                {
                    RemoveElement(kv.Value);
                }
                groupNodes.Clear();
            }
            else
            {
                var deletedControllers = groupNodes.Keys.Except(controller.groupNodes).ToArray();

                foreach (var deletedController in deletedControllers)
                {
                    RemoveElement(groupNodes[deletedController]);
                    groupNodes.Remove(deletedController);
                }

                foreach (var newController in controller.groupNodes.Except(groupNodes.Keys))
                {
                    var newElement = new VFXGroupNode();
                    FastAddElement(newElement);
                    newElement.controller = newController;
                    groupNodes.Add(newController, newElement);
                }
            }
        }

        void SyncStickyNotes()
        {
            if (controller == null)
            {
                foreach (var kv in stickyNotes)
                {
                    SafeRemoveElement(kv.Value);
                }
                rootGroupNodeElements.Clear();
                stickyNotes.Clear();
            }
            else
            {
                var deletedControllers = stickyNotes.Keys.Except(controller.stickyNotes).ToArray();

                foreach (var deletedController in deletedControllers)
                {
                    SafeRemoveElement(stickyNotes[deletedController]);
                    rootGroupNodeElements.Remove(deletedController);
                    stickyNotes.Remove(deletedController);
                }

                foreach (var newController in controller.stickyNotes.Except(stickyNotes.Keys))
                {
                    var newElement = new VFXStickyNote();
                    newElement.controller = newController;
                    FastAddElement(newElement);
                    rootGroupNodeElements[newController] = newElement;
                    stickyNotes[newController] = newElement;
                }
            }
        }

        public void SafeRemoveElement(GraphElement element)
        {
            VFXGroupNode.inRemoveElement = true;

            RemoveElement(element);

            VFXGroupNode.inRemoveElement = false;
        }

        Dictionary<VFXDataEdgeController, VFXDataEdge> dataEdges = new Dictionary<VFXDataEdgeController, VFXDataEdge>();
        Dictionary<VFXFlowEdgeController, VFXFlowEdge> flowEdges = new Dictionary<VFXFlowEdgeController, VFXFlowEdge>();

        void SyncEdges(int change)
        {
            if (change != VFXViewController.Change.flowEdge)
            {
                if (controller == null)
                {
                    foreach (var element in dataEdges.Values)
                    {
                        RemoveElement(element);
                    }
                    dataEdges.Clear();
                }
                else
                {
                    var deletedControllers = dataEdges.Keys.Except(controller.dataEdges).ToArray();

                    foreach (var deletedController in deletedControllers)
                    {
                        var edge = dataEdges[deletedController];
                        if (edge.input != null)
                        {
                            edge.input.Disconnect(edge);
                        }
                        if (edge.output != null)
                        {
                            edge.output.Disconnect(edge);
                        }
                        RemoveElement(edge);
                        dataEdges.Remove(deletedController);
                    }

                    foreach (var newController in controller.dataEdges.Except(dataEdges.Keys).ToArray())
                    {
                        // SyncEdges could be called before the VFXNodeUI have been created, it that case ignore them and trust that they will be created later when the
                        // nodes arrive.
                        if (GetNodeByController(newController.input.sourceNode) == null || GetNodeByController(newController.output.sourceNode) == null)
                        {
                            if (change != VFXViewController.Change.dataEdge)
                            {
                                Debug.LogError("Can't match nodes for a data edge after nodes should have been updated.");
                            }
                            continue;
                        }

                        var newElement = new VFXDataEdge();
                        FastAddElement(newElement);
                        newElement.controller = newController;

                        dataEdges.Add(newController, newElement);
                        if (newElement.input != null)
                            newElement.input.node.RefreshExpandedState();
                        if (newElement.output != null)
                            newElement.output.node.RefreshExpandedState();
                    }
                }
            }

            if (change != VFXViewController.Change.dataEdge)
            {
                if (controller == null)
                {
                    foreach (var element in flowEdges.Values)
                    {
                        RemoveElement(element);
                    }
                    flowEdges.Clear();
                }
                else
                {
                    var deletedControllers = flowEdges.Keys.Except(controller.flowEdges).ToArray();

                    foreach (var deletedController in deletedControllers)
                    {
                        var edge = flowEdges[deletedController];
                        if (edge.input != null)
                        {
                            edge.input.Disconnect(edge);
                        }
                        if (edge.output != null)
                        {
                            edge.output.Disconnect(edge);
                        }
                        RemoveElement(edge);
                        flowEdges.Remove(deletedController);
                    }

                    foreach (var newController in controller.flowEdges.Except(flowEdges.Keys))
                    {
                        var newElement = new VFXFlowEdge();
                        FastAddElement(newElement);
                        newElement.controller = newController;
                        flowEdges.Add(newController, newElement);
                    }
                }
            }
        }

        public Vector2 ScreenToViewPosition(Vector2 position)
        {
            GUIView guiView = panel.InternalGetGUIView();
            if (guiView == null)
                return position;
            return position - guiView.screenPosition.position;
        }

        public Vector2 ViewToScreenPosition(Vector2 position)
        {
            GUIView guiView = panel.InternalGetGUIView();
            if (guiView == null)
                return position;
            return position + guiView.screenPosition.position;
        }

        void OnCreateNode(NodeCreationContext ctx)
        {
            GUIView guiView = panel.InternalGetGUIView();
            if (guiView == null)
                return;
            Vector2 point = ScreenToViewPosition(ctx.screenMousePosition);

            List<VisualElement> picked = new List<VisualElement>();
            panel.PickAll(point, picked);

            VFXContextUI context = picked.OfType<VFXContextUI>().FirstOrDefault();

            if (context != null)
            {
                context.OnCreateBlock(point);
            }
            else
            {
                VFXFilterWindow.Show(VFXViewWindow.currentWindow, point, ctx.screenMousePosition, m_NodeProvider);
            }
        }

        public void CreateTemplateSystem(string path, Vector2 tPos, VFXGroupNode groupNode)
        {
            var resource = VisualEffectResource.GetResourceAtPath(path);
            if (resource != null)
            {
                VFXViewController templateController = VFXViewController.GetController(resource, true);
                templateController.useCount++;

#if OLD_COPY_PASTE
                var data = VFXCopyPaste.SerializeElements(templateController.allChildren, templateController.graph.UIInfos.uiBounds);
                VFXCopyPaste.UnserializeAndPasteElements(controller, tPos, data, this, groupNode != null ? groupNode.controller : null);
#else
                var data = VFXCopy.SerializeElements(templateController.allChildren, templateController.graph.UIInfos.uiBounds);
                VFXPaste.UnserializeAndPasteElements(controller, tPos, data, this, groupNode != null ? groupNode.controller : null);
#endif

                templateController.useCount--;
            }
        }

        void OnToggleCompile(ChangeEvent<bool> e)
        {
            VFXViewWindow.currentWindow.autoCompile = !VFXViewWindow.currentWindow.autoCompile;
        }

        void OnCompile()
        {
            var graph = controller.graph;
            graph.SetExpressionGraphDirty();
            graph.RecompileIfNeeded();
        }

        private bool m_IsRuntimeMode = false;
        void OnToggleRuntimeMode(ChangeEvent<bool> e)
        {
            m_IsRuntimeMode = e.newValue;
            controller.graph.SetCompilationMode(m_IsRuntimeMode ? VFXCompilationMode.Runtime : VFXCompilationMode.Edition);
        }

        public EventPropagation Compile()
        {
            OnCompile();

            return EventPropagation.Stop;
        }

        VFXContext AddVFXContext(Vector2 pos, VFXModelDescriptor<VFXContext> desc)
        {
            if (controller == null) return null;
            return controller.AddVFXContext(pos, desc);
        }

        VFXOperator AddVFXOperator(Vector2 pos, VFXModelDescriptor<VFXOperator> desc)
        {
            if (controller == null) return null;
            return controller.AddVFXOperator(pos, desc);
        }

        VFXParameter AddVFXParameter(Vector2 pos, VFXModelDescriptorParameters desc)
        {
            if (controller == null) return null;
            return controller.AddVFXParameter(pos, desc);
        }

        void AddVFXParameter(Vector2 pos, VFXParameterController parameterController, VFXGroupNode groupNode)
        {
            if (controller == null || parameterController == null) return;

            controller.AddVFXParameter(pos, parameterController, groupNode != null ? groupNode.controller : null);
        }

        public EventPropagation Resync()
        {
            if (controller != null)
                controller.ForceReload();
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDot()
        {
            if (controller == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(controller.graph, VFXExpressionContextOption.None);
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDotReduced()
        {
            if (controller == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(controller.graph, VFXExpressionContextOption.Reduction);
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDotConstantFolding()
        {
            if (controller == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(controller.graph, VFXExpressionContextOption.ConstantFolding);
            return EventPropagation.Stop;
        }

        public EventPropagation ReinitComponents()
        {
            if (attachedComponent != null)
            {
                attachedComponent.Reinit();
            }
            else
            {
                foreach (var component in UnityEngine.Experimental.VFX.VFXManager.GetComponents())
                    component.Reinit();
            }
            return EventPropagation.Stop;
        }

        public IEnumerable<VFXContextUI> GetAllContexts()
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXContextUI)
                    {
                        yield return element as VFXContextUI;
                    }
                }
            }
        }

        public IEnumerable<VFXNodeUI> GetAllNodes()
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXNodeUI)
                    {
                        yield return element as VFXNodeUI;
                    }
                }
            }
        }

        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            if (controller == null) return null;


            if (startAnchor is VFXDataAnchor)
            {
                var controllers = controller.GetCompatiblePorts((startAnchor as  VFXDataAnchor).controller, nodeAdapter);
                return controllers.Select(t => (Port)GetDataAnchorByController(t as VFXDataAnchorController)).ToList();
            }
            else
            {
                var controllers = controller.GetCompatiblePorts((startAnchor as VFXFlowAnchor).controller, nodeAdapter);
                return controllers.Select(t => (Port)GetFlowAnchorByController(t as VFXFlowAnchorController)).ToList();
            }
        }

        public IEnumerable<VFXFlowAnchor> GetAllFlowAnchors(bool input, bool output)
        {
            foreach (var context in GetAllContexts())
            {
                foreach (VFXFlowAnchor anchor in context.GetFlowAnchors(input, output))
                {
                    yield return anchor;
                }
            }
        }

        void VFXElementResized(VisualElement element)
        {
            if (element is IVFXResizable)
            {
                (element as IVFXResizable).OnResized();
            }
        }

        GraphViewChange VFXGraphViewChanged(GraphViewChange change)
        {
            if (change.movedElements != null && change.movedElements.Count > 0)
            {
                HashSet<IVFXMovable> movables = new HashSet<IVFXMovable>(change.movedElements.OfType<IVFXMovable>());
                foreach (var groupNode in groupNodes.Values)
                {
                    var containedElements = groupNode.containedElements;

                    if (containedElements != null && containedElements.Intersect(change.movedElements).Count() > 0)
                    {
                        groupNode.UpdateGeometryFromContent();
                        movables.Add(groupNode);
                    }
                }

                foreach (var groupNode in change.movedElements.OfType<VFXGroupNode>())
                {
                    var containedElements = groupNode.containedElements;
                    if (containedElements != null)
                    {
                        foreach (var node in containedElements.OfType<IVFXMovable>())
                        {
                            movables.Add(node);
                        }
                    }
                }

                foreach (var movable in movables)
                {
                    movable.OnMoved();
                }
            }
            else if (change.elementsToRemove != null)
            {
                controller.Remove(change.elementsToRemove.OfType<IControlledElement>().Where(t => t.controller != null).Select(t => t.controller));
            }

            foreach (var groupNode in groupNodes.Values)
            {
                groupNode.UpdateControllerFromContent();
            }

            return change;
        }

        VFXNodeUI GetNodeByController(VFXNodeController controller)
        {
            if (controller is VFXBlockController)
            {
                var blockController = (controller as VFXBlockController);
                VFXContextUI context = GetNodeByController(blockController.contextController) as VFXContextUI;

                return context.GetAllBlocks().FirstOrDefault(t => t.controller == blockController);
            }
            return GetAllNodes().FirstOrDefault(t => t.controller == controller);
        }

        public VFXDataAnchor GetDataAnchorByController(VFXDataAnchorController controller)
        {
            if (controller == null)
                return null;

            VFXNodeUI node = GetNodeByController(controller.sourceNode);
            if (node == null)
            {
                Debug.LogError("Can't find the node for a given node controller");
                return null;
            }

            VFXDataAnchor anchor = node.GetPorts(controller.direction == Direction.Input, controller.direction == Direction.Output).FirstOrDefault(t => t.controller == controller);
            if (anchor == null)
            {
                // Can happen because the order of the DataWatch is not controlled
                node.ForceUpdate();
                anchor = node.GetPorts(controller.direction == Direction.Input, controller.direction == Direction.Output).FirstOrDefault(t => t.controller == controller);
            }
            return anchor;
        }

        public VFXFlowAnchor GetFlowAnchorByController(VFXFlowAnchorController controller)
        {
            if (controller == null)
                return null;
            return GetAllFlowAnchors(controller.direction == Direction.Input, controller.direction == Direction.Output).Where(t => t.controller == controller).FirstOrDefault();
        }

        public IEnumerable<VFXDataAnchor> GetAllDataAnchors(bool input, bool output)
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXNodeUI)
                    {
                        var ope = element as VFXNodeUI;
                        foreach (VFXDataAnchor anchor in ope.GetPorts(input, output))
                            yield return anchor;
                        if (element is VFXContextUI)
                        {
                            var context = element as VFXContextUI;

                            foreach (VFXBlockUI block in context.GetAllBlocks())
                            {
                                foreach (VFXDataAnchor anchor in block.GetPorts(input, output))
                                    yield return anchor;
                            }
                        }
                    }
                }
            }
        }

        public VFXDataEdge GetDataEdgeByController(VFXDataEdgeController controller)
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXDataEdge)
                    {
                        VFXDataEdge candidate = element as VFXDataEdge;
                        if (candidate.controller == controller)
                            return candidate;
                    }
                }
            }
            return null;
        }

        public IEnumerable<VFXDataEdge> GetAllDataEdges()
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer)
                {
                    if (element is VFXDataEdge)
                    {
                        yield return element as VFXDataEdge;
                    }
                }
            }
        }

        public IEnumerable<Port> GetAllPorts(bool input, bool output)
        {
            foreach (var anchor in GetAllDataAnchors(input, output))
            {
                yield return anchor;
            }
            foreach (var anchor in GetAllFlowAnchors(input, output))
            {
                yield return anchor;
            }
        }

        public void UpdateGlobalSelection()
        {
            if (controller == null) return;

            var objectSelected = selection.OfType<VFXNodeUI>().Select(t => t.controller.model).Concat(selection.OfType<VFXContextUI>().Select(t => t.controller.model).Cast<VFXModel>()).Where(t => t != null).ToArray();

            if (objectSelected.Length > 0)
            {
                Selection.objects = objectSelected;
                Selection.objects = objectSelected;
                return;
            }

            var blackBoardSelected = selection.OfType<BlackboardField>().Select(t => t.GetFirstAncestorOfType<VFXBlackboardRow>().controller.model).ToArray();

            if (blackBoardSelected.Length > 0)
            {
                Selection.objects = blackBoardSelected;
                return;
            }
        }

        void SelectAsset()
        {
            if (Selection.activeObject != controller.model)
            {
                Selection.activeObject = controller.model.asset;
                EditorGUIUtility.PingObject(controller.model.asset);
            }
        }

        void ElementAddedToGroupNode(Group groupNode, IEnumerable<GraphElement> elements)
        {
            (groupNode as VFXGroupNode).ElementsAddedToGroupNode(elements);
        }

        void ElementRemovedFromGroupNode(Group groupNode, IEnumerable<GraphElement> elements)
        {
            (groupNode as VFXGroupNode).ElementsRemovedFromGroupNode(elements);
        }

        void GroupNodeTitleChanged(Group groupNode, string title)
        {
            (groupNode as VFXGroupNode).GroupNodeTitleChanged(title);
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            UpdateGlobalSelection();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            UpdateGlobalSelection();
        }

        public override void ClearSelection()
        {
            bool selectionEmpty = selection.Count() == 0;
            base.ClearSelection();
            if (!selectionEmpty)
                UpdateGlobalSelection();
        }

        VFXBlackboard m_Blackboard;


        VFXComponentBoard m_ComponentBoard;

        public readonly Vector2 defaultPasteOffset = new Vector2(100, 100);
        public Vector2 pasteOffset = Vector2.zero;

        public VFXBlackboard blackboard
        {
            get { return m_Blackboard; }
        }


        protected internal override bool canCopySelection
        {
            get { return selection.OfType<VFXNodeUI>().Any() || selection.OfType<Group>().Any() || selection.OfType<VFXContextUI>().Any() || selection.OfType<VFXStickyNote>().Any(); }
        }

        IEnumerable<Controller> ElementsToController(IEnumerable<GraphElement> elements)
        {
            return elements.OfType<IControlledElement>().Select(t => t.controller);
        }

        void CollectElements(IEnumerable<GraphElement> elements, HashSet<GraphElement> elementsToCopySet)
        {
            foreach (var element in elements)
            {
                if (element is Group)
                {
                    CollectElements((element as Group).containedElements, elementsToCopySet);
                    elementsToCopySet.Add(element);
                }
                else if (element is Node || element is VFXContextUI || element is VFXStickyNote)
                {
                    elementsToCopySet.Add(element);
                }
            }
        }

        protected internal override void CollectCopyableGraphElements(IEnumerable<GraphElement> elements, HashSet<GraphElement> elementsToCopySet)
        {
            CollectElements(elements, elementsToCopySet);

            var nodeuis = new HashSet<VFXNodeUI>(elementsToCopySet.SelectMany(t => t.Query().OfType<VFXNodeUI>().ToList()));
            var contextuis = new HashSet<VFXContextUI>(elementsToCopySet.OfType<VFXContextUI>());

            foreach (var edge in edges.ToList())
            {
                if (edge is VFXDataEdge)
                {
                    if (nodeuis.Contains(edge.input.GetFirstAncestorOfType<VFXNodeUI>()) && nodeuis.Contains(edge.output.GetFirstAncestorOfType<VFXNodeUI>()))
                    {
                        elementsToCopySet.Add(edge);
                    }
                }
                else
                {
                    if (contextuis.Contains(edge.input.GetFirstAncestorOfType<VFXContextUI>()) && contextuis.Contains(edge.output.GetFirstAncestorOfType<VFXContextUI>()))
                    {
                        elementsToCopySet.Add(edge);
                    }
                }
            }
        }

        Rect GetElementsBounds(IEnumerable<GraphElement> elements)
        {
            Rect[] elementBounds = elements.Where(t => !(t is VFXEdge)).Select(t => contentViewContainer.WorldToLocal(t.worldBound)).ToArray();
            if (elementBounds.Length < 1) return Rect.zero;

            Rect bounds = elementBounds[0];

            for (int i = 1; i < elementBounds.Length; ++i)
            {
                bounds = Rect.MinMaxRect(Mathf.Min(elementBounds[i].xMin, bounds.xMin), Mathf.Min(elementBounds[i].yMin, bounds.yMin), Mathf.Max(elementBounds[i].xMax, bounds.xMax), Mathf.Max(elementBounds[i].yMax, bounds.yMax));
            }

            // Round to avoid changes in the asset because of the zoom level.
            bounds.x = Mathf.Round(bounds.x);
            bounds.y = Mathf.Round(bounds.y);
            bounds.width = Mathf.Round(bounds.width);
            bounds.height = Mathf.Round(bounds.height);

            return bounds;
        }

        string SerializeElements(IEnumerable<GraphElement> elements)
        {
            pasteOffset = defaultPasteOffset;
#if OLD_COPY_PASTE
            return VFXCopyPaste.SerializeElements(ElementsToController(elements), GetElementsBounds(elements));
#else
            Profiler.BeginSample("VFXCopy.SerializeElements");
            string result = VFXCopy.SerializeElements(ElementsToController(elements), GetElementsBounds(elements));
            Profiler.EndSample();
            return result;
#endif
        }

        Vector2 pasteCenter
        {
            get
            {
                Vector2 center = layout.size * 0.5f;

                center = this.ChangeCoordinatesTo(contentViewContainer, center);

                return center;
            }
        }

        void UnserializeAndPasteElements(string operationName, string data)
        {
#if OLD_COPY_PASTE
            VFXCopyPaste.UnserializeAndPasteElements(controller, pasteCenter, data, this);
#else
            Profiler.BeginSample("VFXPaste.VFXPaste.UnserializeAndPasteElements");
            VFXPaste.UnserializeAndPasteElements(controller, pasteCenter, data, this);
            Profiler.EndSample();
#endif

            pasteOffset += defaultPasteOffset;
        }

        const float k_MarginBetweenContexts = 30;
        public void PushUnderContext(VFXContextUI context, float size)
        {
            if (size < 5) return;

            HashSet<VFXContextUI> contexts = new HashSet<VFXContextUI>();

            contexts.Add(context);

            var flowEdges = edges.ToList().OfType<VFXFlowEdge>().ToList();

            int contextCount = 0;

            while (contextCount < contexts.Count())
            {
                contextCount = contexts.Count();
                foreach (var flowEdge in flowEdges)
                {
                    VFXContextUI topContext = flowEdge.output.GetFirstAncestorOfType<VFXContextUI>();
                    VFXContextUI bottomContext = flowEdge.input.GetFirstAncestorOfType<VFXContextUI>();
                    if (contexts.Contains(topContext)  && !contexts.Contains(bottomContext))
                    {
                        float topContextBottom = topContext.layout.yMax;
                        float newTopContextBottom = topContext.layout.yMax + size;
                        if (topContext == context)
                        {
                            newTopContextBottom -= size;
                            topContextBottom -= size;
                        }
                        float bottomContextTop = bottomContext.layout.yMin;

                        if (topContextBottom < bottomContextTop && newTopContextBottom + k_MarginBetweenContexts > bottomContextTop)
                        {
                            contexts.Add(bottomContext);
                        }
                    }
                }
            }

            contexts.Remove(context);

            foreach (var c in contexts)
            {
                c.controller.position = c.GetPosition().min + new Vector2(0, size);
            }
        }

        bool canGroupSelection
        {
            get
            {
                return canCopySelection && !selection.Any(t => t is Group);
            }
        }

        public void ValidateCommand(ValidateCommandEvent evt)
        {
            if (evt.commandName == "SelectAll")
            {
                evt.StopPropagation();
                if (evt.imguiEvent != null)
                {
                    evt.imguiEvent.Use();
                }
            }
        }

        public void ExecuteCommand(ExecuteCommandEvent e)
        {
            if (e.commandName == "SelectAll")
            {
                ClearSelection();

                foreach (var element in graphElements.ToList())
                {
                    AddToSelection(element);
                }
                e.StopPropagation();
            }
        }

        void GroupSelection()
        {
            controller.GroupNodes(selection.OfType<ISettableControlledElement<VFXNodeController>>().Select(t => t.controller));
        }

        void AddStickyNote(Vector2 position, VFXGroupNode group = null)
        {
            position = contentViewContainer.WorldToLocal(position);
            controller.AddStickyNote(position, group != null ? group.controller : null);
        }

        void OnCreateNodeInGroupNode(DropdownMenu.MenuAction e)
        {
            //The targeted groupnode will be determined by a PickAll later
            VFXFilterWindow.Show(VFXViewWindow.currentWindow, e.eventInfo.mousePosition, ViewToScreenPosition(e.eventInfo.mousePosition), m_NodeProvider);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            var targetSystem = evt.target as VFXSystemBorder;
            if( evt.target is VFXGroupNode || evt.target is VFXSystemBorder) // Default behaviour only shows the OnCreateNode if the target is the view itself.
                evt.target = this;
            
            base.BuildContextualMenu(evt);

            Vector2 mousePosition = evt.mousePosition;
            bool hasMenu = false;

            if (evt.target is VFXNodeUI)
            {
                evt.menu.InsertAction(evt.target is VFXContextUI ? 1 : 0, "Group Selection", (e) => { GroupSelection(); },
                    (e) => { return canGroupSelection ? DropdownMenu.MenuAction.StatusFlags.Normal : DropdownMenu.MenuAction.StatusFlags.Disabled; });
                hasMenu = true;
            }

            if (evt.target is VFXView )
            {
                evt.menu.InsertAction(1,"Create Sticky Note", (e) => { AddStickyNote(mousePosition); },
                    (e) => { return DropdownMenu.MenuAction.StatusFlags.Normal; });
                hasMenu = true;
            }
            if (targetSystem != null)
            {
                if (hasMenu)
                {
                    evt.menu.AppendSeparator();
                }
                evt.menu.InsertAction(2,string.IsNullOrEmpty(targetSystem.controller.title) ? "Name System" : "Rename System", a => targetSystem.OnRename(), e => DropdownMenu.MenuAction.StatusFlags.Normal);
            }

            if (evt.target is VFXContextUI)
            {
                var context = evt.target as VFXContextUI;
                evt.menu.InsertAction(2,string.IsNullOrEmpty(context.controller.model.label) ? "Name Context" : "Rename Context", a => context.OnRename(), e => DropdownMenu.MenuAction.StatusFlags.Normal);
            }
        }


        List<VFXSystemBorder> m_Systems = new List<VFXSystemBorder>();

        public void UpdateSystems()
        {
            while (m_Systems.Count() > controller.systems.Count())
            {
                VFXSystemBorder border = m_Systems.Last();
                m_Systems.RemoveAt(m_Systems.Count - 1);
                border.RemoveFromHierarchy();
            }

            foreach(var system in m_Systems)
            {
                system.Update();
            }

            while (m_Systems.Count() < controller.systems.Count())
            {
                VFXSystemBorder border = new VFXSystemBorder();
                m_Systems.Add(border);
                AddElement(border);
                border.controller = controller.systems[m_Systems.Count() - 1];
            }
        }
        

        bool IDropTarget.CanAcceptDrop(List<ISelectable> selection)
        {
            return selection.Any(t => t is BlackboardField && (t as BlackboardField).GetFirstAncestorOfType<VFXBlackboardRow>() != null);
        }

        bool IDropTarget.DragExited()
        {
            return true;
        }

        bool IDropTarget.DragEnter(DragEnterEvent evt, IEnumerable<ISelectable> selection, IDropTarget enteredTarget, ISelection dragSource)
        {
            return true;
        }

        bool IDropTarget.DragLeave(DragLeaveEvent evt, IEnumerable<ISelectable> selection, IDropTarget leftTarget, ISelection dragSource)
        {
            return true;
        }

        bool IDropTarget.DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            var rows = selection.OfType<BlackboardField>().Select(t => t.GetFirstAncestorOfType<VFXBlackboardRow>()).Where(t => t != null).ToArray();

            Vector2 mousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);


            foreach (var row in rows)
            {
                AddVFXParameter(mousePosition - new Vector2(100, 75), row.controller, null);
            }

            return true;
        }

        bool IDropTarget.DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;

            return true;
        }

        void OnDragUpdated(DragUpdatedEvent e)
        {
            if (selection.Any(t => t is BlackboardField && (t as BlackboardField).GetFirstAncestorOfType<VFXBlackboardRow>() != null))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                e.StopPropagation();
            }
        }

        void OnDragPerform(DragPerformEvent e)
        {
            var groupNode = GetPickedGroupNode(e.mousePosition);

            var rows = selection.OfType<BlackboardField>().Select(t => t.GetFirstAncestorOfType<VFXBlackboardRow>()).Where(t => t != null).ToArray();
            if (rows.Length > 0)
            {
                Vector2 mousePosition = contentViewContainer.WorldToLocal(e.mousePosition);
                foreach (var row in rows)
                {
                    AddVFXParameter(mousePosition - new Vector2(50, 20), row.controller, groupNode);
                }
            }
        }
    }
}
