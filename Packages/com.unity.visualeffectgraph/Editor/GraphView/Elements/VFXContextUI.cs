using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Profiling;
using System.Reflection;

namespace UnityEditor.VFX.UI
{
    class VFXContextUI : VFXNodeUI, IDropTarget
    {
        // TODO: Unused except for debugging
        const string RectColorProperty = "rect-color";

        Image m_HeaderIcon;
        Image m_HeaderSpace;

        VisualElement m_Footer;
        Image m_FooterIcon;
        Label m_FooterTitle;

        VisualElement m_FlowInputConnectorContainer;
        VisualElement m_FlowOutputConnectorContainer;
        VisualElement m_BlockContainer;
        VisualElement m_NoBlock;

        VisualElement m_DragDisplay;

        Label m_Label;
        TextField m_TextField;

        public new VFXContextController controller
        {
            get { return base.controller as VFXContextController; }
        }
        protected override void OnNewController()
        {
            var blocks = new List<VFXModelDescriptor<VFXBlock>>(VFXLibrary.GetBlocks());

            m_CanHaveBlocks = blocks.Any(t => controller.model.AcceptChild(t.model));
        }

        public static string ContextEnumToClassName(string name)
        {
            if (name[0] != 'k')
            {
                Debug.LogError("Fix this since k has been removed from enums");
            }

            return name.Substring(1).ToLower();
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            Profiler.BeginSample("VFXContextUI.CreateBlockProvider");
            if (m_BlockProvider == null)
            {
                m_BlockProvider = new VFXBlockProvider(controller, (d, mPos) =>
                {
                    AddBlock(mPos, d);
                });
            }
            Profiler.EndSample();

            if (inputContainer.childCount == 0 && !hasSettings)
            {
                mainContainer.AddToClassList("empty");
            }
            else
            {
                mainContainer.RemoveFromClassList("empty");
            }

            m_HeaderIcon.image = GetIconForVFXType(controller.model.inputType);
            m_HeaderIcon.visible = m_HeaderIcon.image.value != null;


            Profiler.BeginSample("VFXContextUI.SetAllStyleClasses");

            VFXContextType contextType = controller.model.contextType;
            foreach (VFXContextType value in System.Enum.GetValues(typeof(VFXContextType)))
            {
                if (value != contextType)
                    RemoveFromClassList(ContextEnumToClassName(value.ToString()));
            }
            AddToClassList(ContextEnumToClassName(contextType.ToString()));

            var inputType = controller.model.inputType;
            if (inputType == VFXDataType.kNone)
            {
                inputType = controller.model.ownedType;
            }
            foreach (VFXDataType value in System.Enum.GetValues(typeof(VFXDataType)))
            {
                if (inputType != value)
                    RemoveFromClassList("inputType" + ContextEnumToClassName(value.ToString()));
            }
            AddToClassList("inputType" + ContextEnumToClassName(inputType.ToString()));

            var outputType = controller.model.outputType;
            foreach (VFXDataType value in System.Enum.GetValues(typeof(VFXDataType)))
            {
                if (value != outputType)
                    RemoveFromClassList("outputType" + ContextEnumToClassName(value.ToString()));
            }
            AddToClassList("outputType" + ContextEnumToClassName(outputType.ToString()));

            var type = controller.model.ownedType;
            foreach (VFXDataType value in System.Enum.GetValues(typeof(VFXDataType)))
            {
                if (value != type)
                    RemoveFromClassList("type" + ContextEnumToClassName(value.ToString()));
            }
            AddToClassList("type" + ContextEnumToClassName(type.ToString()));

            var space = controller.model.space;
            foreach (VFXCoordinateSpace val in System.Enum.GetValues(typeof(VFXCoordinateSpace)))
            {
                if (val != space || !controller.model.spaceable)
                    m_HeaderSpace.RemoveFromClassList("space" + val.ToString());
            }
            if (controller.model.spaceable)
                m_HeaderSpace.AddToClassList("space" + (controller.model.space).ToString());

            Profiler.EndSample();
            if (controller.model.outputType == VFXDataType.kNone)
            {
                if (m_Footer.parent != null)
                    m_Footer.RemoveFromHierarchy();
            }
            else
            {
                if (m_Footer.parent == null)
                    mainContainer.Add(m_Footer);
                m_FooterTitle.text = controller.model.outputType.ToString().Substring(1);
                m_FooterIcon.image = GetIconForVFXType(controller.model.outputType);
                m_FooterIcon.visible = m_FooterIcon.image.value != null;
            }

            Profiler.BeginSample("VFXContextUI.CreateInputFlow");
            HashSet<VisualElement> newInAnchors = new HashSet<VisualElement>();
            foreach (var inanchorcontroller in controller.flowInputAnchors)
            {
                var existing = m_FlowInputConnectorContainer.Select(t => t as VFXFlowAnchor).FirstOrDefault(t => t.controller == inanchorcontroller);
                if (existing == null)
                {
                    var anchor = VFXFlowAnchor.Create(inanchorcontroller);
                    m_FlowInputConnectorContainer.Add(anchor);
                    newInAnchors.Add(anchor);
                }
                else
                {
                    newInAnchors.Add(existing);
                }
            }

            foreach (var nonLongerExistingAnchor in m_FlowInputConnectorContainer.Where(t => !newInAnchors.Contains(t)).ToList()) // ToList to make a copy because the enumerable will change when we delete
            {
                m_FlowInputConnectorContainer.Remove(nonLongerExistingAnchor);
            }
            Profiler.EndSample();

            Profiler.BeginSample("VFXContextUI.CreateInputFlow");
            HashSet<VisualElement> newOutAnchors = new HashSet<VisualElement>();

            foreach (var outanchorcontroller in controller.flowOutputAnchors)
            {
                var existing = m_FlowOutputConnectorContainer.Select(t => t as VFXFlowAnchor).FirstOrDefault(t => t.controller == outanchorcontroller);
                if (existing == null)
                {
                    var anchor = VFXFlowAnchor.Create(outanchorcontroller);
                    m_FlowOutputConnectorContainer.Add(anchor);
                    newOutAnchors.Add(anchor);
                }
                else
                {
                    newOutAnchors.Add(existing);
                }
            }

            foreach (var nonLongerExistingAnchor in m_FlowOutputConnectorContainer.Where(t => !newOutAnchors.Contains(t)).ToList()) // ToList to make a copy because the enumerable will change when we delete
            {
                m_FlowOutputConnectorContainer.Remove(nonLongerExistingAnchor);
            }
            Profiler.EndSample();

            m_Label.text = controller.model.label;
            if (string.IsNullOrEmpty(m_Label.text))
            {
                m_Label.AddToClassList("empty");
            }
            else
            {
                m_Label.RemoveFromClassList("empty");
            }

            RefreshContext();
        }

