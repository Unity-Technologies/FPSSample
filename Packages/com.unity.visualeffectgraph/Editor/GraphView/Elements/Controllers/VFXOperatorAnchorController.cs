using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    abstract class VFXOperatorAnchorController : VFXDataAnchorController
    {
        public VFXOperatorAnchorController(VFXSlot model, VFXNodeController sourceNode, bool hidden) : base(model, sourceNode, hidden)
        {
        }

        public override void UpdateInfos()
        {
            base.UpdateInfos();
            if (model.direction == VFXSlot.Direction.kInput)
            {
                System.Type newAnchorType = model.property.type;

                if (newAnchorType != portType)
                {
                    portType = newAnchorType;
                }
            }
        }
    }


    class VFXInputOperatorAnchorController : VFXOperatorAnchorController
    {
        public VFXInputOperatorAnchorController(VFXSlot model, VFXNodeController sourceNode, bool hidden) : base(model, sourceNode, hidden)
        {
        }

        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    class VFXOutputOperatorAnchorController : VFXOperatorAnchorController
    {
        public VFXOutputOperatorAnchorController(VFXSlot model, VFXNodeController sourceNode, bool hidden) : base(model, sourceNode, hidden)
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
