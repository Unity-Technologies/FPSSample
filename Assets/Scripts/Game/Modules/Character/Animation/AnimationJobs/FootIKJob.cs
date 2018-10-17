using System;
using UnityEngine;
using UnityEngine.Experimental.Animations;
using UnityEngine.Serialization;

public struct FootIkJob : IAnimationJob
{    
    [Serializable]
    public struct JobSettings
    {
        [Tooltip("Place over toe bone when in it's stand idle pose. " +
            "Note that the Foot IK should not be enabled while adjusting!")]
        [FormerlySerializedAs("leftToeIdlePos")]
        public Vector3 leftToeStandPos;

        [Tooltip("Place over toe bone when in it's stand idle pose. " +
            "Note that the Foot IK should not be enabled while adjusting!")]
        [FormerlySerializedAs("rightToeIdlePos")]
        public Vector3 rightToeStandPos;  
        public bool debugIdlePos;
     
        [Space(10)]
        public bool enabled;
           
        [Space(10)]
        [Range(0f, 1f)]
        public float emitRayOffset;
        [Range(0f, 20f)]
        public float maxRayDistance;
        public bool debugRayCast;   

        [Space(10)]
        [Range(0f, 1f)]
        public float maxStepSize;
        [Range(-90f, 90f)]
        public float weightShiftAngle;
        [Range(-1f, 1f)]
        public float weightShiftHorizontal;
        [Range(-1f, 1f)]
        public float weightShiftVertical;
        [Range(5f, 50f)]
        public float maxFootRotationOffset;

        [Space(10)]
        [Range(0f, 1f)]
        public float enterStateEaseIn;
    }
    
    public JobSettings settings;
    
    public Vector2 ikOffset;
    public Vector3 normalLeftFoot;
    public Vector3 normalRightFoot;

    public TransformStreamHandle leftToe;
    public TransformStreamHandle rightToe;

    public float ikWeight;
    
    public bool Setup(Animator animator)
    {
        return true;
    }

    public void Dispose() { }

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        if (!settings.enabled || AnimGraph_Stand.useFootIk.IntValue < 1)
            return;
        
        ikWeight = Mathf.Clamp01(ikWeight + (1 - settings.enterStateEaseIn));
        
