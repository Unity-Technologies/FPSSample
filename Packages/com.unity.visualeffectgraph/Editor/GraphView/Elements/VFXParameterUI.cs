using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Profiling;

namespace UnityEditor.VFX.UI
{
    class VFXParameterDataAnchor : VFXOutputDataAnchor
    {
        public static new VFXParameterDataAnchor Create(VFXDataAnchorController controller, VFXNodeUI node)
        {
            var anchor = new VFXParameterDataAnchor(controller.orientation, controller.direction, controller.portType, node);

            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.controller = controller;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        protected VFXParameterDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type, VFXNodeUI node) : base(anchorOrientation, anchorDirection, type, node)
        {
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return base.ContainsPoint(localPoint) && !m_ConnectorText.ContainsPoint(this.ChangeCoordinatesTo(m_ConnectorText, localPoint));
        }
    }


    static class UXMLHelper
    {
        const string folderName = "Editor Default Resources";

        public static string GetUXMLPath(string name)
        {
            string path = null;
            if (s_Cache.TryGetValue(name, out path))
            {
                return path;
            }
            return GetUXMLPathRecursive("Assets", name);
        }

        static Dictionary<string, string> s_Cache = new Dictionary<string, string>();

        static string GetUXMLPathRecursive(string path, string name)
        {
            Profiler.BeginSample("UXMLHelper.GetUXMLPathRecursive");
            string localFileName = path + "/" + folderName + "/" + name;
            if (System.IO.File.Exists(localFileName))
            {
                Profiler.EndSample();
                s_Cache[name] = localFileName;
                return localFileName;
            }

            foreach (var dir in System.IO.Directory.GetDirectories(path))
            {
                if (dir.Length <= folderName.Length || !dir.EndsWith(folderName) || !"/\\".Contains(dir[dir.Length - folderName.Length - 1]))
                {
                    string result = GetUXMLPathRecursive(dir, name);
                    if (result != null)
                    {
                        Profiler.EndSample();
                        return result;
                    }
                }
            }

            Profiler.EndSample();
            return null;
        }
    }


    class VFXParameterUI : VFXNodeUI
    {
        public VFXParameterUI() : base("uxml/VFXParameter")
        {
            RemoveFromClassList("VFXNodeUI");
            AddStyleSheetPath("VFXParameter");
            AddStyleSheetPath("StyleSheets/GraphView/Node.uss");

            RegisterCallback<MouseEnterEvent>(OnMouseHover);
            RegisterCallback<MouseLeaveEvent>(OnMouseHover);

            m_ExposedIcon = this.Q<Image>("exposed-icon");
            m_SuperCollapsedButton = this.Q("super-collapse-button");
            m_SuperCollapsedButton.AddManipulator(new Clickable(OnToggleSuperCollapse));

            this.AddManipulator(new SuperCollapser());

            m_Pill = this.Q("pill");
        }

        VisualElement m_Pill;

        void OnToggleSuperCollapse()
        {
            controller.superCollapsed = !controller.superCollapsed;
        }

        VisualElement m_SuperCollapsedButton;

        public new VFXParameterNodeController controller
        {
            get { return base.controller as VFXParameterNodeController; }
        }

        protected override bool syncInput
        {
            get { return false; }
        }

        public override VFXDataAnchor InstantiateDataAnchor(VFXDataAnchorController controller, VFXNodeUI node)
        {
            return VFXParameterDataAnchor.Create(controller, node);
        }

        Image m_ExposedIcon;

        protected override void SelfChange()
        {
            base.SelfChange();

            if (m_ExposedIcon != null)
                m_ExposedIcon.visible = controller.parentController.exposed;

            if (controller.parentController.exposed)
            {
                AddToClassList("exposed");
            }
            else
            {
                RemoveFromClassList("exposed");
            }

            if (m_Pill != null)
                m_Pill.tooltip = controller.parentController.model.tooltip;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target == this && controller != null)
            {
                evt.menu.AppendAction("Convert to Inline", OnConvertToInline, e => DropdownMenu.MenuAction.StatusFlags.Normal);
                evt.menu.AppendSeparator();
            }
        }

        void OnConvertToInline(DropdownMenu.MenuAction evt)
        {
            controller.ConvertToInline();
        }

        void OnMouseHover(EventBase evt)
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();
            if (view == null)
                return;
            VFXBlackboard blackboard = view.blackboard;
            if (blackboard == null)
                return;
            VFXBlackboardRow row = blackboard.GetRowFromController(controller.parentController);
            if (row == null)
                return;

            if (evt.GetEventTypeId() == MouseEnterEvent.TypeId())
                row.AddToClassList("hovered");
            else
                row.RemoveFromClassList("hovered");
        }
    }
}
