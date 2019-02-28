using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using System.Collections.Generic;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    partial class VFXOutputDataAnchor : VFXDataAnchor
    {
        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static new VFXOutputDataAnchor Create(VFXDataAnchorController controller, VFXNodeUI node)
        {
            var anchor = new VFXOutputDataAnchor(controller.orientation, controller.direction, controller.portType, node);

            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.controller = controller;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }
        VisualElement m_Icon;

        protected VFXOutputDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type, VFXNodeUI node) : base(anchorOrientation, anchorDirection, type, node)
        {
            m_Icon = new VisualElement()
            {
                name = "icon"
            };

            //Add(new VisualElement() { name = "lineSpacer" });
            AddToClassList("VFXOutputDataAnchor");
            Add(m_Icon); //insert at first ( right since reversed)
        }

        void OnToggleExpanded()
        {
            if (controller.expandedSelf)
            {
                controller.RetractPath();
            }
            else
            {
                controller.ExpandPath();
            }
        }

        VisualElement[] m_Lines;


        Clickable m_ExpandClickable;

        public override void SelfChange(int change)
        {
            base.SelfChange(change);

            if (controller.depth != 0 && m_Lines == null)
            {
                m_Lines = new VisualElement[controller.depth + 1];

                for (int i = 0; i < controller.depth; ++i)
                {
                    var line = new VisualElement();
                    line.style.width = 1;
                    line.name = "line";
                    line.style.marginLeft = PropertyRM.depthOffset-2;
                    line.style.marginRight = 0;

                    Insert(3, line);
                    m_Lines[i] = line;
                }
            }


            if (controller.expandable)
            {
                if( controller.expandedSelf)
                {
                    AddToClassList("icon-expanded");
                }
                else
                {
                    RemoveFromClassList("icon-expanded");
                }
                AddToClassList("icon-expandable");

                if (m_ExpandClickable == null)
                {
                    m_ExpandClickable = new Clickable(OnToggleExpanded);
                    m_Icon.AddManipulator(m_ExpandClickable);
                }
            }
            else
            {
                m_Icon.style.backgroundImage = null;
                if( m_ExpandClickable != null)
                {
                    m_Icon.RemoveManipulator(m_ExpandClickable);
                    m_ExpandClickable = null;
                }
            }


            string text = "";
            string tooltip = null;
            VFXPropertyAttribute.ApplyToGUI(controller.attributes, ref text, ref tooltip);

            this.tooltip = tooltip;
        }

        public Rect internalRect
        {
            get
            {
                Rect layout = this.layout;
                return new Rect(0.0f, 0.0f, layout.width, layout.height);
            }
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return internalRect.Contains(localPoint);
        }
    }
}
