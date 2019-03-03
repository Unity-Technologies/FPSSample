using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class TorusVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The torus used for the volume calculation.")]
            public Torus torus = new Torus();
        }

        public class OutputProperties
        {
            [Tooltip("The volume of the torus.")]
            public float volume;
        }

        override public string name { get { return "Volume (Torus)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.TorusVolume(inputExpression[1], inputExpression[2]) };
        }
    }
}
