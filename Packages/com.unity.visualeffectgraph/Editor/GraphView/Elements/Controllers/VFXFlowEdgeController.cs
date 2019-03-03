using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    internal class VFXFlowEdgeController : VFXEdgeController<VFXFlowAnchorController>
    {
        public VFXFlowEdgeController(VFXFlowAnchorController input, VFXFlowAnchorController output) : base(input, output)
        {
        }
    }
}
