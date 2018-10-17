using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Rendering;
using Node = UnityEditor.Experimental.UIElements.GraphView.Node;
#if UNITY_2018_3_OR_NEWER
using ContextualMenu = UnityEngine.Experimental.UIElements.DropdownMenu;
#endif

namespace UnityEditor.ShaderGraph.Drawing
{
    public sealed class MaterialNodeView : Node
    {
        PreviewRenderData m_PreviewRenderData;
        Image m_PreviewImage;
        VisualElement m_PreviewContainer;
        VisualElement m_ControlItems;
        VisualElement m_PreviewFiller;
        VisualElement m_ControlsDivider;
        IEdgeConnectorListener m_ConnectorListener;
        VisualElement m_PortInputContainer;
        VisualElement m_SettingsContainer;
        bool m_ShowSettings = false;
        VisualElement m_SettingsButton;
        VisualElement m_Settings;
        VisualElement m_NodeSettingsView;


        public void Initialize(AbstractMaterialNode inNode, PreviewManager previewManager, IEdgeConnectorListener connectorListener)
        {
            AddStyleSheetPath("Styles/MaterialNodeView");
            AddToClassList("MaterialNode");

            if (inNode == null)
                return;

            var contents = this.Q("contents");

            m_ConnectorListener = connectorListener;
            node = inNode;
            persistenceKey = node.guid.ToString();
            UpdateTitle();

            // Add controls container
            var controlsContainer = new VisualElement { name = "controls" };
            {
                m_ControlsDivider = new VisualElement { name = "divider" };
                m_ControlsDivider.AddToClassList("horizontal");
                controlsContainer.Add(m_ControlsDivider);
                m_ControlItems = new VisualElement { name = "items" };
                controlsContainer.Add(m_ControlItems);

                // Instantiate control views from node
                foreach (var propertyInfo in node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    foreach (IControlAttribute attribute in propertyInfo.GetCustomAttributes(typeof(IControlAttribute), false))
                        m_ControlItems.Add(attribute.InstantiateControl(node, propertyInfo));
            }
            if (m_ControlItems.childCount > 0)
                contents.Add(controlsContainer);

            if (node.hasPreview)
            {
                // Add actual preview which floats on top of the node
                m_PreviewContainer = new VisualElement
                {
                    name = "previewContainer",
                    clippingOptions = ClippingOptions.ClipAndCacheContents,
                    pickingMode = PickingMode.Ignore
                };
                m_PreviewImage = new Image
                {
                    name = "preview",
                    pickingMode = PickingMode.Ignore,
                    image = Texture2D.whiteTexture,
                };
                {
                    // Add preview collapse button on top of preview
                    var collapsePreviewButton = new VisualElement { name = "collapse" };
                    collapsePreviewButton.Add(new VisualElement { name = "icon" });
                    collapsePreviewButton.AddManipulator(new Clickable(() =>
                        {
                            node.owner.owner.RegisterCompleteObjectUndo("Collapse Preview");
                            UpdatePreviewExpandedState(false);
                        }));
                    m_PreviewImage.Add(collapsePreviewButton);
                }
                m_PreviewContainer.Add(m_PreviewImage);

                // Hook up preview image to preview manager
                m_PreviewRenderData = previewManager.GetPreview(inNode);
                m_PreviewRenderData.onPreviewChanged += UpdatePreviewTexture;
                UpdatePreviewTexture();

                // Add fake preview which pads out the node to provide space for the floating preview
                m_PreviewFiller = new VisualElement { name = "previewFiller" };
                m_PreviewFiller.AddToClassList("expanded");
                {
                    var previewDivider = new VisualElement { name = "divider" };
                    previewDivider.AddToClassList("horizontal");
                    m_PreviewFiller.Add(previewDivider);

                    var expandPreviewButton = new VisualElement { name = "expand" };
                    expandPreviewButton.Add(new VisualElement { name = "icon" });
                    expandPreviewButton.AddManipulator(new Clickable(() =>
                        {
                            node.owner.owner.RegisterCompleteObjectUndo("Expand Preview");
                            UpdatePreviewExpandedState(true);
                        }));
                    m_PreviewFiller.Add(expandPreviewButton);
                }
                contents.Add(m_PreviewFiller);

                UpdatePreviewExpandedState(node.previewExpanded);
            }

            // Add port input container, which acts as a pixel cache for all port inputs
            m_PortInputContainer = new VisualElement
            {
                name = "portInputContainer",
                clippingOptions = ClippingOptions.ClipAndCacheContents,
                pickingMode = PickingMode.Ignore
            };
            Add(m_PortInputContainer);

            AddSlots(node.GetSlots<MaterialSlot>());
            UpdatePortInputs();
            base.expanded = node.drawState.expanded;
            RefreshExpandedState(); //This should not be needed. GraphView needs to improve the extension api here
            UpdatePortInputVisibilities();

            SetPosition(new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0));

            if (node is SubGraphNode)
            {
                RegisterCallback<MouseDownEvent>(OnSubGraphDoubleClick);
            }

            var masterNode = node as IMasterNode;
            if (masterNode != null)
            {
                if (!masterNode.IsPipelineCompatible(GraphicsSettings.renderPipelineAsset))
                {
                    IconBadge wrongPipeline = IconBadge.CreateError("The current render pipeline is not compatible with this master node.");
                    Add(wrongPipeline);
                    VisualElement title = this.Q("title");
                    wrongPipeline.AttachTo(title, SpriteAlignment.LeftCenter);
                }
            }

            m_PortInputContainer.SendToBack();

            // Remove this after updated to the correct API call has landed in trunk. ------------
            VisualElement m_TitleContainer;
            VisualElement m_ButtonContainer;
            m_TitleContainer = this.Q("title");
            // -----------------------------------------------------------------------------------

            var settings = node as IHasSettings;
            if (settings != null)
            {
                m_NodeSettingsView = new NodeSettingsView();
                m_NodeSettingsView.visible = false;

                Add(m_NodeSettingsView);

                m_SettingsButton = new VisualElement {name = "settings-button"};
                m_SettingsButton.Add(new VisualElement { name = "icon" });

                m_Settings = settings.CreateSettingsElement();

                m_SettingsButton.AddManipulator(new Clickable(() =>
                    {
                        UpdateSettingsExpandedState();
                    }));

                // Remove this after updated to the correct API call has landed in trunk. ------------
                m_ButtonContainer = new VisualElement { name = "button-container" };
                m_ButtonContainer.style.flexDirection = StyleValue<FlexDirection>.Create(FlexDirection.Row);
                m_ButtonContainer.Add(m_SettingsButton);
                m_ButtonContainer.Add(m_CollapseButton);
                m_TitleContainer.Add(m_ButtonContainer);
                // -----------------------------------------------------------------------------------
                //titleButtonContainer.Add(m_SettingsButton);
                //titleButtonContainer.Add(m_CollapseButton);

                RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // style.positionTop and style.positionLeft are in relation to the parent,
            // so we translate the layout of the settings button to be in the coordinate
            // space of the settings view's parent.

            var settingsButtonLayout = m_SettingsButton.ChangeCoordinatesTo(m_NodeSettingsView.parent, m_SettingsButton.layout);
            m_NodeSettingsView.style.positionTop = settingsButtonLayout.yMax - 18f;
            m_NodeSettingsView.style.positionLeft = settingsButtonLayout.xMin - 16f;
        }

        void OnSubGraphDoubleClick(MouseDownEvent evt)
        {
            if (evt.clickCount == 2 && evt.button == 0)
            {
                SubGraphNode subgraphNode = node as SubGraphNode;

                var path = AssetDatabase.GetAssetPath(subgraphNode.subGraphAsset);
                ShaderGraphImporterEditor.ShowGraphEditWindow(path);
            }
        }

        public AbstractMaterialNode node { get; private set; }

        public override bool expanded
        {
            get { return base.expanded; }
            set
            {
                if (base.expanded != value)
                    base.expanded = value;

                if (node.drawState.expanded != value)
                {
                    var ds = node.drawState;
                    ds.expanded = value;
                    node.drawState = ds;
                }

                RefreshExpandedState(); //This should not be needed. GraphView needs to improve the extension api here
                UpdatePortInputVisibilities();
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is Node)
            {
                evt.menu.AppendAction("Copy Shader", CopyToClipboard, node.hasPreview ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Hidden);
                evt.menu.AppendAction("Show Generated Code", ShowGeneratedCode, node.hasPreview ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Hidden);
            }

            base.BuildContextualMenu(evt);
        }

        void CopyToClipboard()
        {
            GUIUtility.systemCopyBuffer = ConvertToShader();
        }

        public string SanitizeName(string name)
        {
            return new string(name.Where(c => !Char.IsWhiteSpace(c)).ToArray());
        }

        public void ShowGeneratedCode()
        {
            string name = GetFirstAncestorOfType<GraphEditorView>().assetName;

            string path = String.Format("Temp/GeneratedFromGraph-{0}-{1}-{2}.shader", SanitizeName(name), SanitizeName(node.name), node.guid);
            if (GraphUtil.WriteToFile(path, ConvertToShader()))
                GraphUtil.OpenFile(path);
        }

        string ConvertToShader()
        {
            List<PropertyCollector.TextureInfo> textureInfo;
            var masterNode = node as IMasterNode;
            if (masterNode != null)
                return masterNode.GetShader(GenerationMode.ForReals, node.name, out textureInfo);

            var graph = (AbstractMaterialGraph)node.owner;
            return graph.GetShader(node, GenerationMode.ForReals, node.name).shader;
        }

        void RecreateSettings()
        {
            var settings = node as IHasSettings;
            if (settings != null)
            {
                m_Settings.RemoveFromHierarchy();

                m_Settings = settings.CreateSettingsElement();
                m_NodeSettingsView.Add(m_Settings);
            }
        }

        void UpdateSettingsExpandedState()
        {
            m_ShowSettings = !m_ShowSettings;
            if (m_ShowSettings)
            {
                m_NodeSettingsView.Add(m_Settings);
                m_NodeSettingsView.visible = true;

                m_SettingsButton.AddToClassList("clicked");
            }
            else
            {
                m_Settings.RemoveFromHierarchy();

                m_NodeSettingsView.visible = false;
                m_SettingsButton.RemoveFromClassList("clicked");
            }
        }

        void UpdatePreviewExpandedState(bool expanded)
        {
            node.previewExpanded = expanded;
            if (m_PreviewFiller == null)
                return;
            if (expanded)
            {
                if (m_PreviewContainer.parent != this)
                {
                    Add(m_PreviewContainer);
                    m_PreviewContainer.PlaceBehind(this.Q("selection-border"));
                }
                m_PreviewFiller.AddToClassList("expanded");
                m_PreviewFiller.RemoveFromClassList("collapsed");
            }
            else
            {
                if (m_PreviewContainer.parent == m_PreviewFiller)
                {
                    m_PreviewContainer.RemoveFromHierarchy();
                }
                m_PreviewFiller.RemoveFromClassList("expanded");
                m_PreviewFiller.AddToClassList("collapsed");
            }
        }

        void UpdateTitle()
        {
            var subGraphNode = node as SubGraphNode;
            if (subGraphNode != null && subGraphNode.subGraphAsset != null)
                title = subGraphNode.subGraphAsset.name + " (sub)";
            else
                title = node.name;
        }

        public void OnModified(ModificationScope scope)
        {
            UpdateTitle();
            if (node.hasPreview)
                UpdatePreviewExpandedState(node.previewExpanded);

            base.expanded = node.drawState.expanded;

            // Update slots to match node modification
            if (scope == ModificationScope.Topological)
            {
                RecreateSettings();

                var slots = node.GetSlots<MaterialSlot>().ToList();

                var inputPorts = inputContainer.Children().OfType<ShaderPort>().ToList();
                foreach (var port in inputPorts)
                {
                    var currentSlot = port.slot;
                    var newSlot = slots.FirstOrDefault(s => s.id == currentSlot.id);
                    if (newSlot == null)
                    {
                        // Slot doesn't exist anymore, remove it
                        inputContainer.Remove(port);

                        // We also need to remove the inline input
                        var portInputView = m_PortInputContainer.OfType<PortInputView>().FirstOrDefault(v => Equals(v.slot, port.slot));
                        if (portInputView != null)
                            portInputView.RemoveFromHierarchy();
                    }
                    else
                    {
                        port.slot = newSlot;
                        var portInputView = m_PortInputContainer.OfType<PortInputView>().FirstOrDefault(x => x.slot.id == currentSlot.id);
                        portInputView.UpdateSlot(newSlot);

                        slots.Remove(newSlot);
                    }
                }

                var outputPorts = outputContainer.Children().OfType<ShaderPort>().ToList();
                foreach (var port in outputPorts)
                {
                    var currentSlot = port.slot;
                    var newSlot = slots.FirstOrDefault(s => s.id == currentSlot.id);
                    if (newSlot == null)
                    {
                        outputContainer.Remove(port);
                    }
                    else
                    {
                        port.slot = newSlot;
                        slots.Remove(newSlot);
                    }
                }

                AddSlots(slots);

                slots.Clear();
                slots.AddRange(node.GetSlots<MaterialSlot>());

                if (inputContainer.childCount > 0)
                    inputContainer.Sort((x, y) => slots.IndexOf(((ShaderPort)x).slot) - slots.IndexOf(((ShaderPort)y).slot));
                if (outputContainer.childCount > 0)
                    outputContainer.Sort((x, y) => slots.IndexOf(((ShaderPort)x).slot) - slots.IndexOf(((ShaderPort)y).slot));
            }

            RefreshExpandedState(); //This should not be needed. GraphView needs to improve the extension api here
            UpdatePortInputs();
            UpdatePortInputVisibilities();

            foreach (var control in m_ControlItems)
            {
                var listener = control as INodeModificationListener;
                if (listener != null)
                    listener.OnNodeModified(scope);
            }
        }

        void AddSlots(IEnumerable<MaterialSlot> slots)
        {
            foreach (var slot in slots)
            {
                if (slot.hidden)
                    continue;

                var port = ShaderPort.Create(slot, m_ConnectorListener);
                if (slot.isOutputSlot)
                    outputContainer.Add(port);
                else
                    inputContainer.Add(port);
            }
        }

        void UpdatePortInputs()
        {
            foreach (var port in inputContainer.OfType<ShaderPort>())
            {
                if (!m_PortInputContainer.OfType<PortInputView>().Any(a => Equals(a.slot, port.slot)))
                {
                    var portInputView = new PortInputView(port.slot) { style = { positionType = PositionType.Absolute } };
                    m_PortInputContainer.Add(portInputView);
                    port.RegisterCallback<GeometryChangedEvent>(evt => UpdatePortInput((ShaderPort)evt.target));
                }
            }
        }

        void UpdatePortInput(ShaderPort port)
        {
            var inputView = m_PortInputContainer.OfType<PortInputView>().First(x => Equals(x.slot, port.slot));

            var currentRect = new Rect(inputView.style.positionLeft, inputView.style.positionTop, inputView.style.width, inputView.style.height);
            var targetRect = new Rect(0.0f, 0.0f, port.layout.width, port.layout.height);
            targetRect = port.ChangeCoordinatesTo(inputView.shadow.parent, targetRect);
            var centerY = targetRect.center.y;
            var centerX = targetRect.xMax - currentRect.width;
            currentRect.center = new Vector2(centerX, centerY);

            inputView.style.positionTop = currentRect.yMin;
            var newHeight = inputView.parent.layout.height;
            foreach (var element in inputView.parent.Children())
                newHeight = Mathf.Max(newHeight, element.style.positionTop + element.layout.height);
            if (Math.Abs(inputView.parent.style.height - newHeight) > 1e-3)
                inputView.parent.style.height = newHeight;
        }

        public void UpdatePortInputVisibilities()
        {
            foreach (var portInputView in m_PortInputContainer.OfType<PortInputView>())
            {
                var slot = portInputView.slot;
                var oldVisibility = portInputView.visible;
                portInputView.visible = expanded && !node.owner.GetEdges(node.GetSlotReference(slot.id)).Any();
                if (portInputView.visible != oldVisibility)
                    m_PortInputContainer.MarkDirtyRepaint();
            }
        }

        public void UpdatePortInputTypes()
        {
            foreach (var anchor in inputContainer.Concat(outputContainer).OfType<ShaderPort>())
            {
                var slot = anchor.slot;
                anchor.portName = slot.displayName;
                anchor.visualClass = slot.concreteValueType.ToClassName();
            }

            foreach (var portInputView in m_PortInputContainer.OfType<PortInputView>())
                portInputView.UpdateSlotType();

            foreach (var control in m_ControlItems)
            {
                var listener = control as INodeModificationListener;
                if (listener != null)
                    listener.OnNodeModified(ModificationScope.Graph);
            }
        }

        void OnResize(Vector2 deltaSize)
        {
            var updatedWidth = topContainer.layout.width + deltaSize.x;
            var updatedHeight = m_PreviewImage.layout.height + deltaSize.y;

            var previewNode = node as PreviewNode;
            if (previewNode != null)
            {
                previewNode.SetDimensions(updatedWidth, updatedHeight);
                UpdateSize();
            }
        }

        void UpdatePreviewTexture()
        {
            if (m_PreviewRenderData.texture == null || !node.previewExpanded)
            {
                m_PreviewImage.visible = false;
                m_PreviewImage.image = Texture2D.blackTexture;
            }
            else
            {
                m_PreviewImage.visible = true;
                m_PreviewImage.AddToClassList("visible");
                m_PreviewImage.RemoveFromClassList("hidden");
                if (m_PreviewImage.image != m_PreviewRenderData.texture)
                    m_PreviewImage.image = m_PreviewRenderData.texture;
                else
                    m_PreviewImage.MarkDirtyRepaint();
            }
        }

        void UpdateSize()
        {
            var previewNode = node as PreviewNode;

            if (previewNode == null)
                return;

            var width = previewNode.width;
            var height = previewNode.height;

            m_PreviewImage.style.height = height;
            m_PreviewImage.style.width = width;
        }

        public void Dispose()
        {
            foreach (var portInputView in m_PortInputContainer.OfType<PortInputView>())
                portInputView.Dispose();

            node = null;
            if (m_PreviewRenderData != null)
            {
                m_PreviewRenderData.onPreviewChanged -= UpdatePreviewTexture;
                m_PreviewRenderData = null;
            }
        }
    }
}
