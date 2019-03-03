using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class SphereVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The sphere used for the volume calculation.")]
            public Sphere sphere = new Sphere();
        }

        public class OutputProperties
        {
            [Tooltip("The volume of the sphere.")]
            public float volume;
        }

        override public string name { get { return "Volume (Sphere)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.SphereVolume(inputExpression[1]) };
        }
    }
}
