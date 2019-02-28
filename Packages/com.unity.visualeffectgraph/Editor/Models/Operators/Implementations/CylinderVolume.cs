using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class CylinderVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The cylinder used for the volume calculation.")]
            public Cylinder cylinder = new Cylinder();
        }

        public class OutputProperties
        {
            [Tooltip("The volume of the cylinder.")]
            public float volume;
        }

        override public string name { get { return "Volume (Cylinder)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.CylinderVolume(inputExpression[1], inputExpression[2]) };
        }
    }
}
