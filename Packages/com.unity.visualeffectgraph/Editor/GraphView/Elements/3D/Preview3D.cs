#if false
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    /* class Preview3DController : SimpleElementPresenter
     {
         public Preview3DController()
         {
             title = "3D Preview";

             position = new Rect(100, 100, 300, 300);
         }

         public new void OnEnable()
         {
             base.OnEnable();
             capabilities |= Capabilities.Movable | Capabilities.Resizable;
         }
     }*/
    class Preview3D : GraphElement
    {
        Label m_Label;
        Element3D m_Element;


        public Preview3D()
        {
            style.flexDirection = FlexDirection.Column;
            style.alignItems = Align.Stretch;

            m_Label = new Label();
            Add(m_Label);


            m_Element = new Element3D();
            Add(m_Element);

            m_Element.style.flex = 1;


            style.width = style.height = 300;


            m_Element.AddManipulator(new Rotate3DManipulator(m_Element));
        }

        /*
        public void OnDataChanged()
        {
            base.OnDataChanged();

            Preview3DController controller = GetPresenter<Preview3DController>();

            m_Label.text = controller.title;
        }
        */
    }
}
#endif