        public VFXContextUI() : base("uxml/VFXContext")
        {
            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable | Capabilities.Ascendable;

            AddStyleSheetPath("VFXContext");
            AddStyleSheetPath("Selectable");

            AddToClassList("VFXContext");
            AddToClassList("selectable");

            this.mainContainer.clippingOptions = ClippingOptions.NoClipping;

            m_FlowInputConnectorContainer = this.Q("flow-inputs");

            m_FlowOutputConnectorContainer = this.Q("flow-outputs");

            m_HeaderIcon = titleContainer.Q<Image>("icon");
            m_HeaderSpace = titleContainer.Q<Image>("header-space");
            m_HeaderSpace.AddManipulator(new Clickable(OnSpace));

            m_BlockContainer = this.Q("block-container");
            m_NoBlock = m_BlockContainer.Q("no-blocks");

            m_Footer = this.Q("footer");

            m_FooterTitle = m_Footer.Q<Label>("title-label");
            m_FooterIcon = m_Footer.Q<Image>("icon");


            m_DragDisplay = new VisualElement();
            m_DragDisplay.AddToClassList("dragdisplay");

            m_Label = this.Q<Label>("user-label");
            m_TextField = this.Q<TextField>("user-title-textfield");
            m_TextField.visible = false;

            m_Label.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);
            m_TextField.RegisterCallback<ChangeEvent<string>>(OnTitleChange);
            m_TextField.RegisterCallback<BlurEvent>(OnTitleBlur);
            m_Label.RegisterCallback<GeometryChangedEvent>(OnTitleRelayout);

        }

        bool m_CanHaveBlocks = false;
        void OnSpace()
        {
            controller.model.space = (VFXCoordinateSpace)(((int)controller.model.space + 1) % (CoordinateSpaceInfo.SpaceCount));
        }

