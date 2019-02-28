using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class OrientedBoxVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The box used for the volume calculation.")]
            public OrientedBox box = new OrientedBox();
        }

        public class OutputProperties
        {
            [Tooltip("The volume of the box.")]
            public float volume;
        }

        override public string name { get { return "Volume (Oriented Box)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.BoxVolume(inputExpression[2]) };
        }
    }
}
