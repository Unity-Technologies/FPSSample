using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
    class LookAt : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The eye position.")]
            public Position from = new Position() { position = Vector3.zero };
            [Tooltip("The target position.")]
            public Position to = new Position() { position = Vector3.one };
            [Normalize, Tooltip("The up vector.")]
            public DirectionType up = Vector3.up;
        }

        public class OutputProperties
        {
            public Transform o = Transform.defaultValue;
        }

        override public string name { get { return "Look At"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression from = inputExpression[0];
            VFXExpression to = inputExpression[1];
            VFXExpression up = inputExpression[2];

            VFXExpression viewVector = to - from;

            VFXExpression z = VFXOperatorUtility.Normalize(viewVector);
            VFXExpression x = VFXOperatorUtility.Normalize(VFXOperatorUtility.Cross(up, z));
            VFXExpression y = VFXOperatorUtility.Cross(z, x);

            VFXExpression matrix = new VFXExpressionVector3sToMatrix(x, y, z, from);
            return new[] { matrix };
        }
    }
}