        public bool CanDrop(IEnumerable<VFXBlockUI> blocks)
        {
            bool accept = true;
            if (blocks.Count() == 0) return false;
            foreach (var block in blocks)
            {
                if (!controller.model.AcceptChild(block.controller.model))
                {
                    accept = false;
                    break;
                }
            }
            return accept;
        }

        public override bool HitTest(Vector2 localPoint)
        {
            // needed so that if we click on a block we won't select the context as well.
            /*if (m_NoBlock.parent ==  null && m_BlockContainer.ContainsPoint(this.ChangeCoordinatesTo(m_BlockContainer, localPoint)))
            {
                return false;
            }*/
            return ContainsPoint(localPoint);
        }

        public void DraggingBlocks(IEnumerable<VFXBlockUI> blocks, int index)
        {
            m_DragDisplay.RemoveFromHierarchy();

            if (!CanDrop(blocks))
            {
                return;
            }

            float y = GetBlockIndexY(index, false);

            m_DragDisplay.style.positionTop = y;

            m_BlockContainer.Add(m_DragDisplay);
        }

        public void RemoveDragIndicator()
        {
            if (m_DragDisplay.parent != null)
                m_BlockContainer.Remove(m_DragDisplay);
        }

        bool m_DragStarted;


        public bool CanAcceptDrop(List<ISelectable> selection)
        {
            IEnumerable<VFXBlockUI> blocksUI = selection.Select(t => t as VFXBlockUI).Where(t => t != null);

            return CanDrop(blocksUI);
        }

        public float GetBlockIndexY(int index, bool middle)
        {
            float y = 0;
            if (m_BlockContainer.childCount == 0)
            {
                return 0;
            }
            if (index >= m_BlockContainer.childCount)
            {
                return m_BlockContainer.ElementAt(m_BlockContainer.childCount - 1).layout.yMax;
            }
            else if (middle)
            {
                return m_BlockContainer.ElementAt(index).layout.center.y;
            }
            else
            {
                y = m_BlockContainer.ElementAt(index).layout.yMin;

                if (index > 0)
                {
                    y = (y + m_BlockContainer.ElementAt(index - 1).layout.yMax) * 0.5f;
                }
            }

            return y;
        }

        public int GetDragBlockIndex(Vector2 mousePosition)
        {
            for (int i = 0; i < m_BlockContainer.childCount; ++i)
            {
                float y = GetBlockIndexY(i, true);

                if (mousePosition.y < y)
                {
                    return i;
                }
            }

            return m_BlockContainer.childCount;
        }

        bool IDropTarget.DragEnter(DragEnterEvent evt, IEnumerable<ISelectable> selection, IDropTarget enteredTarget, ISelection dragSource)
        {
            return true;
        }

        bool IDropTarget.DragLeave(DragLeaveEvent evt, IEnumerable<ISelectable> selection, IDropTarget leftTarget, ISelection dragSource)
        {
            RemoveDragIndicator();
            return true;
        }

        bool IDropTarget.DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            IEnumerable<VFXBlockUI> blocksUI = selection.Select(t => t as VFXBlockUI).Where(t => t != null);

            Vector2 mousePosition = m_BlockContainer.WorldToLocal(evt.mousePosition);

            int blockIndex = GetDragBlockIndex(mousePosition);

            DraggingBlocks(blocksUI, blockIndex);
            if (!m_DragStarted)
            {
                // TODO: Do something on first DragUpdated event (initiate drag)
                m_DragStarted = true;
                AddToClassList("dropping");
            }
            else
            {
                // TODO: Do something on subsequent DragUpdated events
            }

