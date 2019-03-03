using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class DistanceToPlane : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The plane used for the distance calculation.")]
            public Plane plane = new Plane();
            [Tooltip("The position used for the distance calculation.")]
            public Position position = new Position();
        }

        public class OutputProperties
        {
            [Tooltip("The closest point on the plane to the supplied position.")]
            public Vector3 closestPosition;
            [Tooltip("The signed distance from the plane.")]
            public float distance;
        }

        override public string name { get { return "Distance (Plane)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression planeDistance = VFXOperatorUtility.SignedDistanceToPlane(inputExpression[0], inputExpression[1], inputExpression[2]);
            VFXExpression pointOnPlane = (inputExpression[2] - inputExpression[1] * VFXOperatorUtility.CastFloat(planeDistance, inputExpression[1].valueType));
            return new VFXExpression[] { pointOnPlane, planeDistance };
        }
    }
}