        if (stream.isHumanStream)
        {
            var human = stream.AsHuman();
            
            var leftToePos = leftToe.GetPosition(stream);
            var rightToePos = rightToe.GetPosition(stream);            

            var leftIkOffset = ikOffset.x * ikWeight;
            var rightIkOffset = ikOffset.y * ikWeight; 
            
            leftToePos += new Vector3(0f, leftIkOffset, 0f);
            rightToePos += new Vector3(0f, rightIkOffset, 0f);      
            

            var leftAnklePos = human.GetGoalPosition(AvatarIKGoal.LeftFoot);
            var rightAnklePos = human.GetGoalPosition(AvatarIKGoal.RightFoot);
            var leftAnkleRot = human.GetGoalRotation(AvatarIKGoal.LeftFoot);
            var rightAnkleRot = human.GetGoalRotation(AvatarIKGoal.RightFoot);
            var leftAnkleIkPos = new Vector3(leftAnklePos.x, leftAnklePos.y + leftIkOffset, leftAnklePos.z);
            var rightAnkleIkPos = new Vector3(rightAnklePos.x, rightAnklePos.y + rightIkOffset, rightAnklePos.z);

            var hipHeightOffset = (leftIkOffset + rightIkOffset) * 0.5f;
            var forwardBackBias = (leftIkOffset - rightIkOffset) * settings.weightShiftHorizontal;

            // TODO: (sunek) Rework weight shift to move towards actual lower foot?
            hipHeightOffset += Mathf.Abs(leftIkOffset - rightIkOffset) * settings.weightShiftVertical;
            var standAngle = Quaternion.AngleAxis(settings.weightShiftAngle, Vector3.up) * Vector3.forward;
            human.bodyLocalPosition += new Vector3(standAngle.x * forwardBackBias, hipHeightOffset, standAngle.z * forwardBackBias);
            
            // Figure out the normal rotation
            var leftNormalRot = Quaternion.LookRotation(Vector3.forward, normalLeftFoot);
            var rightNormalRot = Quaternion.LookRotation(Vector3.forward, normalRightFoot);
            
            // Clamp normal rotation
            var leftAngle = Quaternion.Angle(Quaternion.identity, leftNormalRot);
            var rightAngle = Quaternion.Angle(Quaternion.identity, rightNormalRot);
            
            if (leftAngle > settings.maxFootRotationOffset && settings.maxFootRotationOffset > 0f)
            {
                var fraction = settings.maxFootRotationOffset / leftAngle;
                leftNormalRot = Quaternion.Lerp(Quaternion.identity, leftNormalRot, fraction);
            }

            if (rightAngle > settings.maxFootRotationOffset && settings.maxFootRotationOffset > 0f)
            {
                var fraction = settings.maxFootRotationOffset / rightAngle;
                rightNormalRot = Quaternion.Lerp(Quaternion.identity, rightNormalRot, fraction);                    
            }                
            
            // Apply rotation to ankle                
            var leftToesMatrix = Matrix4x4.TRS(leftToePos, Quaternion.identity, Vector3.one);
            var rightToesMatrix = Matrix4x4.TRS(rightToePos, Quaternion.identity, Vector3.one);

            var leftToesNormalDeltaMatrix = Matrix4x4.TRS(leftToePos, leftNormalRot, Vector3.one) * leftToesMatrix.inverse;
            var rightToesNormalDeltaMatrix = Matrix4x4.TRS(rightToePos, rightNormalRot, Vector3.one) * rightToesMatrix.inverse;
            
            var leftAnkleMatrix = Matrix4x4.TRS(leftAnkleIkPos, leftAnkleRot, Vector3.one) * leftToesMatrix.inverse;
            var rightAnkleMatrix = Matrix4x4.TRS(rightAnkleIkPos, rightAnkleRot, Vector3.one) * rightToesMatrix.inverse;

            leftAnkleMatrix = leftToesNormalDeltaMatrix * leftAnkleMatrix * leftToesMatrix;
            rightAnkleMatrix = rightToesNormalDeltaMatrix * rightAnkleMatrix * rightToesMatrix;
            
            leftAnkleIkPos = leftAnkleMatrix.GetColumn(3);
            rightAnkleIkPos = rightAnkleMatrix.GetColumn(3);
                   
            leftAnkleRot = Quaternion.Lerp(leftAnkleRot, leftAnkleMatrix.rotation, ikWeight);
            rightAnkleRot = Quaternion.Lerp(rightAnkleRot, rightAnkleMatrix.rotation, ikWeight);   
            
            // Update ik position   
            // TODO: (sunek) Consider combating leg overstretch
            var leftPosition = Vector3.Lerp(leftAnklePos, leftAnkleIkPos, ikWeight);
            var rightPosition = Vector3.Lerp(rightAnklePos, rightAnkleIkPos, ikWeight);

            human.SetGoalPosition(AvatarIKGoal.LeftFoot, leftPosition);
            human.SetGoalPosition(AvatarIKGoal.RightFoot, rightPosition);
            human.SetGoalRotation(AvatarIKGoal.LeftFoot, leftAnkleRot);
            human.SetGoalRotation(AvatarIKGoal.RightFoot, rightAnkleRot);

            human.SetGoalWeightPosition(AvatarIKGoal.LeftFoot, 1f);
            human.SetGoalWeightPosition(AvatarIKGoal.RightFoot, 1f);
            human.SetGoalWeightRotation(AvatarIKGoal.LeftFoot, 1f);
            human.SetGoalWeightRotation(AvatarIKGoal.RightFoot, 1f);
        }
    }
}
