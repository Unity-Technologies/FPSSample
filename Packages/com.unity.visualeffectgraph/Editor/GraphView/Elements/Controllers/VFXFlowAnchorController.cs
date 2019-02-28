using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXFlowAnchorController : Controller, IVFXAnchorController
    {
        VFXContextController m_Context;
        public VFXContext owner { get { return m_Context.model; } }
        public VFXContextController context { get { return m_Context; } }

        private int m_SlotIndex;
        public int slotIndex { get { return m_SlotIndex; } }

        public void Init(VFXContextController context, int slotIndex)
        {
            m_Context = context;
            m_SlotIndex = slotIndex;
        }

        List<VFXFlowEdgeController> m_Connections = new List<VFXFlowEdgeController>();

        public virtual void Connect(VFXEdgeController edgeController)
        {
            m_Connections.Add(edgeController as VFXFlowEdgeController);
        }

        public virtual void Disconnect(VFXEdgeController edgeController)
        {
            m_Connections.Remove(edgeController as VFXFlowEdgeController);
        }

        public bool connected
        {
            get { return m_Connections.Count > 0; }
        }

        public virtual bool IsConnectable()
        {
            return true;
        }

        public abstract Direction direction { get; }
        public Orientation orientation { get { return Orientation.Vertical; } }

        public IEnumerable<VFXFlowEdgeController> connections { get { return m_Connections; } }

        public override void ApplyChanges()
        {
        }

        public virtual string title
        {
            get {return ""; }
        }
    }

    class VFXFlowInputAnchorController : VFXFlowAnchorController
    {
        public VFXFlowInputAnchorController()
        {
        }

        public override string title
        {
            get
            {
                if (owner is VFXBasicSpawner)
                {
                    if (slotIndex == 0)
                    {
                        return "Start";
                    }
                    return "Stop";
                }
                return "";
            }
        }

        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    class VFXFlowOutputAnchorController : VFXFlowAnchorController
    {
        public VFXFlowOutputAnchorController()
        {
        }

        public override Direction direction
        {
            get
            {
                return Direction.Output;
            }
        }
    }
}
