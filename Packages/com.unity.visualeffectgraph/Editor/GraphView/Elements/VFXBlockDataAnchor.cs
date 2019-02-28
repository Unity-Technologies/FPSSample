using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Collections.Generic;
using Type = System.Type;
using System.Linq;
using UnityEngine.Profiling;

namespace UnityEditor.VFX.UI
{
    class VFXBlockDataAnchor : VFXEditableDataAnchor
    {
        protected VFXBlockDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type, VFXNodeUI node) : base(anchorOrientation, anchorDirection, type, node)
        {
        }

        public static new VFXBlockDataAnchor Create(VFXDataAnchorController controller, VFXNodeUI node)
        {
            Profiler.BeginSample("VFXBlockDataAnchor.Create");
            var anchor = new VFXBlockDataAnchor(controller.orientation, controller.direction, controller.portType, node);
            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.controller = controller;

            anchor.AddManipulator(anchor.m_EdgeConnector);
            Profiler.EndSample();
            return anchor;
        }
    }
}