            return true;
        }

        bool IDropTarget.DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            RemoveDragIndicator();

            Vector2 mousePosition = m_BlockContainer.WorldToLocal(evt.mousePosition);

            IEnumerable<VFXBlockUI> blocksUI = selection.OfType<VFXBlockUI>();
            if (!CanDrop(blocksUI))
                return true;

            int blockIndex = GetDragBlockIndex(mousePosition);

            BlocksDropped(blockIndex, blocksUI, evt.ctrlKey);

            DragAndDrop.AcceptDrag();

            m_DragStarted = false;
            RemoveFromClassList("dropping");

            return true;
        }

        public void BlocksDropped(int blockIndex, IEnumerable<VFXBlockUI> draggedBlocks, bool copy)
        {
            HashSet<VFXContextController> contexts = new HashSet<VFXContextController>();
            foreach (var draggedBlock in draggedBlocks)
            {
                contexts.Add(draggedBlock.context.controller);
            }

            using (var growContext = new GrowContext(this))
            {
                controller.BlocksDropped(blockIndex, draggedBlocks.Select(t => t.controller), copy);

                foreach (var context in contexts)
                {
                    context.ApplyChanges();
                }
            }
        }

        bool IDropTarget.DragExited()
        {
            // TODO: Do something when current drag is canceled
            RemoveDragIndicator();
            m_DragStarted = false;

            return true;
        }

        public override void SetPosition(Rect newPos)
        {
            //if (classList.Contains("vertical"))
            /*{
                base.SetPosition(newPos);
            }
            else*/
            {
                style.positionType = PositionType.Absolute;
                style.positionLeft = newPos.x;
                style.positionTop = newPos.y;
            }
        }

        public void RemoveBlock(VFXBlockUI block)
        {
            if (block == null)
                return;

            controller.RemoveBlock(block.controller.model);
        }

        private void InstantiateBlock(VFXBlockController blockController)
        {
            Profiler.BeginSample("VFXContextUI.InstantiateBlock");
            Profiler.BeginSample("VFXContextUI.new VFXBlockUI");
            var blockUI = new VFXBlockUI();
            Profiler.EndSample();
            blockUI.controller = blockController;

            m_BlockContainer.Add(blockUI);
            Profiler.EndSample();
        }

        public void RefreshContext()
        {
            Profiler.BeginSample("VFXContextUI.RefreshContext");
            var blockControllers = controller.blockControllers;
            int blockControllerCount = blockControllers.Count();

            // recreate the children list based on the controller list to keep the order.

            var blocksUIs = new Dictionary<VFXBlockController, VFXBlockUI>();

            bool somethingChanged = m_BlockContainer.childCount < blockControllerCount || (!m_CanHaveBlocks && m_NoBlock.parent != null);

            int cptBlock = 0;
            for (int i = 0; i < m_BlockContainer.childCount; ++i)
            {
                var child = m_BlockContainer.ElementAt(i) as VFXBlockUI;
                if (child != null)
                {
                    blocksUIs.Add(child.controller, child);

                    if (!somethingChanged && blockControllerCount > cptBlock && child.controller != blockControllers[cptBlock])
                    {
                        somethingChanged = true;
                    }
                    cptBlock++;
                }
            }
            if (somethingChanged || cptBlock != blockControllerCount)
            {
                foreach (var kv in blocksUIs)
                {
                    kv.Value.RemoveFromClassList("first");
                    m_BlockContainer.Remove(kv.Value);
                }
                if (blockControllers.Count() > 0 || !m_CanHaveBlocks)
                {
                    m_NoBlock.RemoveFromHierarchy();
                }
                else if (m_NoBlock.parent == null)
                {
                    m_BlockContainer.Add(m_NoBlock);
                }
                if (blockControllers.Count > 0)
                {
                    foreach (var blockController in blockControllers)
                    {
                        VFXBlockUI blockUI;
                        if (blocksUIs.TryGetValue(blockController, out blockUI))
                        {
                            m_BlockContainer.Add(blockUI);
                        }
                        else
                        {
                            InstantiateBlock(blockController);
                        }
                    }
                    VFXBlockUI firstBlock = m_BlockContainer.Query<VFXBlockUI>();
                    firstBlock.AddToClassList("first");
                }
            }
            Profiler.EndSample();
        }

        Texture2D GetIconForVFXType(VFXDataType type)
        {
            switch (type)
            {
                case VFXDataType.kNone:
                    return Resources.Load<Texture2D>("VFX/Execution");
                case VFXDataType.kParticle:
                    return Resources.Load<Texture2D>("VFX/Particles");
            }
            return null;
        }

        class GrowContext : IDisposable
        {
            VFXContextUI m_Context;
            float m_PrevSize;
            public GrowContext(VFXContextUI context)
            {
                m_Context = context;
                m_PrevSize = context.layout.size.y;
            }

            void IDisposable.Dispose()
            {
                VFXView view = m_Context.GetFirstAncestorOfType<VFXView>();
                m_Context.controller.ApplyChanges();
                m_Context.panel.InternalValidateLayout();

                view.PushUnderContext(m_Context, m_Context.layout.size.y - m_PrevSize);
            }
        }

        void AddBlock(Vector2 position, VFXModelDescriptor<VFXBlock> descriptor)
        {
            int blockIndex = -1;

            var blocks = m_BlockContainer.Query().OfType<VFXBlockUI>().ToList();
            for (int i = 0; i < blocks.Count; ++i)
            {
                Rect worldBounds = blocks[i].worldBound;
                if (worldBounds.Contains(position))
                {
                    if (position.y > worldBounds.center.y)
                    {
                        blockIndex = i + 1;
                    }
                    else
                    {
                        blockIndex = i;
                    }
                    break;
                }
            }

            using (var growContext = new GrowContext(this))
            {
                controller.AddBlock(blockIndex, descriptor.CreateInstance());
            }
        }

        public void OnCreateBlock(DropdownMenu.MenuAction evt)
        {
            Vector2 referencePosition = evt.eventInfo.mousePosition;

            OnCreateBlock(referencePosition);
        }

        public void OnCreateBlock(Vector2 referencePosition)
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();

            Vector2 screenPosition = view.ViewToScreenPosition(referencePosition);

            VFXFilterWindow.Show(VFXViewWindow.currentWindow, referencePosition, screenPosition, m_BlockProvider);
        }

        VFXBlockProvider m_BlockProvider = null;

        // TODO: Remove, unused except for debugging
        // Declare new USS rect-color and use it
        protected override void OnStyleResolved(ICustomStyle styles)
        {
            base.OnStyleResolved(styles);
            styles.ApplyCustomProperty(RectColorProperty, ref m_RectColor);
        }

        // TODO: Remove, unused except for debugging
        StyleValue<Color> m_RectColor;
        Color rectColor { get { return m_RectColor.GetSpecifiedValueOrDefault(Color.magenta); } }

        public IEnumerable<VFXBlockUI> GetAllBlocks()
        {
            foreach (VFXBlockUI block in m_BlockContainer.OfType<VFXBlockUI>())
            {
                yield return block;
            }
        }

        public IEnumerable<Port> GetAllAnchors(bool input, bool output)
        {
            return (IEnumerable<Port>)GetFlowAnchors(input, output);
        }

        public IEnumerable<VFXFlowAnchor> GetFlowAnchors(bool input, bool output)
        {
            if (input)
                foreach (VFXFlowAnchor anchor in m_FlowInputConnectorContainer)
                {
                    yield return anchor;
                }
            if (output)
                foreach (VFXFlowAnchor anchor in m_FlowOutputConnectorContainer)
                {
                    yield return anchor;
                }
        }

        public class VFXContextOnlyVFXNodeProvider : VFXNodeProvider
        {
            public VFXContextOnlyVFXNodeProvider(VFXViewController controller, Action<Descriptor, Vector2> onAddBlock, Func<Descriptor, bool> filter) :
                base(controller, onAddBlock, filter, new Type[] { typeof(VFXContext)})
            {
            }

            protected override string GetCategory(Descriptor desc)
            {
                return string.Empty;
            }
        }

        bool ProviderFilter(VFXNodeProvider.Descriptor d)
        {
            VFXModelDescriptor desc = d.modelDescriptor as VFXModelDescriptor;
            if (desc == null)
                return false;

            if (!(desc.model is VFXAbstractParticleOutput))
                return false;

            return (desc.model as VFXContext).contextType == VFXContextType.kOutput;
        }

        void OnConvertContext(DropdownMenu.MenuAction action)
        {
            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            VFXFilterWindow.Show(VFXViewWindow.currentWindow, action.eventInfo.mousePosition, view.ViewToScreenPosition(action.eventInfo.mousePosition), new VFXContextOnlyVFXNodeProvider(view.controller, ConvertContext, ProviderFilter));
        }

        void ConvertContext(VFXNodeProvider.Descriptor d, Vector2 mPos)
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();
            VFXViewController viewController = controller.viewController;
            if (view == null) return;

            mPos = view.contentViewContainer.ChangeCoordinatesTo(view, controller.position);

            var newNodeController = view.AddNode(d, mPos);
            var newContextController = newNodeController as VFXContextController;

            //transfer blocks
            foreach (var block in controller.model.children.ToArray()) // To array needed as the IEnumerable content will change
                newContextController.AddBlock(-1, block);

            //transfer settings
            var contextType = controller.model.GetType();
            foreach (var setting in newContextController.model.GetSettings(true))
            {
                FieldInfo myField = contextType.GetField(setting.Name, BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic);
                if (myField == null || myField.GetCustomAttributes(typeof(VFXSettingAttribute), true).Length == 0)
                    continue;

                object value;
                if (VFXConverter.TryConvertTo(myField.GetValue(controller.model), setting.FieldType, out value))
                {
                    newContextController.model.SetSettingValue(setting.Name, value);
                }
            }

            //transfer flow edges
            if (controller.flowInputAnchors.Count == 1)
            {
                foreach (var output in controller.flowInputAnchors[0].connections.Select(t => t.output).ToArray())
                {
                    newContextController.model.LinkFrom(output.context.model, output.slotIndex);
                }
            }

            // Apply the slot changes that can be the result of settings changes
            newContextController.ApplyChanges();

            //transfer master slot values
            foreach (var slot in newContextController.model.inputSlots)
            {
                VFXSlot mySlot = controller.model.inputSlots.FirstOrDefault(t => t.name == slot.name);
                if (mySlot == null)
                    continue;

                object value;
                if (VFXConverter.TryConvertTo(mySlot.value, slot.property.type, out value))
                {
                    slot.value = value;
                }
            }

            foreach (var anchor in newContextController.inputPorts)
            {
                string path = anchor.path;
                var myAnchor = controller.inputPorts.FirstOrDefault(t => t.path == path);

                if (myAnchor == null || !myAnchor.HasLink())
                    continue;

                //There should be only one
                var output = myAnchor.connections.First().output;

                viewController.CreateLink(anchor, output);
            }

            // Apply the change so that it won't unlink the blocks links
            controller.ApplyChanges();

            viewController.RemoveElement(controller);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is VFXContextUI || evt.target is VFXBlockUI)
            {
                if (m_CanHaveBlocks)
                {
                    evt.menu.InsertAction(0, "Create Block", OnCreateBlock, e => DropdownMenu.MenuAction.StatusFlags.Normal);
                    evt.menu.AppendSeparator();
                }
            }

            if (evt.target is VFXContextUI && controller.model is VFXAbstractParticleOutput)
            {
                evt.menu.InsertAction(1,"Convert Output", OnConvertContext, e => DropdownMenu.MenuAction.StatusFlags.Normal);
            }
        }

        void UpdateTitleFieldRect()
        {
            Rect rect = m_Label.layout;

            m_Label.parent.ChangeCoordinatesTo(m_TextField.parent, rect);


            m_TextField.style.positionTop = rect.yMin;
            m_TextField.style.positionLeft = rect.xMin;
            m_TextField.style.positionRight = m_Label.style.marginRight.value + m_Label.style.borderRightWidth.value;
            m_TextField.style.height = rect.height - m_Label.style.marginTop - m_Label.style.marginBottom;
        }

        void OnTitleMouseDown(MouseDownEvent e)
        {
            if (e.clickCount == 2)
            {
                OnRename();
                e.StopPropagation();
                e.PreventDefault();
            }
        }

        public void OnRename()
        {
            m_Label.RemoveFromClassList("empty");
            m_TextField.value = m_Label.text;
            m_TextField.visible = true;
            UpdateTitleFieldRect();

            m_TextField.Focus();
            m_TextField.SelectAll();

        }

        void OnTitleBlur(BlurEvent e)
        {
            controller.model.label = m_TextField.value
                .Trim()
                .Replace("/","")
                .Replace("\\", "")
                .Replace(":", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace("*", "")
                .Replace("?", "")
                .Replace("\"", "")
                .Replace("|", "")
                ;
            m_TextField.visible = false;
        }
        void OnTitleRelayout(GeometryChangedEvent e)
        {
            if( m_TextField.visible)
                UpdateTitleFieldRect();
        }

        void OnTitleChange(ChangeEvent<string> e)
        {
            m_Label.text = m_TextField.value;
        }

    }
}
