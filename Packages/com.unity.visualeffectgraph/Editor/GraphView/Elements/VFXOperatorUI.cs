using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX.UIElements;
using Branch = UnityEditor.VFX.Operator.VFXOperatorDynamicBranch;

namespace UnityEditor.VFX.UI
{
    class VFXOperatorUI : VFXNodeUI
    {
        VisualElement m_EditButton;

        public VFXOperatorUI()
        {
            AddStyleSheetPath("VFXOperator");

            m_Middle = new VisualElement();
            m_Middle.name = "middle";
            inputContainer.parent.Insert(1, m_Middle);

            m_EditButton = new VisualElement() {name = "edit"};
            m_EditButton.Add(new VisualElement() { name = "icon" });
            m_EditButton.AddManipulator(new Clickable(OnEdit));
            this.AddManipulator(new SuperCollapser());

            RegisterCallback<GeometryChangedEvent>(OnPostLayout);
        }

        VisualElement m_EditContainer;

        void OnEdit()
        {
            if (m_EditContainer != null)
            {
                if (m_EditContainer.parent != null)
                {
                    m_EditContainer.RemoveFromHierarchy();
                }
                else
                {
                    expanded = true;
                    RefreshPorts(); // refresh port to make sure outputContainer is added before the editcontainer.
                    topContainer.Add(m_EditContainer);
                }

                UpdateCollapse();
            }
        }

        VisualElement m_Middle;

        public new VFXOperatorController controller
        {
            get { return base.controller as VFXOperatorController; }
        }


        public override void GetPreferedWidths(ref float labelWidth, ref float controlWidth)
        {
            base.GetPreferedWidths(ref labelWidth, ref controlWidth);

            foreach (var port in GetPorts(true, false).Cast<VFXEditableDataAnchor>())
            {
                float portLabelWidth = port.GetPreferredLabelWidth() + 1;
                float portControlWidth = port.GetPreferredControlWidth();

                if (labelWidth < portLabelWidth)
                {
                    labelWidth = portLabelWidth;
                }
                if (controlWidth < portControlWidth)
                {
                    controlWidth = portControlWidth;
                }
            }
        }

        public override void ApplyWidths(float labelWidth, float controlWidth)
        {
            base.ApplyWidths(labelWidth, controlWidth);
            foreach (var port in GetPorts(true, false).Cast<VFXEditableDataAnchor>())
            {
                port.SetLabelWidth(labelWidth);
            }
            inputContainer.style.width = labelWidth + controlWidth + 20;
        }

        public bool isEditable
        {
            get
            {
                return controller != null && controller.isEditable;
            }
        }

        protected VisualElement GetControllerEditor()
        {
            if (controller is VFXCascadedOperatorController)
            {
                var edit = new VFXCascadedOperatorEdit();
                edit.controller = controller as VFXCascadedOperatorController;
                return edit;
            }
            if (controller is VFXNumericUniformOperatorController)
            {
                var edit = new VFXUniformOperatorEdit<VFXNumericUniformOperatorController, VFXOperatorNumericUniform>();
                edit.controller = controller as VFXNumericUniformOperatorController;
                return edit;
            }
            if (controller is VFXBranchOperatorController)
            {
                var edit = new VFXUniformOperatorEdit<VFXBranchOperatorController, Branch>();
                edit.controller = controller as VFXBranchOperatorController;
                return edit;
            }
            if (controller is VFXUnifiedOperatorController)
            {
                var edit = new VFXUnifiedOperatorEdit();
                edit.controller = controller as VFXUnifiedOperatorController;
                return edit;
            }
            return null;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target == this && controller != null && controller.model is VFXInlineOperator)
            {
                evt.menu.AppendAction("Convert to Parameter", OnConvertToParameter, e => DropdownMenu.MenuAction.StatusFlags.Normal);
                evt.menu.AppendSeparator();
            }
        }

        void OnConvertToParameter(DropdownMenu.MenuAction evt)
        {
            controller.ConvertToParameter();
        }

        public override bool superCollapsed
        {
            get { return base.superCollapsed && (m_EditContainer == null || m_EditContainer.parent == null); }
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            bool hasMiddle = inputContainer.childCount != 0;
            if (hasMiddle)
            {
                if (m_Middle.parent == null)
                {
                    inputContainer.parent.Insert(1, m_Middle);
                }
            }
            else if (m_Middle.parent != null)
            {
                m_Middle.RemoveFromHierarchy();
            }

            if (isEditable)
            {
                if (m_EditButton.parent == null)
                {
                    titleContainer.Insert(1, m_EditButton);
                }
                if (m_EditContainer == null)
                {
                    m_EditContainer = GetControllerEditor();
                    if (m_EditContainer != null)
                        m_EditContainer.name = "edit-container";
                }
            }
            else
            {
                if (m_EditContainer != null && m_EditContainer.parent != null)
                {
                    m_EditContainer.RemoveFromHierarchy();
                }
                m_EditContainer = null;
                if (m_EditButton.parent != null)
                {
                    m_EditButton.RemoveFromHierarchy();
                }
            }
        }

        void OnPostLayout(GeometryChangedEvent e)
        {
            RefreshLayout();
        }

        public override void RefreshLayout()
        {
            base.RefreshLayout();
            if (!superCollapsed)
            {
                float settingsLabelWidth = 30;
                float settingsControlWidth = 50;
                GetPreferedSettingsWidths(ref settingsLabelWidth, ref settingsControlWidth);

                float labelWidth = 30;
                float controlWidth = 50;
                GetPreferedWidths(ref labelWidth, ref controlWidth);

                float newMinWidth = Mathf.Max(settingsLabelWidth + settingsControlWidth, labelWidth + controlWidth) + 20;

                if (style.minWidth != newMinWidth)
                {
                    style.minWidth = newMinWidth;
                }

                ApplySettingsWidths(settingsLabelWidth, settingsControlWidth);

                ApplyWidths(labelWidth, controlWidth);
            }
            else
            {
                if (style.minWidth != 0)
                {
                    style.minWidth = 0;
                }
            }
        }
    }
}
