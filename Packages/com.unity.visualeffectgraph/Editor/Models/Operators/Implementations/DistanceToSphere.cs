using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class DistanceToSphere : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The sphere used for the distance calculation.")]
            public Sphere sphere = new Sphere();
            [Tooltip("The position used for the distance calculation.")]
            public Position position = new Position();
        }

        public class OutputProperties
        {
            [Tooltip("The closest point on the sphere to the supplied position.")]
            public Vector3 closestPosition;
            [Tooltip("The signed distance from the sphere. (Negative values represent points that are inside the sphere).")]
            public float distance;
        }

        override public string name { get { return "Distance (Sphere)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression sphereDelta = (inputExpression[2] - inputExpression[0]);
            VFXExpression sphereDeltaLength = VFXOperatorUtility.Length(sphereDelta);
            VFXExpression sphereDistance = (sphereDeltaLength - inputExpression[1]);

            VFXExpression pointOnSphere = (inputExpression[1] / sphereDeltaLength);
            pointOnSphere = (sphereDelta * VFXOperatorUtility.CastFloat(pointOnSphere, inputExpression[0].valueType) + inputExpression[0]);

            return new VFXExpression[] { pointOnSphere, sphereDistance };
        }
    }
}
